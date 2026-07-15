from __future__ import annotations

import math
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT / "src"))

from geometry import (  # noqa: E402
    derived_dimensions,
    ellipse_area,
    load_config,
    local_wing_geometry,
    naca230_coordinates,
    run_design_checks,
)


def test_local_wing_geometry_matches_reported_integration_station() -> None:
    cfg = load_config()
    local = local_wing_geometry(cfg)
    assert local["eta_motor"] == pytest_approx(4.30 / 14.75, rel=1e-10)
    assert local["local_chord_m"] == pytest_approx(2.9593, rel=2e-4)
    assert local["local_thickness_ratio"] == pytest_approx(0.14125, rel=2e-4)
    assert local["flap_hinge_x_m"] == pytest_approx(1.9235, rel=3e-4)


def test_declared_capture_area_matches_lip_geometry() -> None:
    cfg = load_config()
    intake = cfg["intake"]
    area = ellipse_area(intake["lip_width_m"], intake["lip_height_m"])
    assert abs(area - intake["capture_area_m2"]) / intake["capture_area_m2"] < 0.01


def test_all_preliminary_geometry_checks_pass() -> None:
    cfg = load_config()
    failures = [check.name for check in run_design_checks(cfg) if not check.passed]
    assert failures == []
    assert derived_dimensions(cfg)["all_checks_passed"] is True


def test_naca_230_section_is_closed_without_invalid_points() -> None:
    coordinates = naca230_coordinates(2.9593, 0.14125, 80)
    assert len(coordinates) == 158
    assert all(math.isfinite(x) and math.isfinite(z) for x, z in coordinates)
    xs = [point[0] for point in coordinates]
    assert min(xs) > -0.01
    assert max(xs) < 2.97


def pytest_approx(expected: float, *, rel: float):
    # Avoid importing pytest in production modules while keeping readable tests.
    import pytest

    return pytest.approx(expected, rel=rel)
