#!/usr/bin/env python3
"""Validate the preliminary nacelle configuration without requiring CadQuery."""
from __future__ import annotations

import argparse
from pathlib import Path
import sys

from geometry import DEFAULT_CONFIG, failed_checks, load_config, run_design_checks, write_derived_json


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--config", type=Path, default=DEFAULT_CONFIG)
    parser.add_argument("--json", type=Path, default=Path("outputs/derived_dimensions.json"))
    args = parser.parse_args()

    cfg = load_config(args.config)
    results = run_design_checks(cfg)
    print("\nPRELIMINARY NACELLE GEOMETRY CHECKS")
    print("=" * 72)
    for result in results:
        status = "PASS" if result.passed else "FAIL"
        actual = f"{result.actual:.4g}" if isinstance(result.actual, float) else str(result.actual)
        print(f"[{status}] {result.name:36s} actual={actual} limit={result.limit}")
        print(f"       {result.message}")

    write_derived_json(cfg, args.json)
    failures = failed_checks(results)
    if failures:
        print(f"\n{len(failures)} check(s) failed. CAD generation should be blocked.", file=sys.stderr)
        return 2
    print(f"\nAll checks passed. Derived data written to {args.json}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
