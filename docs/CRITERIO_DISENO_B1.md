# Criterio de diseño B1 — reconstrucción desde cero

## Objetivo

B1 reemplaza completamente la geometría A0/A1/A2. La referencia principal es la nacela CFD suministrada por el usuario, complementada por una instalación real de turbohélice de transporte.

La intención geométrica es replicar:

- morro compacto;
- máximo volumen adelantado;
- panza profunda;
- toma inferior rectangular ovalada con carenado exterior;
- tomas laterales pequeñas;
- escapes laterales altos dentro de housings;
- saddle fairing ancho y casi plano bajo el ala;
- terminación posterior pequeña y elevada.

## Restricciones conservadas

| Parámetro | Valor |
|---|---:|
| Largo total | 2.556 m |
| Ancho máximo | 0.920 m |
| Altura máxima del cuerpo OML | 1.340 m |
| Posición transversal | +4.300 m |
| Cuerda local | 2.958955 m |
| Frente respecto del BA | -1.000 m |
| Cola respecto del BA | +1.556 m |
| Área mínima de captura | 0.1846585 m² |
| Envolvente motor | 2.130 × 0.720 × 0.840 m |

## OML

La piel principal utiliza 12 secciones totalmente nuevas. Los laterales se controlan con exponentes superiores a los de una elipse para evitar una cápsula redonda. La panza usa una curvatura más llena que la corona.

Las cuatro guías longitudinales son:

- corona;
- panza;
- lateral exterior;
- lateral interior.

La sección final no mantiene una tapa circular grande: converge a un volumen pequeño y elevado.

## Saddle fairing

El fairing superior se genera con secciones propias de techo casi plano. No se utiliza una elipse cerrada como joroba. Su techo local se aproxima a `Z=0.758 m`, que con el montaje `Z=-0.880 m` queda cerca de `Z=-0.122 m`, coherente con el intradós medido en la estación del motor.

## Toma principal

La admisión se divide en dos operaciones:

1. carenado exterior fusionado con la panza;
2. conducto interior restado del sólido.

La boca es un rectángulo redondeado:

```text
ancho = 0.620 m
alto = 0.310 m
radio = 0.085 m
```

Área aproximada:

```text
A = b h - (4 - π) r²
A ≈ 0.186 m²
```

por encima del requisito de `0.1846585 m²`.

## Tomas laterales

Cada lateral recibe un housing corto fusionado con la piel. Después se resta un ducto más pequeño. Así la entrada tiene labio y profundidad sin aparecer como un pod independiente.

## Escape

Cada escape se construye con:

1. housing térmico exterior fusionado;
2. nozzle interior por loft de corte;
3. filete de labio cuando el kernel lo permite.

El diámetro equivalente preliminar de `0.60677 m` queda como referencia de cálculo y no como diámetro visible.

## Mantenimiento

Stage 3 crea dos grandes rebajes de cowling, líneas laterales de firewall y un panel de servicio. No se crean puertas pequeñas ni placas independientes.

## Archivos activos

```text
src/B1Config.cs
src/B1Geometry.cs
src/B1Stage1Builder.cs
src/B1Stage2Builder.cs
src/B1Stage3Builder.cs
src/B1AssemblyReviewBuilder.cs
```

Los builders A2 fueron retirados de `src`, por lo que el ejecutable ya no puede generar accidentalmente la geometría anterior.
