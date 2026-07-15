# NACELA — generador nativo para SOLIDWORKS 2021

Proyecto C#/.NET Framework que controla SOLIDWORKS mediante su API COM y genera geometría nativa editable.

No usa FreeCAD, CadQuery, STEP, IGES, STL, mallas ni cuerpos importados.

## Revisión activa: B2

`B2` genera únicamente la pieza verde del frente mostrada en la referencia. Se dejó afuera deliberadamente la pieza azul posterior y cualquier detalle secundario.

La idea geométrica es simple y explícita:

1. un cuerpo central construido con un loft de elipses verdaderas;
2. un boss frontal cuyo primer perfil es un círculo y que se une al cuerpo con un loft separado;
3. una toma inferior cuyo primer perfil es un rectángulo redondeado y que se une a la panza mediante otro loft separado;
4. las tres piezas se convierten en un único sólido mediante operaciones `Combine/Add`.

No se usa un loft gigante desde el círculo hasta toda la nacela. Tampoco se usan curvas guía, filetes automáticos ni exponentes variables en el cuerpo central, porque esas operaciones estaban generando torsiones y formas difíciles de controlar.

## Alcance de esta revisión

Se genera solamente:

- cuerpo central verde sólido;
- círculo frontal cerrado;
- transición circular hacia el cuerpo central;
- toma inferior rectangular redondeada cerrada;
- unión sólida de las tres partes;
- extremo posterior ancho y plano para conectar más adelante la pieza azul.

No se generan todavía:

- shell o vaciado;
- hueco del eje o del spinner;
- boca abierta de la toma;
- ductos internos;
- saddle superior;
- pieza azul posterior;
- escapes;
- tomas laterales;
- paneles.

El vaciado debe hacerse al final, cuando la forma exterior esté aprobada.

## Requisitos del TFG conservados

```text
L_nac = 2.556 m
W_nac = 0.920 m
H_nac objetivo = 1.340 m
y_motor = +4.300 m
c_local = 2.958955 m
plano de hélice = 1.000 m delante del borde de ataque
x_nac_aft respecto del BA = +1.556 m
A_c_total futura = 0.1846585 m2
envolvente motor = 2.130 x 0.720 x 0.840 m
```

La pieza se crea localmente con el plano de hélice en `X=0` y se inserta en el conjunto mediante:

```text
X = -1.739330 m
Y = +4.300000 m
Z = -0.880000 m
```

## Geometría B2

### Cuerpo central verde

- 11 secciones elípticas;
- costura común en `+Y` para evitar torsión;
- sin curvas guía;
- sin fillets automáticos;
- ancho máximo de `0.920 m`;
- extremo posterior de `0.720 x 0.820 m`, sin boattail en punta.

### Boss frontal circular

Se construye como un cuerpo sólido separado:

```text
círculo Ø0.280
círculo Ø0.340
óvalo 0.420 x 0.460
óvalo 0.520 x 0.580
óvalo 0.640 x 0.720
óvalo 0.760 x 0.860
óvalo 0.840 x 0.960
```

Las últimas secciones penetran dentro del cuerpo central. Después se ejecuta `Combine/Add`.

### Toma inferior rectangular redondeada

Se construye con siete perfiles de la misma familia de superelipse, por lo que todos mantienen la misma orientación y no se retuercen durante el loft.

La boca permanece cerrada para evaluar solamente la forma exterior. Las últimas secciones penetran dentro de la panza y luego se ejecuta `Combine/Add`.

## Archivos activos

```text
src/B2Config.cs
src/B2Geometry.cs
src/B2BodyOps.cs
src/B2Stage1Builder.cs
src/B2AssemblyReviewBuilder.cs
src/Program.cs
src/SwSession.cs
src/SwGeometry.cs
src/SwGeometryCompatTypes.cs
```

Los archivos B1 pueden permanecer en la carpeta, pero `BUILD.bat` no los compila.

## Uso

```text
01_ACTUALIZAR.bat
02_EJECUTAR_REVISION.bat
03_ABRIR_ULTIMO_RESULTADO.bat
```

Resultados:

```text
generated\B2\NACELA_DERECHA_B2_STAGE1_FRENTE_SOLIDO.SLDPRT
generated\B2\ALA_REVIEW_NACELA_DER_B2.SLDASM
generated\B2\VALIDACION_B2_STAGE1.txt
```

También se generan vistas lateral, frontal, planta e isométrica de la pieza y del ensamblaje.

`05_EJECUTAR_STAGE3_CAPOS.bat` permanece deshabilitado hasta aprobar el cuerpo verde B2.
