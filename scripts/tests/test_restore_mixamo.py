import hashlib
import importlib.util
from pathlib import Path
import tempfile
import unittest
import zipfile

spec = importlib.util.spec_from_file_location("restore_mixamo", Path(__file__).parents[1] / "Restore-MixamoAssets.py")
restore = importlib.util.module_from_spec(spec)
spec.loader.exec_module(restore)


class RestoreMixamoTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name)
        self.source = self.root / "downloads"
        self.source.mkdir()
        self.project = self.root / "project"
        (self.project / "ProjectSettings").mkdir(parents=True)
        (self.project / "ProjectSettings/ProjectVersion.txt").write_text("test")
        self.target = self.project / "Assets/VRFPS/External/Mixamo"
        self.original = restore.EXPECTED
        self.addCleanup(setattr, restore, "EXPECTED", self.original)
        for name in self.original:
            if name.endswith(".zip"):
                with zipfile.ZipFile(self.source / name, "w") as archive:
                    archive.writestr("zombie idle.fbx", b"motion")
            else:
                (self.source / name).write_bytes(name.encode())
        self.set_hashes()

    def set_hashes(self):
        restore.EXPECTED = {name: hashlib.sha256((self.source / name).read_bytes()).hexdigest() for name in self.original}

    def test_repeat_is_idempotent_and_changed_destination_is_preserved(self):
        self.assertEqual(restore.restore(self.source, self.project), 3)
        self.assertEqual(restore.restore(self.source, self.project), 3)
        changed = self.target / "Yaku J Ignite.fbx"
        changed.write_bytes(b"local edit")
        with self.assertRaisesRegex(ValueError, "Preserving modified"):
            restore.restore(self.source, self.project)
        self.assertEqual(changed.read_bytes(), b"local edit")

    def test_bad_last_source_writes_nothing(self):
        (self.source / "Scary Zombie Pack.zip").write_bytes(b"bad")
        with self.assertRaisesRegex(ValueError, "Unexpected contents"):
            restore.restore(self.source, self.project)
        self.assertFalse(self.target.exists())

    def test_zip_escape_writes_nothing(self):
        with zipfile.ZipFile(self.source / "Scary Zombie Pack.zip", "w") as archive:
            archive.writestr("../outside.fbx", b"motion")
        self.set_hashes()
        with self.assertRaisesRegex(ValueError, "Unexpected motion entry"):
            restore.restore(self.source, self.project)
        self.assertFalse(self.target.exists())


if __name__ == "__main__":
    unittest.main()
