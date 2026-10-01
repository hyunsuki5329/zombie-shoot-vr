import csv
import json
from pathlib import Path
import struct
import subprocess
import sys
import tempfile
import unittest

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
from analyze_vr_frame_timings import analyze

META = {"schemaVersion": 1, "source": "Unity.FrameTimingManager", "units": "milliseconds",
        "mapId": "interaction-lab", "scenario": "wave-lab", "buildGuid": "fixture",
        "graphicsApi": "Synthetic", "phaseNames": ["Preparing", "Active", "Paused", "Dead"]}
FRAME = struct.Struct("<QdiiiI5d")


def frame(index, *, times=(14, 3, 2, 1, 8), mask=31, phase=1, attempt=1):
    return FRAME.pack(index + 100, float(index), index + 10, attempt, phase, mask, *times)


class FrameAnalysisTests(unittest.TestCase):
    def setUp(self):
        self.directory = tempfile.TemporaryDirectory()
        self.addCleanup(self.directory.cleanup)
        self.path = Path(self.directory.name) / "frames.vrft"

    def write(self, frames, *, footer=True, metadata=None, tail=b"", footer_count=None):
        meta = json.dumps(metadata or META).encode()
        data = b"VRFT0001" + struct.pack("<i", len(meta)) + meta
        data += b"".join(b"\x01" + row for row in frames)
        if footer:
            masks = [FRAME.unpack(row)[5] for row in frames]
            data += b"\x02" + struct.pack("<7q", len(frames) if footer_count is None else footer_count,
                                          0, 0, 0, 0, sum(bool(m & 1) for m in masks), sum(bool(m & 16) for m in masks))
        self.path.write_bytes(data + tail)

    def test_nearest_rank_percentiles_keep_cpu_wait_semantics(self):
        self.write([frame(i, times=(i, i, i, 0, i)) for i in range(1, 101)])
        result = analyze(self.path)
        self.assertTrue(result["recording_complete"])
        self.assertEqual(result["metrics"]["gpu_ms"]["p95"], 95)
        self.assertEqual(result["metrics"]["gpu_ms"]["p99"], 99)
        self.assertEqual(result["metrics"]["cpu_total_ms"]["p95"], 95)
        self.assertIsNone(result["cpu_active_work_p95_ms"])
        self.assertEqual(result["performance_acceptance"], "unverified")

    def test_invalid_gpu_and_nan_cpu_are_excluded_and_csv_uses_empty_cells(self):
        self.write([frame(1, times=(14, 3, 2, 1, 0), mask=15),
                    frame(2, times=(float("nan"), 3, 2, 1, 8), mask=30)])
        output = self.path.with_suffix(".csv")
        result = analyze(self.path, csv_path=output)
        self.assertEqual(result["metrics"]["gpu_ms"]["valid_samples"], 1)
        self.assertEqual(result["metrics"]["gpu_ms"]["p95"], 8)
        self.assertEqual(result["metrics"]["cpu_total_ms"]["invalid_samples"], 1)
        with output.open(newline="") as stream:
            rows = list(csv.DictReader(stream))
        self.assertEqual(rows[0]["gpu_ms"], "")
        self.assertEqual(rows[1]["cpu_total_ms"], "")
        original = output.read_bytes()
        with self.assertRaises(FileExistsError):
            analyze(self.path, csv_path=output)
        self.assertEqual(output.read_bytes(), original)

    def test_time_attempt_and_retrieval_phase_filters(self):
        self.write([frame(1), frame(2, phase=2), frame(3, attempt=2), frame(4, attempt=2)])
        result = analyze(self.path, after=2, before=4, attempt=2, phase="Active")
        self.assertEqual(result["selected_frames"], 2)
        self.assertEqual(result["frames"], 4)

    def test_missing_or_partial_footer_remains_incomplete(self):
        for tail in (b"", b"\x02\x00", b"\x01\x00"):
            with self.subTest(tail=tail):
                self.write([frame(1)], footer=False, tail=tail)
                self.assertFalse(analyze(self.path)["recording_complete"])

    def test_corrupt_mask_timestamp_footer_and_units_are_rejected(self):
        for rows in ([frame(1, times=(14, 3, 2, 1, 0), mask=31)], [frame(1), frame(1)]):
            self.write(rows)
            with self.assertRaises(ValueError):
                analyze(self.path)
        self.write([frame(1)], footer_count=2)
        with self.assertRaises(ValueError):
            analyze(self.path)
        self.write([frame(1)], metadata={**META, "units": "seconds"})
        with self.assertRaises(ValueError):
            analyze(self.path)

    def test_empty_and_filtered_empty_data_never_become_zero_ms(self):
        self.write([])
        result = analyze(self.path)
        self.assertIsNone(result["metrics"]["gpu_ms"]["p95"])
        self.write([frame(1)])
        self.assertIsNone(analyze(self.path, after=100)["metrics"]["gpu_ms"]["p95"])

    def test_frame_limit_and_bad_input_never_publish_csv(self):
        self.write([frame(1), frame(2)])
        output = self.path.with_suffix(".csv")
        with self.assertRaises(ValueError):
            analyze(self.path, max_frames=1, csv_path=output)
        self.assertFalse(output.exists())
        self.write([frame(1)], tail=b"\x01")
        with self.assertRaises(ValueError):
            analyze(self.path)

    def test_context_metadata_and_option_boundaries(self):
        self.write([frame(1)])
        for options in ({"after": -1}, {"after": float("nan")}, {"after": 2, "before": 1},
                        {"attempt": 0}, {"phase": "unknown"}, {"max_frames": 0}):
            with self.subTest(options=options), self.assertRaises(ValueError):
                analyze(self.path, **options)
        for row in (frame(1, attempt=0), frame(1, phase=99), frame(1, mask=63)):
            self.write([row])
            with self.assertRaises(ValueError):
                analyze(self.path)
        self.write([frame(1)], metadata={**META, "schemaVersion": True})
        with self.assertRaises(ValueError):
            analyze(self.path)

    def test_cli_outputs_and_existing_file_protection(self):
        self.write([frame(1)])
        script = Path(__file__).resolve().parents[1] / "analyze_vr_frame_timings.py"
        output = self.path.with_suffix(".json")
        csv_output = self.path.with_suffix(".csv")
        command = [sys.executable, "-B", str(script), str(self.path), "--output", str(output), "--csv", str(csv_output)]
        first = subprocess.run(command, capture_output=True, text=True)
        self.assertEqual(first.returncode, 0, first.stderr)
        self.assertEqual(json.loads(output.read_text())["selected_frames"], 1)
        saved = (output.read_bytes(), csv_output.read_bytes())
        self.assertEqual(subprocess.run(command, capture_output=True).returncode, 2)
        self.assertEqual(saved, (output.read_bytes(), csv_output.read_bytes()))
        source = self.path.read_bytes()
        same = subprocess.run([sys.executable, "-B", str(script), str(self.path), "--output", str(self.path)], capture_output=True)
        self.assertEqual(same.returncode, 2)
        self.assertEqual(self.path.read_bytes(), source)


if __name__ == "__main__":
    unittest.main()
