#!/usr/bin/env python3
"""Generate simple dimensioned SVG views from the authoritative YAML inputs."""
from __future__ import annotations

import argparse
from pathlib import Path
from html import escape

from geometry import DEFAULT_CONFIG, load_config, local_wing_geometry


SVG_HEADER = '<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 {w} {h}" width="{w}" height="{h}">\n'


def sx(x: float, xmin: float, scale: float, margin: float) -> float:
    return margin + (x - xmin) * scale


def sy(z: float, zmax: float, scale: float, margin: float) -> float:
    return margin + (zmax - z) * scale


def svg_attrs(attrs: dict[str, object]) -> str:
    def key(name: str) -> str:
        return "class" if name == "class_" else name.replace("_", "-")
    return " ".join(f'{key(k)}="{escape(str(v))}"' for k, v in attrs.items())


def polyline(points: list[tuple[float, float]], **attrs: object) -> str:
    attr = svg_attrs(attrs)
    pts = " ".join(f"{x:.1f},{y:.1f}" for x, y in points)
    return f'<polyline points="{pts}" {attr}/>'


def line(x1: float, y1: float, x2: float, y2: float, **attrs: object) -> str:
    attr = svg_attrs(attrs)
    return f'<line x1="{x1:.1f}" y1="{y1:.1f}" x2="{x2:.1f}" y2="{y2:.1f}" {attr}/>'


def text(x: float, y: float, value: str, **attrs: object) -> str:
    attr = svg_attrs(attrs)
    return f'<text x="{x:.1f}" y="{y:.1f}" {attr}>{escape(value)}</text>'


def ellipse(cx: float, cy: float, rx: float, ry: float, **attrs: object) -> str:
    attr = svg_attrs(attrs)
    return f'<ellipse cx="{cx:.1f}" cy="{cy:.1f}" rx="{rx:.1f}" ry="{ry:.1f}" {attr}/>'


def dimension_horizontal(x1: float, x2: float, y: float, label: str) -> list[str]:
    return [
        line(x1, y, x2, y, stroke="#111", stroke_width="1.3", marker_start="url(#arrow)", marker_end="url(#arrow)"),
        text((x1 + x2) / 2, y - 7, label, text_anchor="middle", font_size="13"),
    ]


def dimension_vertical(x: float, y1: float, y2: float, label: str) -> list[str]:
    return [
        line(x, y1, x, y2, stroke="#111", stroke_width="1.3", marker_start="url(#arrow)", marker_end="url(#arrow)"),
        text(x + 16, (y1 + y2) / 2, label, text_anchor="middle", font_size="13", transform=f"rotate(-90 {x+16:.1f} {(y1+y2)/2:.1f})"),
    ]


def defs() -> str:
    return """<defs>
  <marker id="arrow" markerWidth="7" markerHeight="7" refX="3.5" refY="3.5" orient="auto-start-reverse">
    <path d="M0,0 L7,3.5 L0,7 z" fill="#111"/>
  </marker>
  <style>
    .outline { fill:#f5f5f5; stroke:#111; stroke-width:2; }
    .reference { fill:none; stroke:#666; stroke-width:1.4; stroke-dasharray:7 5; }
    .duct { fill:none; stroke:#005a8d; stroke-width:2.2; }
    .engine { fill:none; stroke:#a13b00; stroke-width:2; stroke-dasharray:8 4; }
    .gear { fill:none; stroke:#684c8c; stroke-width:2; stroke-dasharray:5 4; }
    text { font-family:Arial,Helvetica,sans-serif; fill:#111; }
  </style>
</defs>\n"""


