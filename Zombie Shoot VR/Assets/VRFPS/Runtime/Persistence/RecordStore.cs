using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Security;
using System.Xml;

namespace VRFPS
{
    public sealed class RecordStore
    {
        [DataContract] sealed class Document
        {
            [DataMember(Name = "schemaVersion", IsRequired = true, Order = 0)] public int Schema;
            [DataMember(Name = "records", IsRequired = true, Order = 1)] public Row[] Records;
        }
        [DataContract] sealed class Row
        {
            [DataMember(Name = "mapId", IsRequired = true, Order = 0)] public string Map;
            [DataMember(Name = "reachedWave", IsRequired = true, Order = 1)] public int Wave;
            [DataMember(Name = "kills", IsRequired = true, Order = 2)] public int Kills;
        }
        readonly string directory, main, backup;
        Dictionary<string, BestRecord> records = new Dictionary<string, BestRecord>(StringComparer.Ordinal);
        public string LastError { get; private set; }
        public bool RecoveredFromBackup { get; private set; }

        public RecordStore(string directory)
        {
            if (string.IsNullOrWhiteSpace(directory)) throw new ArgumentException("A record directory is required.", nameof(directory));
            this.directory = Path.GetFullPath(directory);
            main = Path.Combine(this.directory, "records-v1.json"); backup = main + ".bak";
            var mainError = "Main record file is missing.";
            if (File.Exists(main) && TryRead(main, out var loaded, out mainError)) { records = loaded; return; }
            if (File.Exists(backup))
            {
                if (TryRead(backup, out loaded, out var backupError))
                {
                    records = loaded; RecoveredFromBackup = true;
                    LastError = mainError + " Recovered the previous valid backup.";
                    return;
                }
                LastError = mainError + " Backup could not be loaded: " + backupError;
            }
            else if (File.Exists(main)) LastError = mainError;
        }

        public BestRecord Read(string mapId)
        {
            ValidateMap(mapId);
            return records.TryGetValue(mapId, out var record) ? record : default;
        }

        public bool TryUpdate(string mapId, int reachedWave, int kills)
        {
            ValidateMap(mapId); ValidateScore(reachedWave, kills);
            if (!IsBetter(Read(mapId), reachedWave, kills)) return false;
            records[mapId] = new BestRecord(reachedWave, kills);
            return true;
        }

        public static bool IsBetter(BestRecord old, int reachedWave, int kills) =>
            reachedWave > old.reachedWave || reachedWave == old.reachedWave && kills > old.kills;

        public bool Save()
        {
            var temporary = Path.Combine(directory, "records-v1." + Guid.NewGuid().ToString("N") + ".tmp");
            try
            {
                Directory.CreateDirectory(directory);
                var document = new Document { Schema = 1, Records = records.OrderBy(pair => pair.Key, StringComparer.Ordinal)
                    .Select(pair => new Row { Map = pair.Key, Wave = pair.Value.reachedWave, Kills = pair.Value.kills }).ToArray() };
                using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                {
                    new DataContractJsonSerializer(typeof(Document)).WriteObject(stream, document);
                    stream.Flush(true);
                }
                if (!TryRead(temporary, out _, out var error)) throw new InvalidDataException("Temporary record verification failed: " + error);
                if (File.Exists(main))
                {
                    // Preserve the previous valid snapshot, never replace it with a corrupt main.
                    var validMain = TryRead(main, out _, out _);
                    File.Replace(temporary, main, validMain ? backup : null);
                }
                else File.Move(temporary, main);
                LastError = null;
                return true;
            }
            catch (Exception error) when (IsStorageError(error))
            {
                LastError = "Record save failed: " + error.Message;
                return false;
            }
            finally
            {
                try { if (File.Exists(temporary)) File.Delete(temporary); }
                catch (Exception error) when (IsStorageError(error))
                { LastError = (LastError ?? "") + " Temporary file cleanup failed: " + error.Message; }
            }
        }

        static bool TryRead(string path, out Dictionary<string, BestRecord> loaded, out string error)
        {
            loaded = null; error = null;
            try
            {
                Document document;
                using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    if (stream.Length > 65536) throw new InvalidDataException("Record file exceeds the schema size limit.");
                    document = new DataContractJsonSerializer(typeof(Document)).ReadObject(stream) as Document;
                }
                if (document == null || document.Schema != 1 || document.Records == null || document.Records.Length > 2)
                    throw new InvalidDataException("Unsupported record schema.");
                var candidate = new Dictionary<string, BestRecord>(StringComparer.Ordinal);
                foreach (var row in document.Records)
                {
                    if (row == null) throw new InvalidDataException("Empty record row.");
                    ValidateMap(row.Map); ValidateScore(row.Wave, row.Kills);
                    if (candidate.ContainsKey(row.Map)) throw new InvalidDataException("Duplicate map record.");
                    candidate.Add(row.Map, new BestRecord(row.Wave, row.Kills));
                }
                loaded = candidate;
                return true;
            }
            catch (Exception failure) when (IsStorageError(failure)) { error = failure.Message; return false; }
        }
        static void ValidateMap(string mapId)
        {
            if (mapId != "room" && mapId != "classroom") throw new ArgumentException("Unknown map ID.", nameof(mapId));
        }
        static void ValidateScore(int wave, int kills)
        {
            if (wave < 0 || kills < 0 || wave == 0 && kills != 0)
                throw new ArgumentOutOfRangeException(nameof(wave), "Record values must describe a started run or an empty record.");
        }
        static bool IsStorageError(Exception error) => error is IOException || error is InvalidDataException || error is UnauthorizedAccessException ||
            error is SerializationException || error is XmlException || error is ArgumentException ||
            error is SecurityException || error is NotSupportedException;
    }
}
