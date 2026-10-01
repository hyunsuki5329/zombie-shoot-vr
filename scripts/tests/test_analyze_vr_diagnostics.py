import copy
import json
from pathlib import Path
import sys
import tempfile
import unittest
import subprocess

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
from analyze_vr_diagnostics import analyze


HEADER = {"kind": "header", "schemaVersion": 1, "scenario": "wave-lab",
          "mapId": "interaction-lab", "unityVersion": "6000.3.23f1", "buildGuid": "test",
          "utc": "2026-09-28T00:00:00Z", "automaticIntervalSeconds": 1,
          "cpuGpuTimingCaptured": False}
BASE = {"kind": "sample", "mapId": "interaction-lab", "phase": "Active", "attempt": 1,
        "index": 0, "elapsedSeconds": 0, "activeSeconds": 0, "managedHeapBytes": 100,
        "alive": 0, "rangedAlive": 0, "corpses": 0, "projectiles": 0, "magazines": 1,
        "looseMagazines": 0, "effects": 0, "effectObjects": 2, "enemyBodies": 0,
        "projectileBodies": 0, "magazineBodies": 1, "peakAlive": 0, "peakCorpses": 0,
        "peakProjectiles": 0, "peakMagazines": 1, "peakLoose": 0, "wave": 1, "kills": 0,
        "aliveCap": 4, "rangedCap": 1, "projectileCap": 12, "magazineCap": 10,
        "looseCap": 8, "corpseCap": 4, "effectCap": 16, "enemyBodyCap": 16,
        "boundsSatisfied": True}


def sample(sequence, **changes):
    value = copy.deepcopy(BASE)
    value.update(index=sequence, elapsedSeconds=sequence, activeSeconds=sequence)
    value.update(changes)
    return value


class DiagnosticsTests(unittest.TestCase):
    def setUp(self):
        self.directory = tempfile.TemporaryDirectory()
        self.addCleanup(self.directory.cleanup)
        self.path = Path(self.directory.name) / "capture.jsonl"

    def write(self, rows, footer=True, header=None):
        records = [header or HEADER, *rows]
        if footer:
            records.append({"kind": "footer", "samples": len(rows)})
        self.path.write_text("\n".join(json.dumps(row) for row in records) + "\n", encoding="utf-8")
        return analyze(self.path)

    def test_short_valid_capture_is_not_performance_acceptance(self):
        result = self.write([sample(0), sample(1, managedHeapBytes=120)])
        self.assertTrue(result["recording_complete"])
        self.assertEqual(result["samples"], 2)
        self.assertFalse(result["thirty_minute_sample_span"])
        self.assertEqual(result["performance_acceptance"], "unverified")
        self.assertEqual(result["managed_heap_delta_bytes"], 20)
        self.assertEqual(result["source_sha256"], __import__("hashlib").sha256(self.path.read_bytes()).hexdigest())

    def test_missing_footer_is_incomplete(self):
        self.assertFalse(self.write([sample(0)], footer=False)["recording_complete"])

    def test_bounds_are_recomputed_including_inactive_allocations(self):
        result = self.write([sample(0, alive=5, aliveCap=99, projectileBodies=13,
                                    magazineBodies=11, effectObjects=17, boundsSatisfied=True)])
        self.assertEqual(result["bound_violation_samples"], 1)
        self.assertEqual(result["cap_mismatch_samples"], 1)
        self.assertIn("projectileBodies", result["violating_fields"])
        self.assertIn("alive", result["violating_fields"])

    def test_long_paused_capture_is_not_active_play_or_fps_pass(self):
        result = self.write([sample(i, phase="Paused", activeSeconds=0) for i in range(1801)])
        self.assertTrue(result["thirty_minute_sample_span"])
        self.assertEqual(result["observed_active_seconds"], 0)
        self.assertEqual(result["performance_acceptance"], "unverified")

    def test_retry_does_not_subtract_previous_active_time(self):
        result = self.write([sample(0), sample(1), sample(2, attempt=2, activeSeconds=0),
                             sample(3, attempt=2, activeSeconds=1)])
        self.assertEqual(result["observed_active_seconds"], 2)
        self.assertEqual(result["last_attempt"], 2)

    def test_time_gaps_are_reported(self):
        result = self.write([sample(0), sample(1, elapsedSeconds=10)])
        self.assertEqual(result["sampling_gap_count"], 1)
        self.assertEqual(result["largest_gap_seconds"], 10)

    def test_invalid_schema_numbers_order_and_footer_are_rejected(self):
        for changes in ({"alive": -1}, {"alive": True}, {"elapsedSeconds": float("nan")},
                        {"attempt": 0}, {"index": 3}, {"mapId": "other"}):
            with self.subTest(changes=changes), self.assertRaises(ValueError):
                self.write([sample(0, **changes)])
        with self.assertRaises(ValueError):
            self.write([sample(0)], header={**HEADER, "schemaVersion": 99})
        self.write([sample(0)])
        self.path.write_text(self.path.read_text().replace('"samples": 1', '"samples": 2'))
        with self.assertRaises(ValueError):
            analyze(self.path)

    def test_truncated_json_and_records_after_footer_are_rejected(self):
        for tail in ('{"kind":', json.dumps(sample(1))):
            self.write([sample(0)])
            with self.path.open("a") as stream:
                stream.write(tail)
            with self.subTest(tail=tail), self.assertRaises(ValueError):
                analyze(self.path)

    def test_backward_clocks_and_duplicate_json_keys_are_rejected(self):
        for changed in ({"elapsedSeconds": 0}, {"activeSeconds": 0}):
            with self.subTest(changed=changed), self.assertRaises(ValueError):
                self.write([sample(0, elapsedSeconds=1, activeSeconds=1), sample(1, **changed)])
        self.write([sample(0)])
        self.path.write_text(self.path.read_text().replace('"schemaVersion": 1', '"schemaVersion": 1, "schemaVersion": 1'))
        with self.assertRaises(ValueError):
            analyze(self.path)

    def test_classroom_policy_and_unknown_map_are_distinguished(self):
        result = self.write([sample(0, mapId="classroom", alive=6, rangedAlive=2, aliveCap=6, rangedCap=2)],
                            header={**HEADER, "mapId": "classroom", "scenario": "map-session"})
        self.assertEqual(result["bound_violation_samples"], 0)
        result = self.write([sample(0, mapId="custom")], header={**HEADER, "mapId": "custom"})
        self.assertFalse(result["known_map_policy"])

    def test_cli_creates_summary_but_never_overwrites_output_or_source(self):
        self.write([sample(0)])
        output = self.path.with_suffix(".summary.json")
        command = [sys.executable, "-B", str(Path(__file__).resolve().parents[1] / "analyze_vr_diagnostics.py"),
                   str(self.path), "--output", str(output)]
        original = self.path.read_bytes()
        self.assertEqual(subprocess.run(command, capture_output=True).returncode, 0)
        summary = output.read_bytes()
        self.assertEqual(subprocess.run(command, capture_output=True).returncode, 2)
        self.assertEqual(output.read_bytes(), summary)
        self.assertEqual(self.path.read_bytes(), original)
        command[-1] = str(self.path)
        self.assertEqual(subprocess.run(command, capture_output=True).returncode, 2)
        self.assertEqual(self.path.read_bytes(), original)


if __name__ == "__main__":
    unittest.main()
