"""Shared geometry and design checks for the parametric nacelle model.

The module intentionally has no CAD-kernel dependency.  It can therefore be used
for unit tests and design-space validation before CadQuery is installed.
"""
from __future__ import annotations

from dataclasses import dataclass
from pathlib import Path
from typing import Any, Iterable
import json
import math

import yaml


ROOT = Path(__file__).resolve().parents[1]
DEFAULT_CONFIG = ROOT / "config" / "nacelle.yaml"


class GeometryError(ValueError):
    """Raised when the design inputs are internally inconsistent."""


def load_config(path: str | Path = DEFAULT_CONFIG) -> dict[str, Any]:
    with Path(path).open("r", encoding="utf-8") as stream:
        data = yaml.safe_load(stream)
    if not isinstance(data, dict):
        raise GeometryError("The configuration root must be a mapping.")
    return data


def lerp(a: float, b: float, t: float) -> float:
    return a + (b - a) * t


def local_wing_geometry(cfg: dict[str, Any]) -> dict[str, float]:
    wing = cfg["wing"]
    semi_span = wing["span_m"] / 2.0
    eta = abs(wing["motor_y_m"]) / semi_span
    if not 0.0 <= eta <= 1.0:
        raise GeometryError(f"Motor span station eta={eta:.3f} lies outside the wing.")

    chord = lerp(wing["root_chord_m"], wing["tip_chord_m"], eta)
    tc = lerp(wing["root_thickness_ratio"], wing["tip_thickness_ratio"], eta)
    thickness = chord * tc
    leading_edge_x = 0.25 * wing["root_chord_m"] - 0.25 * chord
    return {
        "semi_span_m": semi_span,
        "eta_motor": eta,
        "local_chord_m": chord,
        "local_thickness_ratio": tc,
        "local_max_thickness_m": thickness,
        "local_leading_edge_offset_from_root_m": leading_edge_x,
        "local_trailing_edge_x_m": chord,
        "flap_hinge_x_m": chord * (1.0 - wing["flap"]["chord_ratio"]),
        "spoiler_x_start_m": chord * wing["spoiler"]["x_over_c_in"],
        "spoiler_x_end_m": chord * wing["spoiler"]["x_over_c_out"],
    }


def ellipse_area(width: float, height: float) -> float:
    return math.pi * width * height / 4.0


def equivalent_diameter(area: float) -> float:
    if area <= 0.0:
        raise GeometryError("Area must be positive.")
    return math.sqrt(4.0 * area / math.pi)


def nacelle_station_interpolate(cfg: dict[str, Any], x_m: float) -> dict[str, float]:
    stations = cfg["nacelle"]["stations"]
    if x_m < stations[0]["x_m"] or x_m > stations[-1]["x_m"]:
        raise GeometryError(f"x={x_m:.3f} m lies outside the nacelle station range.")
    for first, second in zip(stations[:-1], stations[1:]):
        if first["x_m"] <= x_m <= second["x_m"]:
            dx = second["x_m"] - first["x_m"]
            t = 0.0 if dx == 0 else (x_m - first["x_m"]) / dx
            return {
                "x_m": x_m,
                "width_m": lerp(first["width_m"], second["width_m"], t),
                "height_m": lerp(first["height_m"], second["height_m"], t),
                "z_center_m": lerp(first["z_center_m"], second["z_center_m"], t),
            }
    return dict(stations[-1])


def naca230_mean_line(x: float) -> tuple[float, float]:
    """Return camber ordinate and slope for the NACA 230 mean line.

    The constants are the standard non-reflexed NACA five-digit 230-series
    values. Coordinates are normalized by chord.
    """
    m = 0.2025
    k1 = 15.957
    if x < m:
        yc = k1 / 6.0 * (x**3 - 3.0 * m * x**2 + m**2 * (3.0 - m) * x)
        dy = k1 / 6.0 * (3.0 * x**2 - 6.0 * m * x + m**2 * (3.0 - m))
    else:
        yc = k1 * m**3 / 6.0 * (1.0 - x)
        dy = -k1 * m**3 / 6.0
    return yc, dy


def naca_thickness(x: float, thickness_ratio: float) -> float:
    # Closed trailing edge coefficient (-0.1036) avoids a finite trailing edge gap.
    return 5.0 * thickness_ratio * (
        0.2969 * math.sqrt(max(x, 0.0))
        - 0.1260 * x
        - 0.3516 * x**2
        + 0.2843 * x**3
        - 0.1036 * x**4
    )


def naca230_coordinates(
    chord_m: float,
    thickness_ratio: float,
    points_per_surface: int = 80,
) -> list[tuple[float, float]]:
    if chord_m <= 0.0 or not 0.05 <= thickness_ratio <= 0.30:
        raise GeometryError("Invalid airfoil chord or thickness ratio.")
    if points_per_surface < 20:
        raise GeometryError("Use at least 20 points per airfoil surface.")

    # Cosine spacing clusters points at the leading and trailing edges.
    xs = [0.5 * (1.0 - math.cos(math.pi * i / (points_per_surface - 1))) for i in range(points_per_surface)]
    upper: list[tuple[float, float]] = []
    lower: list[tuple[float, float]] = []
    for x in xs:
        yc, dy = naca230_mean_line(x)
        theta = math.atan(dy)
        yt = naca_thickness(x, thickness_ratio)
        upper.append(((x - yt * math.sin(theta)) * chord_m, (yc + yt * math.cos(theta)) * chord_m))
        lower.append(((x + yt * math.sin(theta)) * chord_m, (yc - yt * math.cos(theta)) * chord_m))
    return list(reversed(upper)) + lower[1:-1]


