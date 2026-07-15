from __future__ import annotations

import importlib.util
import sys
from pathlib import Path

import pytest

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT / "src"))

pytestmark = pytest.mark.skipif(importlib.util.find_spec("cadquery") is None, reason="CadQuery not installed")


def test_all_named_cad_objects_build_and_have_positive_volume() -> None:
    from geometry import load_config
    from nacelle_model import make_nacelle

    models = make_nacelle(load_config())
    expected = {
        "nacelle_shell",
        "outer_mold_line",
        "intake_duct",
        "intake_flowpath",
        "exhaust_duct",
        "exhaust_flowpath",
        "engine_envelope",
        "spinner",
        "propeller_disk",
        "wing_reference",
        "gear_bay_envelope",
        "forward_engine_mount",
        "aft_engine_mount",
        "firewall",
    }
    assert set(models) == expected
    for name, shape in models.items():
        assert shape.isValid(), name
        assert shape.Volume() > 0.0, name

    bbox = models["outer_mold_line"].BoundingBox()
    assert bbox.xlen == pytest.approx(4.91, rel=0.03)
    assert bbox.ylen == pytest.approx(1.22, rel=0.03)
    assert bbox.zlen >= 1.40
