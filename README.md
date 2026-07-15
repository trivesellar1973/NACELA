# NACELA — generador nativo para SOLIDWORKS 2021

Proyecto C#/.NET Framework que controla SOLIDWORKS mediante su API COM y genera geometría nativa editable.

No usa FreeCAD, CadQuery, STEP, IGES, STL, mallas ni cuerpos importados.

## Revisión activa: B2

`B2` es una reconstrucción nueva centrada únicamente en la parte delantera. No modifica ni reutiliza la geometría B1.

La revisión se detiene deliberadamente antes de shell, caladuras, ductos, escapes, tomas laterales y paneles. Primero deben aprobarse:

- el círculo frontal donde se integrará el cono de hélice;
- la transición por loft desde círculo a óvalo vertical;
- la unión de esa nariz con la OML;
- la toma inferior tipo rectángulo ovalado;
- el blend de la toma con la panza;
- la lectura frontal conjunta de círculo superior y toma inferior.

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

### OML principal

- 11 secciones nuevas;
- una spline cerrada por sección;
- cuatro guías longitudinales;
- costados tensos;
- altura principal reducida para evitar una forma esférica;
- panza preparada para recibir la toma inferior;
- boattail posterior progresivo.

### Boss frontal del spinner

Se construye como un loft sólido separado que se fusiona con la OML:

```text
círculo Ø0.280
círculo Ø0.360
óvalo 0.440 x 0.500
óvalo 0.560 x 0.680
óvalo 0.700 x 0.880
óvalo 0.820 x 1.020
óvalo 0.880 x 1.080
```

No existe todavía hueco de eje. La cara frontal permanece cerrada para evaluar la forma exterior.

### Toma inferior sólida

La toma se crea con ocho secciones rounded-rectangle. Es un carenado sólido fusionado con la panza, no una caja extruida ni un hueco cortado.

La primera cara es un rectángulo ovalado cerrado y las secciones crecen hacia abajo y atrás antes de volver a integrarse en el cuerpo.

No se abre todavía la boca ni se crea el ducto.

### Saddle superior

Se genera con ocho secciones nuevas y techo progresivo. Su función en B2 es solamente permitir la revisión dentro del ensamblaje del ala.

## Archivos activos

```text
src/B2Config.cs
src/B2Geometry.cs
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

`05_EJECUTAR_STAGE3_CAPOS.bat` permanece deshabilitado hasta aprobar el frente B2.
