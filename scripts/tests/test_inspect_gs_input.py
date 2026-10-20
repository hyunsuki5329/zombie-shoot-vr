import importlib.util
from pathlib import Path
import tempfile
import unittest
import subprocess
import sys


class InspectGsInputTests(unittest.TestCase):
    def inspect(self, data):
        spec = importlib.util.spec_from_file_location(
            "inspect_gs_input", Path(__file__).parents[1] / "inspect_gs_input.py")
        self.assertTrue(Path(spec.origin).exists(), "GS input inspection is not implemented")
        module = importlib.util.module_from_spec(spec)
        spec.loader.exec_module(module)
        with tempfile.TemporaryDirectory() as folder:
            path = Path(folder) / "room.ply"
            path.write_bytes(data)
            return module.inspect_ply(path)

    @staticmethod
    def ply(properties, count=1, payload=b""):
        return ("ply\nformat binary_little_endian 1.0\n"
                f"element vertex {count}\n" +
                "".join(f"property {kind} {name}\n" for kind, name in properties) +
                "end_header\n").encode() + payload

    def test_scaniverse_rgb_point_cloud_is_not_gaussian(self):
        props = [("float", x) for x in ("x", "y", "z")]
        props += [("uchar", x) for x in ("red", "green", "blue")]
        result = self.inspect(self.ply(props, payload=bytes(15)))
        self.assertEqual("point-cloud", result["classification"])
        self.assertFalse(result["gaussianHeader"])
        self.assertIn("opacity", result["missingGaussianProperties"])

    def test_gaussian_header_does_not_claim_renderer_acceptance(self):
        names = ["x", "y", "z", "f_dc_0", "f_dc_1", "f_dc_2", "opacity"]
        names += [f"scale_{i}" for i in range(3)] + [f"rot_{i}" for i in range(4)]
        result = self.inspect(self.ply([("float", x) for x in names], payload=bytes(4 * len(names))))
        self.assertTrue(result["gaussianHeader"])
        self.assertEqual("not-tested", result["rendererImport"])

    def test_truncated_payload_is_rejected(self):
        with self.assertRaises(ValueError):
            self.inspect(self.ply([("float", "x")], payload=b"\0"))

    def test_missing_header_end_is_rejected(self):
        with self.assertRaises(ValueError):
            self.inspect(b"ply\nformat binary_little_endian 1.0\n")

    def test_invalid_magic_is_rejected(self):
        with self.assertRaises(ValueError):
            self.inspect(b"not a ply\nend_header\n")

    def test_nonpositive_vertex_count_is_rejected(self):
        for count in (0, -1):
            with self.assertRaises(ValueError):
                self.inspect(self.ply([("float", "x")], count))

    def test_face_properties_cannot_supply_gaussian_attributes(self):
        data = self.ply([("float", x) for x in ("x", "y", "z")], payload=bytes(12))
        data = data.replace(b"end_header", b"element face 0\nproperty float opacity\nend_header")
        self.assertIn("opacity", self.inspect(data)["missingGaussianProperties"])

    def test_cli_cannot_overwrite_capture_or_existing_evidence(self):
        with tempfile.TemporaryDirectory() as folder:
            path = Path(folder) / "room.ply"
            original = self.ply([("float", x) for x in ("x", "y", "z")], payload=bytes(12))
            path.write_bytes(original)
            script = Path(__file__).parents[1] / "inspect_gs_input.py"
            result = subprocess.run([sys.executable, str(script), str(path), "--output", str(path)],
                                    capture_output=True)
            self.assertNotEqual(0, result.returncode)
            self.assertEqual(original, path.read_bytes())


if __name__ == "__main__":
    unittest.main()
