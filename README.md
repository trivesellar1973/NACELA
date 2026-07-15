# NACELA — generador nativo para SOLIDWORKS 2021

Proyecto C#/.NET Framework que controla SOLIDWORKS mediante su API COM y construye una nacela turbohélice mediante operaciones nativas editables.

No usa FreeCAD, CadQuery, STEP, IGES, STL, mallas ni cuerpos importados.

## Revisión activa: B1

`B1` es una reconstrucción completa desde cero. No reutiliza la geometría A0, A1 ni A2.

La forma se planteó a partir de la referencia CFD aportada por el usuario:

- morro compacto alrededor del gearbox;
- volumen máximo adelantado;
- costados tensos y menos esféricos;
- panza profunda;
- terminación posterior puntiaguda;
- fairing superior ancho con techo casi plano;
- toma inferior tipo scoop rectangular ovalado;
- dos tomas laterales pequeñas con housing;
- dos escapes altos con housing térmico y nozzle interior;
- capós laterales grandes y paneles funcionales.

## Requisitos de la hoja 03_CONFIGURACION

```text
L_nac = 2.556 m
W_nac = 0.920 m
H_nac = 1.340 m
y_motor = +4.300 m
c_local = 2.958955 m
plano de hélice = 1.000 m delante del borde de ataque
x_nac_aft respecto del BA = +1.556 m
A_c_total = 0.1846585 m2
envolvente motor = 2.130 x 0.720 x 0.840 m
```

El extremo trasero queda aproximadamente en:

```text
x/c = 1.556 / 2.958955 = 0.526
```

## Sistema de coordenadas

La pieza se crea localmente:

- origen en el plano de hélice;
- `+X` hacia atrás;
- `+Y` lateral;
- `+Z` hacia arriba;
- eje del motor en `Y=0`, `Z=0`.

Después se inserta en el ensamblaje con:

```text
X = -1.739330 m
Y = +4.300000 m
Z = -0.880000 m
```

## Stage 1 — OML e integración

Archivo:

```text
NACELA_DERECHA_B1_STAGE1_OML.SLDPRT
```

Genera:

- 12 secciones OML completamente nuevas;
- una spline cerrada por sección;
- cuatro curvas guía;
- loft sólido principal;
- envolvente interna oculta del PW127XT-M;
- boss y transición del gearbox;
- hueco de eje;
- saddle fairing de techo casi plano;
- vistas lateral, frontal, planta e isométrica.

## Stage 2 — admisión, tomas laterales y escape

Archivo:

```text
NACELA_DERECHA_B1_STAGE2_SISTEMAS.SLDPRT
```

Genera:

- scoop inferior exterior fusionado con la panza;
- boca rounded rectangle de `0.620 x 0.310 m`;
- radios de esquina de `0.085 m`;
- ducto ascendente por loft hasta interfaz `0.420 x 0.240 m`;
- dos tomas laterales redondeadas;
- dos escapes altos con housing y nozzle interior;
- filetes de labios cuando el kernel lo permite.

El área aproximada de la toma rectangular redondeada es superior al mínimo de `0.1846585 m2`.

## Stage 3 — capós y paneles

Archivo:

```text
NACELA_DERECHA_B1_STAGE3_FINAL.SLDPRT
```

Genera:

- un capó lateral exterior grande;
- un capó lateral interior grande;
- líneas laterales de firewall;
- panel exterior de servicio;
- rebajes poco profundos en la misma piel, no placas añadidas.

## Ensamblaje de revisión

```text
ALA_REVIEW_NACELA_DER_B1.SLDASM
```

La nacela se inserta mediante una transformación global explícita y queda fijada en la estación `Y=+4.300 m`.

## Uso

Actualizar el proyecto:

```text
01_ACTUALIZAR.bat
```

Preparar el ala la primera vez:

```text
00_PREPARAR_ALA.bat
```

Generar el modelo B1 completo:

```text
02_EJECUTAR_REVISION.bat
```

Abrir el último resultado:

```text
03_ABRIR_ULTIMO_RESULTADO.bat
```

Generar solamente Stage 1:

```text
04_EJECUTAR_SOLO_OML.bat
```

Ejecutar explícitamente Stage 3:

```text
05_EJECUTAR_STAGE3_CAPOS.bat
```

## Resultados

```text
generated\B1\
```

Archivos principales:

```text
NACELA_DERECHA_B1_STAGE1_OML.SLDPRT
NACELA_DERECHA_B1_STAGE2_SISTEMAS.SLDPRT
NACELA_DERECHA_B1_STAGE3_FINAL.SLDPRT
ALA_REVIEW_NACELA_DER_B1.SLDASM
VALIDACION_B1_STAGE1.txt
VALIDACION_B1_STAGE2.txt
VALIDACION_B1_STAGE3.txt
```

También se generan BMP lateral, frontal, planta e isométrica.

## Código B1

La geometría activa está separada en:

```text
src/B1Config.cs
src/B1Geometry.cs
src/B1Stage1Builder.cs
src/B1Stage2Builder.cs
src/B1Stage3Builder.cs
src/B1AssemblyReviewBuilder.cs
```

Los builders anteriores fueron retirados del directorio `src`. La infraestructura general de conexión, guardado y operaciones API permanece en `SwSession.cs` y `SwGeometry.cs`.

## Alcance

B1 es una definición preliminar editable para integración y TFG. No sustituye un installation drawing OEM ni la validación estructural, térmica, de vibraciones, de distorsión de admisión o de flujo de escape.
