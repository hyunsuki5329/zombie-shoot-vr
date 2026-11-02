"""Summarize schema-1 object diagnostics; never infer VR performance acceptance."""
import argparse
import hashlib
import json
import math
from pathlib import Path
import sys

COUNTS = "alive rangedAlive corpses projectiles magazines looseMagazines effects effectObjects enemyBodies projectileBodies magazineBodies peakAlive peakCorpses peakProjectiles peakMagazines peakLoose wave kills managedHeapBytes".split()
CAP_FIELDS = {"alive": "aliveCap", "rangedAlive": "rangedCap", "corpses": "corpseCap",
              "projectiles": "projectileCap", "magazines": "magazineCap", "looseMagazines": "looseCap",
              "effects": "effectCap", "enemyBodies": "enemyBodyCap"}
MAP_CAPS = {"room": (4, 1), "classroom": (6, 2), "interaction-lab": (4, 1)}


def integer(record, field, minimum=0):
    value = record.get(field)
    if type(value) is not int or value < minimum:
        raise ValueError(f"{field} must be an integer >= {minimum}")
    return value


def number(record, field):
    value = record.get(field)
    if type(value) not in (int, float) or not math.isfinite(value) or value < 0:
        raise ValueError(f"{field} must be finite and nonnegative")
    return value


def unique_object(pairs):
    result = {}
    for key, value in pairs:
        if key in result:
            raise ValueError(f"Duplicate JSON key: {key}")
        result[key] = value
    return result


def reject_constant(value):
    raise ValueError(f"Non-JSON numeric constant: {value}")