def side_view(cfg: dict, output: Path) -> None:
    stations = cfg["nacelle"]["stations"]
    wing = local_wing_geometry(cfg)
    x_min = cfg["nacelle"]["front_x_m"] - 0.35
    x_max = cfg["nacelle"]["aft_x_m"] + 0.35
    z_min = -1.15
    z_max = 1.25
    width, height, margin = 1180, 560, 60
    scale = min((width - 2 * margin) / (x_max - x_min), (height - 2 * margin) / (z_max - z_min))

    upper = [(sx(s["x_m"], x_min, scale, margin), sy(s["z_center_m"] + s["height_m"] / 2, z_max, scale, margin)) for s in stations]
    lower = [(sx(s["x_m"], x_min, scale, margin), sy(s["z_center_m"] - s["height_m"] / 2, z_max, scale, margin)) for s in reversed(stations)]
    outline = upper + lower + [upper[0]]

    parts = [SVG_HEADER.format(w=width, h=height), defs()]
    parts.append(polyline(outline, class_="outline"))

    wing_z = cfg["wing"]["local_chord_line_z_m"]
    wing_le = 0.0
    wing_te = wing["local_chord_m"]
    wing_t = wing["local_max_thickness_m"]
    wing_box = [
        (sx(wing_le, x_min, scale, margin), sy(wing_z + wing_t / 2, z_max, scale, margin)),
        (sx(wing_te, x_min, scale, margin), sy(wing_z + wing_t * 0.15, zmax= z_max, scale=scale, margin=margin)),
        (sx(wing_te, x_min, scale, margin), sy(wing_z - wing_t * 0.12, zmax= z_max, scale=scale, margin=margin)),
        (sx(wing_le, x_min, scale, margin), sy(wing_z - wing_t / 2, z_max, scale, margin)),
        (sx(wing_le, x_min, scale, margin), sy(wing_z + wing_t / 2, z_max, scale, margin)),
    ]
    parts.append(polyline(wing_box, class_="reference"))
    parts.append(text(sx(0.95, x_min, scale, margin), sy(wing_z + 0.32, z_max, scale, margin), f"Ala local c={wing['local_chord_m']:.3f} m", font_size="13"))

    engine = cfg["engine_envelope"]
    ex1, ex2 = engine["engine_front_x_m"], engine["engine_aft_x_m"]
    er = engine["max_diameter_m"] / 2
    eoutline = [
        (sx(ex1, x_min, scale, margin), sy(er, z_max, scale, margin)),
        (sx(ex2, x_min, scale, margin), sy(er, z_max, scale, margin)),
        (sx(ex2, x_min, scale, margin), sy(-er, z_max, scale, margin)),
        (sx(ex1, x_min, scale, margin), sy(-er, z_max, scale, margin)),
        (sx(ex1, x_min, scale, margin), sy(er, z_max, scale, margin)),
    ]
    parts.append(polyline(eoutline, class_="engine"))
    parts.append(text(sx(-0.50, x_min, scale, margin), sy(0.05, z_max, scale, margin), "Envolvente PW127XT-M", font_size="13", text_anchor="middle"))

    intake = cfg["intake"]
    duct_points = [
        (sx(intake["lip_x_m"], x_min, scale, margin), sy(intake["lip_z_center_m"], z_max, scale, margin)),
        (sx(intake["throat_x_m"], x_min, scale, margin), sy(intake["lip_z_center_m"] + 0.04, z_max, scale, margin)),
        (sx(intake["diffuser_exit_x_m"], x_min, scale, margin), sy(intake["diffuser_exit_z_center_m"], z_max, scale, margin)),
    ]
    parts.append(polyline(duct_points, class_="duct"))
    parts.append(text(sx(-0.65, x_min, scale, margin), sy(-0.74, z_max, scale, margin), "Toma inferior + difusor", font_size="13", fill="#005a8d"))

    gear = cfg["landing_gear_bay"]
    gx1, gx2 = gear["x_front_m"], gear["x_aft_m"]
    gz1, gz2 = gear["z_center_m"] - gear["height_m"] / 2, gear["z_center_m"] + gear["height_m"] / 2
    gear_box = [
        (sx(gx1, x_min, scale, margin), sy(gz2, z_max, scale, margin)),
        (sx(gx2, x_min, scale, margin), sy(gz2, z_max, scale, margin)),
        (sx(gx2, x_min, scale, margin), sy(gz1, z_max, scale, margin)),
        (sx(gx1, x_min, scale, margin), sy(gz1, z_max, scale, margin)),
        (sx(gx1, x_min, scale, margin), sy(gz2, z_max, scale, margin)),
    ]
    parts.append(polyline(gear_box, class_="gear"))
    parts.append(text(sx(2.5, x_min, scale, margin), sy(-0.42, z_max, scale, margin), "Reserva tren principal", font_size="13", fill="#684c8c", text_anchor="middle"))

    prop_x = sx(cfg["engine_envelope"]["propeller_plane_x_m"], x_min, scale, margin)
    prop_r = cfg["propeller"]["diameter_m"] / 2 * scale
    axis_y = sy(0.0, z_max, scale, margin)
    parts.append(line(prop_x, axis_y - prop_r, prop_x, axis_y + prop_r, class_="reference"))
    parts.append(text(prop_x - 8, axis_y - prop_r - 8, f"Disco helice Ø {cfg['propeller']['diameter_m']:.2f} m", font_size="13", text_anchor="end"))

    ydim = height - 25
    parts += dimension_horizontal(sx(cfg["nacelle"]["front_x_m"], x_min, scale, margin), sx(cfg["nacelle"]["aft_x_m"], x_min, scale, margin), ydim, f"L nacela = {cfg['nacelle']['aft_x_m']-cfg['nacelle']['front_x_m']:.2f} m")
    parts += dimension_vertical(width - 45, sy(cfg["nacelle"]["maximum_height_m"] / 2, z_max, scale, margin), sy(-cfg["nacelle"]["maximum_height_m"] / 2, z_max, scale, margin), f"H max = {cfg['nacelle']['maximum_height_m']:.2f} m")
    parts.append(text(30, 30, f"{cfg['project']['name']} - Vista lateral - Rev. {cfg['project']['revision']}", font_size="18", font_weight="bold"))
    parts.append("</svg>\n")
    output.write_text("".join(parts), encoding="utf-8")


