using System;
using System.IO;
using NUnit.Framework;

namespace VRFPS.Tests
{
    public class RecordStoreTests
    {
        string directory;
        string Main => Path.Combine(directory, "records-v1.json");
        [SetUp] public void Setup()
        {
            directory = Path.GetFullPath(Path.Combine("Temp", "VRFPS-RecordTests", Guid.NewGuid().ToString("N")));
            Directory.CreateDirectory(directory);
        }
        [TearDown] public void Cleanup()
        {
            // Only this fixture's newly created GUID directory is owned by this test.
            if (Directory.Exists(directory)) Directory.Delete(directory, true);
        }

        [Test] public void BestRecordKeepsOneRunAndSeparatesMapsAcrossReload()
        {
            var store = new RecordStore(directory);
            Assert.That(store.Read("room").reachedWave, Is.Zero);
            Assert.That(store.TryUpdate("room", 6, 42), Is.True);
            Assert.That(store.TryUpdate("room", 5, 100), Is.False);
            Assert.That(store.TryUpdate("room", 6, 43), Is.True);
            Assert.That(store.TryUpdate("room", 7, 1), Is.True);
            Assert.That(store.TryUpdate("classroom", 3, 15), Is.True);
            Assert.That(store.Save(), Is.True);
            var reloaded = new RecordStore(directory);
            Assert.That(reloaded.Read("room").reachedWave, Is.EqualTo(7));
            Assert.That(reloaded.Read("room").kills, Is.EqualTo(1));
            Assert.That(reloaded.Read("classroom").reachedWave, Is.EqualTo(3));
            Assert.That(reloaded.Read("classroom").kills, Is.EqualTo(15));
            var copy = reloaded.Read("room"); copy.kills = 999;
            Assert.That(reloaded.Read("room").kills, Is.EqualTo(1));
        }

        [Test] public void CorruptMainRecoversBackupAndSavePreservesThatGoodBackup()
        {
            var store = new RecordStore(directory);
            store.TryUpdate("room", 5, 20); Assert.That(store.Save(), Is.True);
            store.TryUpdate("room", 6, 40); Assert.That(store.Save(), Is.True);
            var goodBackup = File.ReadAllText(Main + ".bak");
            File.WriteAllText(Main, "{broken");
            var recovered = new RecordStore(directory);
            Assert.That(recovered.Read("room").reachedWave, Is.EqualTo(5));
            Assert.That(recovered.RecoveredFromBackup, Is.True);
            Assert.That(recovered.LastError, Is.Not.Null.And.Not.Empty);
            recovered.TryUpdate("room", 7, 1);
            Assert.That(recovered.Save(), Is.True, recovered.LastError);
            Assert.That(File.ReadAllText(Main + ".bak"), Is.EqualTo(goodBackup));
            Assert.That(new RecordStore(directory).Read("room").reachedWave, Is.EqualTo(7));
        }

        [TestCase("{broken")]
        [TestCase("{\"schemaVersion\":2,\"records\":[]}")]
        [TestCase("{\"schemaVersion\":1,\"records\":[{\"mapId\":\"room\",\"reachedWave\":-1,\"kills\":2}]}")]
        [TestCase("{\"schemaVersion\":1,\"records\":[{\"mapId\":\"room\",\"reachedWave\":6}]}")]
        [TestCase("{\"schemaVersion\":1,\"records\":[{\"mapId\":\"unknown\",\"reachedWave\":6,\"kills\":2}]}")]
        [TestCase("{\"schemaVersion\":1,\"records\":[{\"mapId\":\"room\",\"reachedWave\":6,\"kills\":2},{\"mapId\":\"room\",\"reachedWave\":7,\"kills\":3}]}")]
        public void InvalidFileStartsEmptyAndReportsErrorWithoutOverwritingEvidence(string json)
        {
            File.WriteAllText(Main, json);
            var store = new RecordStore(directory);
            Assert.That(store.Read("room").reachedWave, Is.Zero);
            Assert.That(store.LastError, Is.Not.Null.And.Not.Empty);
            Assert.That(File.ReadAllText(Main), Is.EqualTo(json));
        }