def analyze(path):
    path = Path(path)
    digest = hashlib.sha256()
    header = footer = previous = first = last = None
    samples = gaps = violations = mismatches = 0
    largest_gap = active_seconds = 0.0
    maxima = {field: 0 for field in COUNTS}
    minimum_heap = None
    violating_fields = set()
    phases = {}
    # Read bounded lines and aggregate, retaining no sample list.
    with path.open("rb") as stream:
        line_number = 0
        while raw := stream.readline(1024 * 1024 + 1):
            line_number += 1
            if len(raw) > 1024 * 1024:
                raise ValueError(f"Line {line_number}: record exceeds 1 MiB")
            digest.update(raw)
            if not raw.strip():
                continue
            try:
                row = json.loads(raw.decode("utf-8-sig" if line_number == 1 else "utf-8"),
                                 object_pairs_hook=unique_object, parse_constant=reject_constant)
                if not isinstance(row, dict):
                    raise ValueError("Record must be an object")
                if footer is not None:
                    raise ValueError("Record appears after footer")
                if header is None:
                    if row.get("kind") != "header" or integer(row, "schemaVersion") != 1:
                        raise ValueError("Expected schema-1 header")
                    for field in ("mapId", "scenario", "utc", "unityVersion", "buildGuid"):
                        if not isinstance(row.get(field), str) or not row[field]:
                            raise ValueError(f"Missing header {field}")
                    if row["scenario"] not in ("wave-lab", "map-session"):
                        raise ValueError("Unknown capture scenario")
                    if row.get("cpuGpuTimingCaptured") is not False:
                        raise ValueError("Schema 1 has no CPU/GPU timing samples")
                    if number(row, "automaticIntervalSeconds") != 1:
                        raise ValueError("Schema 1 expects a one-second automatic interval")
                    header = row
                    alive_cap, ranged_cap = MAP_CAPS.get(row["mapId"], (6, 2))
                    limits = {"alive": alive_cap, "rangedAlive": ranged_cap, "corpses": 4,
                              "projectiles": 12, "projectileBodies": 12, "magazines": 10,
                              "magazineBodies": 10, "looseMagazines": 8, "effects": 16,
                              "effectObjects": 16, "enemyBodies": 16}
                    continue
                if row.get("kind") == "footer":
                    if integer(row, "samples") != samples:
                        raise ValueError("Footer sample count does not match")
                    footer = row
                    continue
                if row.get("kind") != "sample":
                    raise ValueError("Expected sample or footer")
                if integer(row, "index") != samples:
                    raise ValueError("Missing, repeated or reordered sample index")
                integer(row, "attempt", 1)
                for field in COUNTS + list(CAP_FIELDS.values()):
                    integer(row, field)
                elapsed = number(row, "elapsedSeconds")
                active = number(row, "activeSeconds")
                if row.get("mapId") != header["mapId"]:
                    raise ValueError("Map changed inside one capture")
                if row.get("phase") not in ("Active", "Preparing", "Paused", "Dead"):
                    raise ValueError("Unknown run phase")
                if type(row.get("boundsSatisfied")) is not bool:
                    raise ValueError("Missing boolean boundsSatisfied")
                if previous is not None:
                    gap = elapsed - previous["elapsedSeconds"]
                    if gap < 0 or row["attempt"] < previous["attempt"]:
                        raise ValueError("Time or attempt moved backwards")
                    largest_gap = max(largest_gap, gap)
                    gaps += gap > 2.5
                    if row["attempt"] == previous["attempt"]:
                        delta = active - previous["activeSeconds"]
                        if delta < 0:
                            raise ValueError("Active clock reset without a new attempt")
                        active_seconds += delta
                exceeded = {field for field, cap in limits.items() if row[field] > cap}
                violations += bool(exceeded)
                violating_fields.update(exceeded)
                mismatches += any(row[cap_field] != limits[field] for field, cap_field in CAP_FIELDS.items())
                for field in COUNTS:
                    maxima[field] = max(maxima[field], row[field])
                minimum_heap = row["managedHeapBytes"] if minimum_heap is None else min(minimum_heap, row["managedHeapBytes"])
                phases[row["phase"]] = phases.get(row["phase"], 0) + 1
                if first is None:
                    first = row
                previous = last = row
                samples += 1
            except (ValueError, UnicodeError, OverflowError) as error:
                raise ValueError(f"Line {line_number}: {error}") from error
    if header is None:
        raise ValueError("Capture is empty")
    span = last["elapsedSeconds"] - first["elapsedSeconds"] if samples else 0
    return {
        "source": str(path.resolve()), "source_sha256": digest.hexdigest(), "schema_version": 1,
        "map_id": header["mapId"], "scenario": header["scenario"], "build_guid": header["buildGuid"],
        "known_map_policy": header["mapId"] in MAP_CAPS,
        "recording_complete": footer is not None and samples > 0, "samples": samples,
        "sample_span_seconds": span, "thirty_minute_sample_span": span >= 1800,
        "observed_active_seconds": active_seconds, "last_attempt": last["attempt"] if samples else None,
        "sampling_gap_count": gaps, "largest_gap_seconds": largest_gap, "phase_samples": phases,
        "bound_violation_samples": violations, "cap_mismatch_samples": mismatches,
        "violating_fields": sorted(violating_fields), "sample_maxima": maxima,
        "managed_heap_min_bytes": minimum_heap,
        "managed_heap_delta_bytes": last["managedHeapBytes"] - first["managedHeapBytes"] if samples else None,
        "performance_acceptance": "unverified",
        "limitations": ["No CPU/GPU timings, p95, p99, total RAM or VRAM data",
                        "Sample span includes pauses and menus; it is not active play duration",
                        "Active time sums only observed intervals within each attempt",
                        "One-second samples can miss transient bounds violations",
                        "Heap growth alone neither proves nor disproves a memory leak",
                        "Map ID and scenario do not establish actual scan or headset provenance"],
    }


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("capture", type=Path)
    parser.add_argument("--output", type=Path, help="Create a new summary JSON; never overwrite")
    args = parser.parse_args()
    try:
        result = analyze(args.capture)
        content = json.dumps(result, ensure_ascii=False, indent=2, allow_nan=False) + "\n"
        if args.output:
            with args.output.open("x", encoding="utf-8") as stream:
                stream.write(content)
        else:
            print(content, end="")
    except (ValueError, OSError) as error:
        print(f"Diagnostics analysis failed: {error}", file=sys.stderr)
        return 2
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