def front_view(cfg: dict, output: Path) -> None:
    width, height = 760, 620
    scale = 135.0
    cx, cy = width / 2, height / 2 + 20
    nac_w = cfg["nacelle"]["maximum_width_m"] * scale
    nac_h = cfg["nacelle"]["maximum_height_m"] * scale
    prop_r = cfg["propeller"]["diameter_m"] / 2 * scale
    intake = cfg["intake"]

    parts = [SVG_HEADER.format(w=width, h=height), defs()]
    parts.append(ellipse(cx, cy, prop_r, prop_r, class_="reference"))
    parts.append(ellipse(cx, cy, nac_w / 2, nac_h / 2, class_="outline"))
    parts.append(ellipse(cx, cy + 0.35 * scale, intake["lip_width_m"] * scale / 2, intake["lip_height_m"] * scale / 2, fill="none", stroke="#005a8d", stroke_width="2.2"))
    for angle in range(0, 360, 60):
        import math
        a = math.radians(angle)
        parts.append(line(cx, cy, cx + prop_r * math.cos(a), cy + prop_r * math.sin(a), stroke="#555", stroke_width="3"))
    parts += dimension_horizontal(cx - nac_w / 2, cx + nac_w / 2, height - 35, f"W max = {cfg['nacelle']['maximum_width_m']:.2f} m")
    parts += dimension_vertical(width - 55, cy - nac_h / 2, cy + nac_h / 2, f"H max = {cfg['nacelle']['maximum_height_m']:.2f} m")
    parts.append(text(25, 30, f"Vista frontal - helice {cfg['propeller']['blade_count']} palas - Ø {cfg['propeller']['diameter_m']:.2f} m", font_size="18", font_weight="bold"))
    parts.append(text(cx, cy + 0.36 * scale, f"A captura {cfg['intake']['capture_area_m2']:.3f} m²", font_size="13", text_anchor="middle", fill="#005a8d"))
    parts.append("</svg>\n")
    output.write_text("".join(parts), encoding="utf-8")


