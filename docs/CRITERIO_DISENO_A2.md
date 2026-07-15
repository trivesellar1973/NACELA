# Criterio de diseño — NACELA A2

## 1. Objetivo

La revisión A2 toma como referencia visual una nacela turbohélice de transporte con vistas interior y exterior coloreadas por presión estática. No se intenta copiar un componente OEM exacto; se replica su lógica geométrica:

- cuerpo delantero redondeado;
- volumen máximo adelantado;
- panza profunda;
- toma principal inferior ancha;
- escapes laterales altos y enrasados;
- fairing superior ancho que continúa hacia el ala;
- boattail corto;
- pocas discontinuidades externas.

## 2. Datos obligatorios del TFG

| Parámetro | Valor |
|---|---:|
| Longitud nacela | 2.556 m |
| Ancho máximo | 0.920 m |
| Altura máxima | 1.340 m |
| Estación transversal | +4.300 m |
| Cuerda local | 2.958955 m |
| Frente respecto del BA | -1.000 m |
| Cola respecto del BA | +1.556 m |
| Área total de captura | 0.1846585 m² |
| Envolvente motor | 2.130 × 0.720 × 0.840 m |

La relación de posición posterior es:

```text
x_aft/c_local = 1.556 / 2.958955 = 0.526
```

## 3. Sistema local

La pieza se crea con el plano de hélice en `X=0`. La transformación de ensamblaje ubica después el componente en:

```text
X = -1.739330 m
Y = +4.300000 m
Z = -0.880000 m
```

El borde de ataque local del ala queda en `X_local=1.000 m`, lo que permite diseñar el saddle fairing alrededor de una referencia estable.

## 4. OML

Se usan 12 secciones cerradas. Cada sección es una sola spline, no cuatro cuadrantes. Los exponentes separados para laterales, corona y panza permiten:

- laterales más verticales en la zona máxima;
- techo redondeado pero contenido;
- panza más llena;
- convergencia posterior progresiva.

Cuatro guías controlan corona, panza, lateral exterior y lateral interior.

## 5. Integración con el ala

El fairing superior no es un pylon rectangular. Es un loft fusionado que:

- comienza antes del borde de ataque;
- alcanza su máxima anchura entre `X=1.30` y `1.65 m`;
- se mantiene alto debajo del intradós;
- converge antes de la cola de la nacela.

Su geometría se superpone al OML para generar un solo sólido.

## 6. Toma principal

La toma principal es inferior y no es una toma NACA. La boca elíptica adoptada es:

```text
0.650 × 0.362 m
A = π/4 × 0.650 × 0.362 = 0.1848 m²
```

Se construye con:

1. bolsillo exterior de labio;
2. boca real;
3. sección de captura;
4. transición por loft;
5. interfaz de motor de `0.420 × 0.260 m`.

## 7. Tomas NACA auxiliares

Las NACA pequeñas se reservan para ventilación o refrigeración de accesorios. No alimentan el compresor principal.

La base geométrica sigue el criterio del memorando NACA RM A7I30: paredes divergentes, rampa poco inclinada y relación ancho/profundidad elevada. La forma exacta deberá verificarse posteriormente con CFD.

Fuente primaria:

- NACA RM A7I30, *An Experimental Investigation of the Design Variables for NACA Submerged Duct Entrances*.
- https://ntrs.nasa.gov/api/citations/19930093809/downloads/19930093809.pdf

## 8. Escape

Cada salida se representa como una abertura lateral alta tipo D-shaped dentro de un bolsillo térmico. La abertura visible no adopta literalmente el diámetro equivalente preliminar de 0.60677 m, porque ese valor debe revisarse como área de flujo y no como diámetro exterior de una boquilla circular.

Dimensión visible A2:

```text
0.340 × 0.180 m
```

## 9. Mantenimiento

Stage 3 incluye dos grandes contornos laterales y un panel de aceite. Se ejecuta solo después de validar Stage 2 para evitar que los paneles oculten defectos de forma.

La arquitectura de una puerta interior y otra exterior con acceso lateral es coherente con instalaciones turbohélice regionales. La cinemática, bisagras y cierres quedan para una revisión posterior.

Referencia operacional primaria:

- UK AAIB, DHC-8-402 Q400 G-PRPC, engine access door event.
- https://assets.publishing.service.gov.uk/media/59afe6e440f0b6174109f635/DHC-8-402_Dash_8__Q400__G-PRPC_09-17.pdf

## 10. Estado

A2 es una geometría preliminar editable para integración del Capítulo 4. No sustituye:

- installation drawing OEM;
- análisis estructural;
- firewall y drenajes;
- validación de distorsión de admisión;
- cálculo térmico de escape;
- análisis de vibraciones;
- verificación de despeje de hélice y terreno.
