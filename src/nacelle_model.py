#!/usr/bin/env python3
"""Build the preliminary full-scale nacelle as parametric CadQuery solids.

This is an integration model, not a production definition.  It deliberately keeps
engine, intake, exhaust, firewall, wing and landing-gear bay as separate named
solids so each discipline can replace the preliminary envelope independently.
"""
from __future__ import annotations

import argparse
from pathlib import Path
from typing import Any, Iterable

try:
    import cadquery as cq
except ImportError as exc:  # pragma: no cover - exercised in GitHub Actions
    raise SystemExit(
        "CadQuery is required. Install with: python -m pip install -r requirements.txt"
    ) from exc

from geometry import (
    DEFAULT_CONFIG,
    failed_checks,
    load_config,
    local_wing_geometry,
    naca230_coordinates,
    run_design_checks,
    write_derived_json,
)


def elliptical_wire_x(x: float, width: float, height: float, y: float = 0.0, z: float = 0.0) -> cq.Wire:
    """Create an ellipse in a plane normal to the aircraft x axis."""
    if width <= 0.0 or height <= 0.0:
        raise ValueError("Ellipse dimensions must be positive.")
    return cq.Workplane("YZ", origin=(x, y, z)).ellipse(width / 2.0, height / 2.0).val()


def loft_elliptical_sections(sections: Iterable[dict[str, float]]) -> cq.Shape:
    wires = [
        elliptical_wire_x(
            section["x_m"],
            section["width_m"],
            section["height_m"],
            section.get("y_center_m", 0.0),
            section.get("z_center_m", 0.0),
        )
        for section in sections
    ]
    return cq.Solid.makeLoft(wires, False)


def shrink_sections(
    sections: list[dict[str, float]],
    thickness: float,
    end_cap: float = 0.025,
) -> list[dict[str, float]]:
    inner: list[dict[str, float]] = []
    for index, section in enumerate(sections):
        x = section["x_m"]
        if index == 0:
            x += end_cap
        elif index == len(sections) - 1:
            x -= end_cap
        width = section["width_m"] - 2.0 * thickness
        height = section["height_m"] - 2.0 * thickness
        if width <= 0.0 or height <= 0.0:
            raise ValueError("Shell thickness collapses an inner loft section.")
        inner.append(
            {
                "x_m": x,
                "width_m": width,
                "height_m": height,
                "y_center_m": section.get("y_center_m", 0.0),
                "z_center_m": section.get("z_center_m", 0.0),
            }
        )
    return inner


def make_engine_envelope(cfg: dict[str, Any]) -> cq.Shape:
    engine = cfg["engine_envelope"]
    main = cq.Solid.makeCylinder(
        engine["max_diameter_m"] / 2.0,
        engine["overall_length_m"],
        cq.Vector(engine["engine_front_x_m"], 0, 0),
        cq.Vector(1, 0, 0),
    )
    gearbox = cq.Solid.makeCylinder(
        engine["gearbox_diameter_m"] / 2.0,
        engine["gearbox_length_m"],
        cq.Vector(engine["engine_front_x_m"] - 0.08, 0, 0),
        cq.Vector(1, 0, 0),
    )
    accessories = cq.Solid.makeCylinder(
        engine["max_diameter_m"] / 2.0 + engine["accessory_clearance_radial_m"],
        0.58,
        cq.Vector(engine["engine_aft_x_m"] - 0.58, 0, -0.02),
        cq.Vector(1, 0, 0),
    )
    return main.fuse(gearbox).fuse(accessories).clean()


def make_spinner_and_prop_disk(cfg: dict[str, Any]) -> tuple[cq.Shape, cq.Shape]:
    prop = cfg["propeller"]
    x_plane = cfg["engine_envelope"]["propeller_plane_x_m"]
    spinner = cq.Solid.makeCone(
        prop["spinner_max_diameter_m"] / 2.0,
        0.045,
        prop["spinner_length_m"],
        cq.Vector(x_plane, 0, 0),
        cq.Vector(-1, 0, 0),
    )
    disk = cq.Solid.makeCylinder(
        prop["diameter_m"] / 2.0,
        prop["disk_thickness_m"],
        cq.Vector(x_plane - prop["disk_thickness_m"] / 2.0, 0, 0),
        cq.Vector(1, 0, 0),
    )
    return spinner, disk