def plan_view(cfg: dict, output: Path) -> None:
    width, height, margin = 1180, 520, 60
    x_min = cfg["nacelle"]["front_x_m"] - 0.3
    x_max = cfg["nacelle"]["aft_x_m"] + 0.3
    y_min, y_max = -1.25, 1.25
    scale = min((width - 2 * margin) / (x_max - x_min), (height - 2 * margin) / (y_max - y_min))
    stations = cfg["nacelle"]["stations"]
    upper = [(sx(s["x_m"], x_min, scale, margin), sy(s["width_m"] / 2, y_max, scale, margin)) for s in stations]
    lower = [(sx(s["x_m"], x_min, scale, margin), sy(-s["width_m"] / 2, y_max, scale, margin)) for s in reversed(stations)]
    outline = upper + lower + [upper[0]]

    wing = local_wing_geometry(cfg)
    parts = [SVG_HEADER.format(w=width, h=height), defs(), polyline(outline, class_="outline")]
    wing_span = cfg["wing"]["local_section_span_m"]
    y1, y2 = -wing_span / 2, wing_span / 2
    wing_poly = [
        (sx(0.0, x_min, scale, margin), sy(y2, y_max, scale, margin)),
        (sx(wing["local_chord_m"], x_min, scale, margin), sy(y2, y_max, scale, margin)),
        (sx(wing["local_chord_m"], x_min, scale, margin), sy(y1, y_max, scale, margin)),
        (sx(0.0, x_min, scale, margin), sy(y1, y_max, scale, margin)),
        (sx(0.0, x_min, scale, margin), sy(y2, y_max, scale, margin)),
    ]
    parts.append(polyline(wing_poly, class_="reference"))
    parts.append(line(sx(wing["flap_hinge_x_m"], x_min, scale, margin), sy(y1, y_max, scale, margin), sx(wing["flap_hinge_x_m"], x_min, scale, margin), sy(y2, y_max, scale, margin), stroke="#b00020", stroke_width="1.8", stroke_dasharray="8 5"))
    parts.append(text(sx(wing["flap_hinge_x_m"], x_min, scale, margin) + 10, sy(y2, y_max, scale, margin) - 12, "Linea de bisagra flap", font_size="13", fill="#b00020"))
    parts.append(line(sx(cfg["exhaust"]["duct_start_x_m"], x_min, scale, margin), sy(0.35, y_max, scale, margin), sx(cfg["exhaust"]["outlet_x_m"], x_min, scale, margin), sy(cfg["exhaust"]["outlet_y_m"], y_max, scale, margin), class_="duct"))
    parts.append(text(sx(0.95, x_min, scale, margin), sy(0.72, y_max, scale, margin), "Escape lateral exterior", font_size="13", fill="#005a8d"))
    parts.append(text(30, 30, f"Vista en planta - estacion motor y={cfg['wing']['motor_y_m']:.2f} m (eta={wing['eta_motor']:.3f})", font_size="18", font_weight="bold"))
    parts += dimension_horizontal(sx(0.0, x_min, scale, margin), sx(wing["local_chord_m"], x_min, scale, margin), height - 25, f"c local = {wing['local_chord_m']:.3f} m")
    parts.append("</svg>\n")
    output.write_text("".join(parts), encoding="utf-8")


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--config", type=Path, default=DEFAULT_CONFIG)
    parser.add_argument("--output-dir", type=Path, default=Path("docs/generated"))
    args = parser.parse_args()
    cfg = load_config(args.config)
    args.output_dir.mkdir(parents=True, exist_ok=True)
    side_view(cfg, args.output_dir / "nacelle_side.svg")
    front_view(cfg, args.output_dir / "nacelle_front.svg")
    plan_view(cfg, args.output_dir / "nacelle_plan.svg")
    print(f"SVG drawings written to {args.output_dir}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
