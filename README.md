# NACELA — generador nativo para SOLIDWORKS 2021

Este repositorio reemplaza el prototipo de FreeCAD/CadQuery. El entregable es un programa **C#/.NET Framework** que controla SOLIDWORKS mediante su API COM y crea operaciones nativas editables: croquis 3D, splines cerradas, lofts sólidos, curvas guía, fairing y ensamblaje de revisión.

No usa STEP, IGES, STL ni cuerpos importados.

## Estado actual

La revisión `A0` genera solamente el **Stage 1**:

- cuerpo exterior OML de la nacela derecha;
- diez secciones cerradas y asimétricas;
- cuatro curvas guía continuas;
- loft sólido nativo;
- fairing superior corto ajustado al intradós medido;
- eje de motor de referencia;
- pieza independiente `.SLDPRT`;
- copia del ensamblaje del ala con la nacela insertada y fijada;
- imagen BMP y reporte geométrico.

La toma de aire, los escapes y los capós están bloqueados deliberadamente. Agregarlos antes de aprobar el OML volvería a producir una geometría parcheada. El flujo acordado es: ejecutar, revisar capturas, corregir el cuerpo y recién entonces habilitar Stage 2.

## Uso

1. Clonar el repositorio en Windows.
2. Tener SOLIDWORKS 2021 instalado.
3. Ejecutar `CONFIGURAR_RUTA_ALA.bat` si el ensamblaje no está en:

```text
%USERPROFILE%\Desktop\AlaSW\ALA_COMPLETA_MECANISMOS.SLDASM
```

4. Para actualizar el proyecto:

```text
01_ACTUALIZAR.bat
```

5. Para compilar y generar la revisión:

```text
02_EJECUTAR_REVISION.bat
```

6. Para abrir el ensamblaje generado:

```text
03_ABRIR_ULTIMO_RESULTADO.bat
```

Los resultados se guardan en:

```text
generated\A0\
```

## Archivos generados

```text
NACELA_DERECHA_A0_STAGE1.SLDPRT
ALA_REVIEW_NACELA_DER_A0.SLDASM
NACELA_DERECHA_A0_ISO.bmp
ALA_REVIEW_NACELA_DER_A0_ISO.bmp
VALIDACION_STAGE1_A0.txt
```

## Parámetros

La geometría se modifica en `config/defaults.ini`. Para cambios locales de ruta usar `config/local.ini`, que no se versiona.

La revisión A0 conserva las dimensiones calculadas para la envolvente del motor:

- longitud: 2.556 m;
- ancho máximo: 0.920 m;
- altura máxima: 1.340 m;
- estación transversal: y = +4.300 m;
- eje del motor: z = -0.880 m;
- extremo delantero: x = -1.739330 m;
- extremo trasero: x = +0.816670 m.

La forma no se obtiene de una extrusión lateral. Se define mediante diez secciones asimétricas, con panza más llena, techo contenido y cola ascendente.

## Compilación

`BUILD.bat` busca:

- `csc.exe` de .NET Framework 4.x;
- `SolidWorks.Interop.sldworks.dll` en la instalación de SOLIDWORKS, en `lib\` o en la carpeta anterior `AlaSW`.

No se versiona la DLL de SOLIDWORKS.

## Qué enviar para la siguiente iteración

Enviar tres capturas del ensamblaje:

- lateral estricta;
- frontal estricta;
- isométrica desde delante y abajo.

No alcanza con una sola vista: una forma puede verse bien en isométrica y seguir siendo demasiado ancha, corta o alta.

## Límite del modelo

Este repositorio automatiza un diseño preliminar nativo de SOLIDWORKS. No es una instalación certificada ni reemplaza el installation drawing del PW127XT-M, análisis CFD, cargas, fuego, vibración, bird strike o mantenimiento.