def make_intake(cfg: dict[str, Any]) -> tuple[cq.Shape, cq.Shape]:
    intake = cfg["intake"]
    lip = cfg["nacelle"]["inlet_lip_thickness_m"]
    outer_sections = [
        {
            "x_m": intake["lip_x_m"],
            "width_m": intake["lip_width_m"] + 2 * lip,
            "height_m": intake["lip_height_m"] + 2 * lip,
            "z_center_m": intake["lip_z_center_m"],
        },
        {
            "x_m": intake["throat_x_m"],
            "width_m": intake["throat_width_m"] + 2 * lip,
            "height_m": intake["throat_height_m"] + 2 * lip,
            "z_center_m": intake["lip_z_center_m"] + 0.04,
        },
        {
            "x_m": intake["diffuser_exit_x_m"],
            "width_m": intake["diffuser_exit_width_m"] + 2 * lip,
            "height_m": intake["diffuser_exit_height_m"] + 2 * lip,
            "z_center_m": intake["diffuser_exit_z_center_m"],
        },
    ]
    flow_sections = [
        {
            "x_m": intake["lip_x_m"] - 0.025,
            "width_m": intake["lip_width_m"],
            "height_m": intake["lip_height_m"],
            "z_center_m": intake["lip_z_center_m"],
        },
        {
            "x_m": intake["throat_x_m"],
            "width_m": intake["throat_width_m"],
            "height_m": intake["throat_height_m"],
            "z_center_m": intake["lip_z_center_m"] + 0.04,
        },
        {
            "x_m": intake["diffuser_exit_x_m"] + 0.18,
            "width_m": intake["diffuser_exit_width_m"],
            "height_m": intake["diffuser_exit_height_m"],
            "z_center_m": intake["diffuser_exit_z_center_m"],
        },
    ]
    outer = loft_elliptical_sections(outer_sections)
    flow = loft_elliptical_sections(flow_sections)
    duct_shell = outer.cut(flow).clean()
    return duct_shell, flow


def make_exhaust(cfg: dict[str, Any]) -> tuple[cq.Shape, cq.Shape]:
    exhaust = cfg["exhaust"]
    sign = 1.0 if exhaust["side"].lower() == "outboard" else -1.0
    wall = exhaust["wall_thickness_m"]
    inner_sections = [
        {
            "x_m": exhaust["duct_start_x_m"],
            "width_m": 0.25,
            "height_m": 0.20,
            "y_center_m": sign * 0.28,
            "z_center_m": 0.02,
        },
        {
            "x_m": exhaust["outlet_x_m"],
            "width_m": exhaust["outlet_width_m"],
            "height_m": exhaust["outlet_height_m"],
            "y_center_m": sign * abs(exhaust["outlet_y_m"]),
            "z_center_m": exhaust["outlet_z_m"],
        },
        {
            "x_m": exhaust["outlet_x_m"] + exhaust["nozzle_length_m"],
            "width_m": exhaust["outlet_width_m"] * 0.92,
            "height_m": exhaust["outlet_height_m"] * 0.88,
            "y_center_m": sign * (abs(exhaust["outlet_y_m"]) + 0.06),
            "z_center_m": exhaust["outlet_z_m"],
        },
    ]
    outer_sections = [
        {
            **section,
            "width_m": section["width_m"] + 2 * wall,
            "height_m": section["height_m"] + 2 * wall,
        }
        for section in inner_sections
    ]
    flow = loft_elliptical_sections(inner_sections)
    shell = loft_elliptical_sections(outer_sections).cut(flow).clean()
    return shell, flow


def make_wing_reference(cfg: dict[str, Any]) -> cq.Shape:
    wing = cfg["wing"]
    local = local_wing_geometry(cfg)
    coords = naca230_coordinates(local["local_chord_m"], local["local_thickness_ratio"], 100)
    profile = cq.Workplane("XZ").polyline(coords).close()
    solid = profile.extrude(wing["local_section_span_m"] / 2.0, both=True)
    solid = solid.rotate((0, 0, 0), (0, 1, 0), -wing["wing_incidence_deg"])
    return solid.translate((0, 0, wing["local_chord_line_z_m"])).val()


def make_gear_bay(cfg: dict[str, Any]) -> cq.Shape:
    gear = cfg["landing_gear_bay"]
    length = gear["x_aft_m"] - gear["x_front_m"]
    bay = (
        cq.Workplane("XY")
        .box(length, gear["width_m"], gear["height_m"], centered=(True, True, True))
        .edges()
        .fillet(min(0.08, gear["height_m"] * 0.12))
        .val()
    )
    return bay.translate(((gear["x_front_m"] + gear["x_aft_m"]) / 2.0, 0, gear["z_center_m"]))


def annulus_x(x: float, outer_d: float, inner_d: float, thickness: float) -> cq.Shape:
    outer = cq.Solid.makeCylinder(
        outer_d / 2.0,
        thickness,
        cq.Vector(x - thickness / 2.0, 0, 0),
        cq.Vector(1, 0, 0),
    )
    inner = cq.Solid.makeCylinder(
        inner_d / 2.0,
        thickness + 0.01,
        cq.Vector(x - thickness / 2.0 - 0.005, 0, 0),
        cq.Vector(1, 0, 0),
    )
    return outer.cut(inner)


def make_mounts_and_firewall(cfg: dict[str, Any]) -> tuple[cq.Shape, cq.Shape, cq.Shape]:
    mounts = cfg["mounts"]
    forward = annulus_x(
        mounts["forward_ring_x_m"],
        mounts["ring_outer_diameter_m"],
        mounts["ring_inner_diameter_m"],
        mounts["ring_thickness_m"],
    )
    aft = annulus_x(
        mounts["aft_ring_x_m"],
        mounts["ring_outer_diameter_m"],
        mounts["ring_inner_diameter_m"],
        mounts["ring_thickness_m"],
    )
    firewall_x = cfg["engine_envelope"]["firewall_x_m"]
    firewall = cq.Solid.makeCylinder(
        mounts["ring_outer_diameter_m"] / 2.0,
        0.012,
        cq.Vector(firewall_x - 0.006, 0, 0),
        cq.Vector(1, 0, 0),
    )
    return forward, aft, firewall


