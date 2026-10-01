"""Read VRFT0001 raw timings without claiming hardware performance acceptance."""
import argparse
from array import array
from contextlib import ExitStack
import csv
import hashlib
import json
import math
from pathlib import Path
import shutil
import struct
import sys
import tempfile

from analyze_vr_diagnostics import unique_object, reject_constant

FRAME = struct.Struct("<QdiiiI5d")
FOOTER = struct.Struct("<7q")
METRICS = ("cpu_total_ms", "cpu_main_ms", "cpu_render_ms", "cpu_present_wait_ms", "gpu_ms")


def analyze(path, *, after=0, before=None, attempt=None, phase=None,
            max_frames=2_000_000, csv_path=None):
    if (not math.isfinite(after) or after < 0 or
            before is not None and (not math.isfinite(before) or before < after)):
        raise ValueError("Invalid elapsed-time interval")
    if attempt is not None and (type(attempt) is not int or attempt < 1):
        raise ValueError("attempt must be a positive integer")
    if type(max_frames) is not int or max_frames < 1:
        raise ValueError("max_frames must be a positive integer")
    path = Path(path)
    csv_path = Path(csv_path) if csv_path is not None else None
    if csv_path is not None and csv_path.exists():
        raise FileExistsError(csv_path)
    digest = hashlib.sha256()
    values = [array("d") for _ in METRICS]
    count = selected = valid_cpu = valid_gpu = 0
    previous = footer = None
    truncated = False
    with ExitStack() as stack:
        stream = stack.enter_context(path.open("rb"))
        pending_csv = None
        if csv_path is not None:
            pending_csv = stack.enter_context(tempfile.TemporaryFile(
                mode="w+", encoding="utf-8", newline="", dir=csv_path.parent))
            writer = csv.writer(pending_csv)
            writer.writerow(("timestamp", "elapsed_seconds", "retrieval_frame", "attempt", "phase", "valid_mask", *METRICS))

        def read(size):
            raw = stream.read(size)
            digest.update(raw)
            return raw

        if read(8) != b"VRFT0001":
            raise ValueError("Not a VRFT0001 file")
        raw = read(4)
        if len(raw) != 4:
            raise ValueError("Incomplete metadata length")
        length, = struct.unpack("<i", raw)
        if not 1 <= length <= 65536:
            raise ValueError("Invalid metadata length")
        raw = read(length)
        if len(raw) != length:
            raise ValueError("Incomplete metadata")
        meta = json.loads(raw.decode("utf-8"), object_pairs_hook=unique_object,
                          parse_constant=reject_constant)
        if (not isinstance(meta, dict) or type(meta.get("schemaVersion")) is not int or
                meta["schemaVersion"] != 1 or meta.get("units") != "milliseconds" or
                meta.get("source") != "Unity.FrameTimingManager" or
                meta.get("phaseNames") != ["Preparing", "Active", "Paused", "Dead"]):
            raise ValueError("Unsupported frame metadata")
        phases = meta["phaseNames"]
        if phase is not None and phase not in phases:
            raise ValueError("Unknown retrieval phase")
        while tag := read(1):
            if tag not in (b"\x01", b"\x02"):
                raise ValueError("Unknown record tag")
            raw = read(FRAME.size if tag == b"\x01" else FOOTER.size)
            if len(raw) != (FRAME.size if tag == b"\x01" else FOOTER.size):
                truncated = True
                break
            if tag == b"\x02":
                footer = FOOTER.unpack(raw)
                if (any(n < 0 for n in footer) or footer[0] != count or
                        footer[5:] != (valid_cpu, valid_gpu)):
                    raise ValueError("Footer counters disagree with records")
                if read(1):
                    raise ValueError("Data after footer")
                break
            stamp, elapsed, retrieval, run, state, mask, *times = FRAME.unpack(raw)
            if (stamp == 0 or not math.isfinite(elapsed) or elapsed < 0 or retrieval < 0 or
                    run < 1 or not 0 <= state < len(phases)):
                raise ValueError("Invalid frame context")
            if previous is not None and (stamp <= previous[0] or elapsed < previous[1] or
                                         retrieval < previous[2] or run < previous[3]):
                raise ValueError("Frame context regressed")
            expected = sum(1 << i for i, value in enumerate(times)
                           if math.isfinite(value) and (value >= 0 if i == 3 else value > 0))
            if mask != expected:
                raise ValueError("Validity mask disagrees with raw times")
            previous = (stamp, elapsed, retrieval, run)
            count += 1
            if count > max_frames:
                raise ValueError("Frame limit exceeded; increase --max-frames explicitly")
            valid_cpu += bool(mask & 1)
            valid_gpu += bool(mask & 16)
            if (elapsed < after or before is not None and elapsed > before or
                    attempt is not None and run != attempt or phase is not None and phases[state] != phase):
                continue
            selected += 1
            for i, value in enumerate(times):
                if mask & (1 << i):
                    values[i].append(value)
            if pending_csv is not None:
                writer.writerow((stamp, elapsed, retrieval, run, phases[state], mask,
                                 *(value if mask & (1 << i) else "" for i, value in enumerate(times))))
        metrics = {}
        for name, samples in zip(METRICS, values):
            ordered = sorted(samples)
            metrics[name] = {"valid_samples": len(ordered), "invalid_samples": selected - len(ordered),
                             **{f"p{p}": ordered[math.ceil(p / 100 * len(ordered)) - 1] if ordered else None
                                for p in (50, 95, 99)}}
        result = {"schema_version": 1, "input": str(path), "sha256": digest.hexdigest(),
                  "metadata": meta, "frames": count, "selected_frames": selected,
                  "recording_complete": footer is not None, "truncated_record": truncated,
                  "filters": {"after": after, "before": before, "attempt": attempt, "retrieval_phase": phase},
                  "percentile_method": "nearest-rank", "metrics": metrics,
                  "footer": dict(zip(("accepted", "duplicates", "zero_timestamp", "disabled_polls", "empty_polls",
                                      "valid_cpu_total", "valid_gpu"), footer)) if footer is not None else None,
                  "cpu_active_work_p95_ms": None, "performance_acceptance": "unverified",
                  "limitations": ["CPU total includes waits; it is not active CPU work.",
                                  "Phase and attempt describe retrieval time, not the delayed measured frame.",
                                  "Latest-one retrieval does not guarantee full frame coverage.",
                                  "This file alone proves neither headset settings nor real-map duration/RAM/VRAM acceptance."]}
        if pending_csv is not None:
            pending_csv.seek(0)
            with csv_path.open("x", encoding="utf-8", newline="") as output:
                shutil.copyfileobj(pending_csv, output)
        return result


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("capture", type=Path)
    parser.add_argument("--after", type=float, default=0)
    parser.add_argument("--before", type=float)
    parser.add_argument("--attempt", type=int)
    parser.add_argument("--phase", choices=("Preparing", "Active", "Paused", "Dead"))
    parser.add_argument("--max-frames", type=int, default=2_000_000)
    parser.add_argument("--output", type=Path)
    parser.add_argument("--csv", type=Path)
    args = parser.parse_args()
    try:
        for output in (args.output, args.csv):
            if output is not None and output.exists():
                raise FileExistsError(output)
        if args.output is not None and args.csv is not None and args.output.resolve() == args.csv.resolve():
            raise ValueError("JSON and CSV outputs must differ")
        result = analyze(args.capture, after=args.after, before=args.before, attempt=args.attempt,
                         phase=args.phase, max_frames=args.max_frames, csv_path=args.csv)
        text = json.dumps(result, ensure_ascii=False, indent=2, allow_nan=False) + "\n"
        if args.output is None:
            print(text, end="")
        else:
            with args.output.open("x", encoding="utf-8") as output:
                output.write(text)
    except (OSError, ValueError) as error:
        print(f"Frame analysis failed: {error}", file=sys.stderr)
        return 2
    return 0


if __name__ == "__main__":
    sys.exit(main())