@dataclass(frozen=True)
class CheckResult:
    name: str
    passed: bool
    actual: float | str
    limit: float | str
    message: str

    def as_dict(self) -> dict[str, Any]:
        return {
            "name": self.name,
            "passed": self.passed,
            "actual": self.actual,
            "limit": self.limit,
            "message": self.message,
        }


def run_design_checks(cfg: dict[str, Any]) -> list[CheckResult]:
    wing = local_wing_geometry(cfg)
    engine = cfg["engine_envelope"]
    nacelle = cfg["nacelle"]
    intake = cfg["intake"]
    propeller = cfg["propeller"]
    gear = cfg["landing_gear_bay"]

    engine_mid_x = 0.5 * (engine["engine_front_x_m"] + engine["engine_aft_x_m"])
    station = nacelle_station_interpolate(cfg, engine_mid_x)
    radial_clearance = min(station["width_m"], station["height_m"]) / 2.0 - engine["max_diameter_m"] / 2.0

    capture_geom = ellipse_area(intake["lip_width_m"], intake["lip_height_m"])
    diffuser_area = ellipse_area(intake["diffuser_exit_width_m"], intake["diffuser_exit_height_m"])
    capture_error = abs(capture_geom - intake["capture_area_m2"]) / intake["capture_area_m2"]

    results = [
        CheckResult(
            "motor_span_station",
            0.20 <= wing["eta_motor"] <= 0.40,
            wing["eta_motor"],
            "0.20 <= eta <= 0.40",
            "Motor placement remains in the reference ATR-like inboard wing region.",
        ),
        CheckResult(
            "engine_shell_radial_clearance",
            radial_clearance >= nacelle["minimum_engine_shell_clearance_m"],
            radial_clearance,
            nacelle["minimum_engine_shell_clearance_m"],
            "Minimum preliminary radial clearance between engine envelope and outer loft.",
        ),
        CheckResult(
            "propeller_nacelle_radial_clearance",
            (propeller["diameter_m"] - nacelle["stations"][0]["height_m"]) / 2.0 >= 1.25,
            (propeller["diameter_m"] - nacelle["stations"][0]["height_m"]) / 2.0,
            1.25,
            "Radial separation between the propeller tip and forward nacelle contour.",
        ),
        CheckResult(
            "intake_capture_area_geometry",
            capture_error <= 0.05,
            capture_geom,
            intake["capture_area_m2"],
            "Elliptical lip geometry should match the declared capture area within 5%.",
        ),
        CheckResult(
            "intake_diffuser_area_ratio",
            abs(diffuser_area / capture_geom - intake["area_ratio_exit_to_capture"]) <= 0.05,
            diffuser_area / capture_geom,
            intake["area_ratio_exit_to_capture"],
            "Geometric exit-to-capture area ratio matches the declared flowpath target.",
        ),
        CheckResult(
            "intake_area_progression",
            ellipse_area(intake["throat_width_m"], intake["throat_height_m"]) < diffuser_area < capture_geom,
            f"Ath={ellipse_area(intake['throat_width_m'], intake['throat_height_m']):.3f}, Aex={diffuser_area:.3f}, Acap={capture_geom:.3f}",
            "Ath < Aex < Acap",
            "The throat contracts the capture streamtube and the downstream duct diffuses again.",
        ),
        CheckResult(
            "firewall_aft_of_engine",
            engine["firewall_x_m"] >= engine["engine_aft_x_m"] + 0.05,
            engine["firewall_x_m"] - engine["engine_aft_x_m"],
            0.05,
            "Firewall must remain aft of the preliminary engine envelope.",
        ),
        CheckResult(
            "gear_bay_aft_of_firewall",
            (not gear["enabled"]) or gear["x_front_m"] > engine["firewall_x_m"],
            gear["x_front_m"],
            engine["firewall_x_m"],
            "The main landing gear bay is separated from the engine/fire zone.",
        ),
        CheckResult(
            "nacelle_covers_wing_chord",
            nacelle["aft_x_m"] >= wing["local_trailing_edge_x_m"],
            nacelle["aft_x_m"],
            wing["local_trailing_edge_x_m"],
            "ATR-like gear nacelle extends at least to the local wing trailing edge.",
        ),
    ]
    return results


def derived_dimensions(cfg: dict[str, Any]) -> dict[str, Any]:
    wing = local_wing_geometry(cfg)
    intake = cfg["intake"]
    results = run_design_checks(cfg)
    return {
        "project": cfg["project"],
        "wing_local": wing,
        "nacelle_total_length_m": cfg["nacelle"]["aft_x_m"] - cfg["nacelle"]["front_x_m"],
        "propeller_radius_m": cfg["propeller"]["diameter_m"] / 2.0,
        "intake_lip_geometric_area_m2": ellipse_area(intake["lip_width_m"], intake["lip_height_m"]),
        "intake_exit_geometric_area_m2": ellipse_area(
            intake["diffuser_exit_width_m"], intake["diffuser_exit_height_m"]
        ),
        "checks": [result.as_dict() for result in results],
        "all_checks_passed": all(result.passed for result in results),
    }


def write_derived_json(cfg: dict[str, Any], destination: str | Path) -> None:
    destination = Path(destination)
    destination.parent.mkdir(parents=True, exist_ok=True)
    destination.write_text(json.dumps(derived_dimensions(cfg), indent=2, ensure_ascii=False), encoding="utf-8")


def failed_checks(results: Iterable[CheckResult]) -> list[CheckResult]:
    return [result for result in results if not result.passed]