def make_nacelle(cfg: dict[str, Any]) -> dict[str, cq.Shape]:
    checks = run_design_checks(cfg)
    failures = failed_checks(checks)
    if failures:
        names = ", ".join(check.name for check in failures)
        raise ValueError(f"Geometry checks failed before CAD generation: {names}")

    stations = cfg["nacelle"]["stations"]
    outer = loft_elliptical_sections(stations)
    inner = loft_elliptical_sections(shrink_sections(stations, cfg["nacelle"]["shell_thickness_m"]))
    shell = outer.cut(inner)

    intake_shell, intake_flow = make_intake(cfg)
    exhaust_shell, exhaust_flow = make_exhaust(cfg)

    # The intake is part of the aerodynamic outer mold line; its internal flow path
    # cuts through the nacelle shell. The exhaust remains a replaceable duct module.
    shell = shell.fuse(intake_shell).cut(intake_flow).cut(exhaust_flow).clean()

    engine = make_engine_envelope(cfg)
    spinner, prop_disk = make_spinner_and_prop_disk(cfg)
    wing = make_wing_reference(cfg)
    gear_bay = make_gear_bay(cfg)
    forward_mount, aft_mount, firewall = make_mounts_and_firewall(cfg)

    return {
        "nacelle_shell": shell,
        "outer_mold_line": outer.fuse(intake_shell).clean(),
        "intake_duct": intake_shell,
        "intake_flowpath": intake_flow,
        "exhaust_duct": exhaust_shell,
        "exhaust_flowpath": exhaust_flow,
        "engine_envelope": engine,
        "spinner": spinner,
        "propeller_disk": prop_disk,
        "wing_reference": wing,
        "gear_bay_envelope": gear_bay,
        "forward_engine_mount": forward_mount,
        "aft_engine_mount": aft_mount,
        "firewall": firewall,
    }


def export_models(models: dict[str, cq.Shape], cfg: dict[str, Any], output_dir: Path) -> None:
    output_dir.mkdir(parents=True, exist_ok=True)
    for name, shape in models.items():
        # The model is authored in metres. STEP files explicitly declare metres.
        cq.exporters.export(shape, str(output_dir / f"{name}.step"), unit="M")
        if name.endswith("flowpath") or name == "propeller_disk":
            # Reference volumes are STEP-only to avoid presenting them as printable parts.
            continue
        if name in {"nacelle_shell", "outer_mold_line", "intake_duct", "exhaust_duct", "spinner"}:
            # STL carries no unit metadata. Scale metre geometry to millimetre numbers so
            # standard CAD/mesh applications import the full-scale model correctly.
            tolerance_mm = cfg["model_options"]["export_stl_tolerance_mm"]
            cq.exporters.export(
                shape.scale(1000.0),
                str(output_dir / f"{name}.stl"),
                tolerance=tolerance_mm,
                angularTolerance=0.1,
            )

    assembly = cq.Assembly(name="PW127XT_M_Nacelle_Integration")
    colors = {
        "nacelle_shell": cq.Color(0.82, 0.84, 0.87, 0.65),
        "engine_envelope": cq.Color(0.78, 0.30, 0.05, 0.50),
        "intake_duct": cq.Color(0.05, 0.36, 0.58, 0.85),
        "exhaust_duct": cq.Color(0.32, 0.32, 0.34, 0.90),
        "wing_reference": cq.Color(0.55, 0.60, 0.63, 0.35),
        "gear_bay_envelope": cq.Color(0.42, 0.28, 0.58, 0.35),
        "firewall": cq.Color(0.82, 0.15, 0.12, 0.85),
        "spinner": cq.Color(0.12, 0.12, 0.12, 1.0),
        "propeller_disk": cq.Color(0.15, 0.15, 0.15, 0.10),
    }
    for name in [
        "nacelle_shell",
        "engine_envelope",
        "intake_duct",
        "exhaust_duct",
        "wing_reference",
        "gear_bay_envelope",
        "forward_engine_mount",
        "aft_engine_mount",
        "firewall",
        "spinner",
        "propeller_disk",
    ]:
        assembly.add(models[name], name=name, color=colors.get(name, cq.Color(0.6, 0.6, 0.6)))
    assembly.export(
        str(output_dir / "nacelle_integration_assembly.step"),
        exportType="STEP",
        unit="M",
    )


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--config", type=Path, default=DEFAULT_CONFIG)
    parser.add_argument("--output-dir", type=Path, default=Path("outputs/cad"))
    args = parser.parse_args()

    cfg = load_config(args.config)
    models = make_nacelle(cfg)
    export_models(models, cfg, args.output_dir)
    write_derived_json(cfg, args.output_dir.parent / "derived_dimensions.json")
    print(f"Exported {len(models)} named CAD objects to {args.output_dir}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