        [Test] public void LockedDestinationReturnsFailureAndRetainsInMemoryBestForRetry()
        {
            var store = new RecordStore(directory);
            store.TryUpdate("room", 2, 10); Assert.That(store.Save(), Is.True);
            var original = File.ReadAllText(Main);
            store.TryUpdate("room", 3, 1);
            using (var held = new FileStream(Main, FileMode.Open, FileAccess.Read, FileShare.None))
            {
                Assert.That(store.Save(), Is.False);
                Assert.That(store.LastError, Is.Not.Null.And.Not.Empty);
            }
            Assert.That(File.ReadAllText(Main), Is.EqualTo(original));
            Assert.That(store.Read("room").reachedWave, Is.EqualTo(3));
            Assert.That(Directory.GetFiles(directory, "*.tmp"), Is.Empty);
            Assert.That(store.Save(), Is.True);
            Assert.That(new RecordStore(directory).Read("room").kills, Is.EqualTo(1));
        }

        [Test] public void UnwritableDirectoryDoesNotThrowOrLoseCurrentScore()
        {
            var blocker = Path.Combine(directory, "file-not-directory");
            File.WriteAllText(blocker, "preserve");
            var store = new RecordStore(blocker);
            store.TryUpdate("classroom", 4, 18);
            Assert.That(store.Save(), Is.False);
            Assert.That(store.LastError, Is.Not.Null.And.Not.Empty);
            Assert.That(store.Read("classroom").kills, Is.EqualTo(18));
            Assert.That(File.ReadAllText(blocker), Is.EqualTo("preserve"));
        }

        [Test] public void InvalidInputDoesNotMutateValidRecords()
        {
            var store = new RecordStore(directory);
            store.TryUpdate("room", 6, 42);
            Assert.Throws<ArgumentException>(() => store.TryUpdate("../room", 7, 3));
            Assert.Throws<ArgumentOutOfRangeException>(() => store.TryUpdate("room", -1, 3));
            Assert.Throws<ArgumentOutOfRangeException>(() => store.TryUpdate("room", 0, 3));
            Assert.That(store.TryUpdate("room", 6, 42), Is.False);
            Assert.That(store.Read("room").kills, Is.EqualTo(42));
        }

        [Test] public void PublishedSchemaLoadsAndMissingMainUsesValidBackup()
        {
            File.WriteAllText(Main + ".bak", "{\"schemaVersion\":1,\"records\":[{\"mapId\":\"room\",\"reachedWave\":6,\"kills\":42},{\"mapId\":\"classroom\",\"reachedWave\":4,\"kills\":11}]}");
            var store = new RecordStore(directory);
            Assert.That(store.RecoveredFromBackup, Is.True);
            Assert.That(store.Read("room").reachedWave, Is.EqualTo(6));
            Assert.That(store.Read("room").kills, Is.EqualTo(42));
            Assert.That(store.Read("classroom").kills, Is.EqualTo(11));
            Assert.That(store.Save(), Is.True);
            Assert.That(store.LastError, Is.Null);
            Assert.That(new RecordStore(directory).Read("classroom").reachedWave, Is.EqualTo(4));
        }

        [Test] public void BothCorruptFilesRemainUntouchedAndReturnEmptyRecords()
        {
            File.WriteAllText(Main, "{broken");
            const string backup = "{\"schemaVersion\":2,\"records\":[]}";
            File.WriteAllText(Main + ".bak", backup);
            var store = new RecordStore(directory);
            Assert.That(store.Read("room").reachedWave, Is.Zero);
            Assert.That(store.Read("classroom").kills, Is.Zero);
            Assert.That(store.RecoveredFromBackup, Is.False);
            Assert.That(store.LastError, Is.Not.Null.And.Not.Empty);
            Assert.That(File.ReadAllText(Main), Is.EqualTo("{broken"));
            Assert.That(File.ReadAllText(Main + ".bak"), Is.EqualTo(backup));
        }
    }
}
