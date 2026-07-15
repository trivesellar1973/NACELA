# NACELA — generador nativo para SOLIDWORKS 2021

Generador C#/.NET Framework que controla SOLIDWORKS mediante la API COM y construye una nacela turbohélice con operaciones nativas editables.

No usa FreeCAD, CadQuery, STEP, IGES, STL, mallas ni cuerpos importados.

## Objetivo A2

La revisión `A2` reconstruye la nacela siguiendo una referencia visual de transporte turbohélice con:

- morro corto y redondeado;
- volumen principal robusto pero no esférico;
- hombros laterales definidos;
- panza profunda para accesorios;
- boattail posterior corto;
- fairing superior ancho tipo saddle;
- toma principal inferior integrada;
- dos escapes laterales altos, enrasados y dentro de bolsillos térmicos;
- dos entradas NACA pequeñas para refrigeración de accesorios;
- capós laterales y paneles funcionales disponibles como Stage 3 separado.

## Requisitos conservados de la hoja 03_CONFIGURACION

```text
L_nac = 2.556 m
W_nac = 0.920 m
H_nac = 1.340 m
y_motor = +4.300 m
c_local = 2.958955 m
x_nac_fwd relativo al BA = -1.000 m
x_nac_aft relativo al BA = +1.556 m
A_c_total = 0.1846585 m2
envolvente motor = 2.130 x 0.720 x 0.840 m
```

El extremo trasero queda aproximadamente en `x/c = 0.526`, dentro del límite de la configuración.

## Entrada CAD

El único modelo previo necesario para revisar la integración es el conjunto del ala:

```text
ALA_COMPLETA_MECANISMOS.SLDASM
ALA_FIJA_DERECHA.SLDPRT
ALA_FIJA_IZQUIERDA.SLDPRT
ALERON_DERECHO.SLDPRT
ALERON_IZQUIERDO.SLDPRT
FLAP_DERECHO.SLDPRT
SPOILER_DER_EXTERIOR.SLDPRT
SPOILER_DER_INTERIOR.SLDPRT
SPOILER_IZQ_EXTERIOR.SLDPRT
SPOILER_IZQ_INTERIOR.SLDPRT
```

No hace falta ninguna nacela previa. Cada ejecución crea documentos nuevos desde una plantilla de pieza de SOLIDWORKS.

## Sistema de coordenadas

La pieza se modela localmente:

- origen: plano de hélice;
- `X`: hacia atrás;
- `Y`: lateral;
- `Z`: vertical;
- eje del motor local: `Y=0`, `Z=0`.

El ensamblaje aplica después la transformación:

```text
X = -1.739330 m
Y = +4.300000 m
Z = -0.880000 m
```

Así se evita duplicar las coordenadas globales dentro de los croquis de la pieza.

## Etapas

### Stage 1 — OML e integración

Genera:

- 12 perfiles cerrados asimétricos;
- cuatro curvas guía;
- loft sólido principal;
- envolvente interna oculta del PW127XT-M;
- boss y transición del gearbox;
- hueco central del eje;
- fairing superior ancho tipo saddle;
- vistas lateral, frontal, planta e isométrica.

Archivo:

```text
NACELA_DERECHA_A2_STAGE1_OML.SLDPRT
```

### Stage 2 — admisión y escape

Genera:

- bolsillo exterior de labio;
- chin intake de 0.650 x 0.362 m;
- ducto por loft hasta interfaz 0.420 x 0.260 m;
- dos escapes D-shaped de 0.340 x 0.180 m;
- bolsillos térmicos alrededor de los escapes;
- dos tomas NACA auxiliares.

Archivo:

```text
NACELA_DERECHA_A2_STAGE2_SISTEMAS.SLDPRT
```

La revisión normal termina aquí para revisar primero la forma y la integración.

### Stage 3 — capós y paneles

Se ejecuta de forma explícita después de aprobar Stage 2. Agrega:

- gran capó exterior;
- gran capó interior;
- línea funcional de firewall;
- panel exterior de servicio de aceite.

Archivo:

```text
NACELA_DERECHA_A2_STAGE3_FINAL.SLDPRT
```

## Uso

Actualizar:

```text
01_ACTUALIZAR.bat
```

Preparar el ala la primera vez:

```text
00_PREPARAR_ALA.bat
```

Generar A2 hasta Stage 2 y crear el ensamblaje de revisión:

```text
02_EJECUTAR_REVISION.bat
```

Abrir el último resultado:

```text
03_ABRIR_ULTIMO_RESULTADO.bat
```

Generar solo OML:

```text
04_EJECUTAR_SOLO_OML.bat
```

Generar también Stage 3:

```text
05_EJECUTAR_STAGE3_CAPOS.bat
```

## Resultados

```text
generated\A2\
```

La revisión normal crea:

```text
NACELA_DERECHA_A2_STAGE1_OML.SLDPRT
NACELA_DERECHA_A2_STAGE2_SISTEMAS.SLDPRT
ALA_REVIEW_NACELA_DER_A2.SLDASM
VALIDACION_STAGE1_A2.txt
VALIDACION_STAGE2_A2.txt
```

También crea BMP lateral, frontal, planta e isométrica de cada etapa.

## Criterio de modelado

- Una spline cerrada por sección.
- Cuatro guías longitudinales.
- Loft sólido nativo.
- Fairing fusionado con el cuerpo principal.
- Tomas y escapes obtenidos mediante herramientas loft y sustracciones booleanas nativas.
- Sin pods añadidos ni tubos circulares sobredimensionados.
- Detalles funcionales antes que paneles decorativos.

## Limitación

El modelo es una definición preliminar para integración y TFG. La geometría interna exacta, soportes, accesorios, zonas térmicas y condiciones de admisión/escape deben validarse posteriormente con documentación OEM y CFD.
