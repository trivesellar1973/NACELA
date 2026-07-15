# NACELA — generador nativo para SOLIDWORKS 2021

Este repositorio contiene un programa **C#/.NET Framework** que controla SOLIDWORKS mediante su API COM y crea la nacela con operaciones nativas editables.

No usa STEP, IGES, STL ni cuerpos importados.

## Qué archivos son de entrada

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

**No hace falta ninguna nacela previa.** El programa crea una pieza nueva vacía y genera desde cero sus croquis, secciones, curvas guía, loft, fairing y referencias.

Si el ala no está disponible, la pieza de nacela se genera igualmente. Solamente se omite el ensamblaje de revisión.

## Estado actual

La revisión `A0` genera el Stage 1:

- cuerpo exterior OML de la nacela derecha;
- diez secciones cerradas y asimétricas;
- cuatro curvas guía continuas;
- loft sólido nativo;
- fairing superior corto;
- eje de motor de referencia;
- pieza independiente `.SLDPRT`;
- copia opcional del ensamblaje del ala con la nacela insertada;
- imagen BMP y reporte geométrico.

La toma, los escapes y los capós se incorporan después de aprobar la forma exterior.

## Instalación del ala desde el ZIP

Descargar el paquete `AlaSW*.zip` y dejarlo en una de estas ubicaciones:

```text
carpeta del repositorio
%USERPROFILE%\Downloads
%USERPROFILE%\Desktop
```

Ejecutar:

```text
00_PREPARAR_ALA.bat
```

El BAT busca dentro del ZIP `ALA_COMPLETA_MECANISMOS.SLDASM`, copia el conjunto completo a:

```text
%USERPROFILE%\Desktop\AlaSW
```

y escribe la ruta en `config\local.ini`.

No instala ni copia nacelas.

## Flujo de uso

1. Actualizar el proyecto:

```text
01_ACTUALIZAR.bat
```

2. Preparar el ala, solo la primera vez:

```text
00_PREPARAR_ALA.bat
```

3. Compilar y ejecutar:

```text
02_EJECUTAR_REVISION.bat
```

4. Abrir el último ensamblaje generado, si el ala estaba instalada:

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
NACELA_DERECHA_A0_ISO.bmp
VALIDACION_STAGE1_A0.txt
```

Cuando el ala está disponible también se generan:

```text
ALA_REVIEW_NACELA_DER_A0.SLDASM
ALA_REVIEW_NACELA_DER_A0_ISO.bmp
```

## Corrección del error de ruta

La versión actual ya no pasa al ejecutable la ruta del repositorio terminada en `\`. Esa forma podía dejar una comilla residual y provocar:

```text
System.ArgumentException: Caracteres no válidos en la ruta de acceso
```

El ejecutable localiza ahora la raíz del repositorio mediante `config\defaults.ini` y maneja cualquier error dentro del log.

## Parámetros geométricos A0

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
- `SolidWorks.Interop.sldworks.dll` en la instalación de SOLIDWORKS o en `lib\`.

No se versiona la DLL de SOLIDWORKS.

## Qué enviar para la siguiente iteración

Enviar:

- vista lateral estricta;
- vista frontal estricta;
- vista superior;
- isométrica desde delante y abajo;
- `ultimo_ejecucion.log` si aparece un error.

Este modelo es una integración preliminar, no una definición certificada del PW127XT-M.
