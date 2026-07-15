# NACELA — generador nativo para SOLIDWORKS 2021

Este repositorio contiene un programa C#/.NET Framework que controla SOLIDWORKS mediante la API COM y crea una nacela turbohélice mediante operaciones nativas editables.

No usa FreeCAD, CadQuery, STEP, IGES, STL ni cuerpos importados.

## Revisión A1

La A1 corrige el defecto principal de A0: la pieza ya no contiene coordenadas globales del ala. La nacela se modela en un sistema local con origen en el plano de hélice y después se inserta en el ensamblaje mediante una transformación explícita:

- X global de montaje: borde de ataque local menos 1,600 m;
- Y global: +4,300 m;
- Z global del eje: -0,550 m.

Con esto deja de aparecer en el centro del ala.

### Stage 1 — OML y montaje

- 10 secciones cerradas asimétricas;
- 4 curvas guía continuas;
- loft sólido nativo;
- morro, boss de caja reductora y abertura central;
- envolvente interna oculta del PW127XT-M;
- fairing superior corto situado alrededor del borde de ataque real;
- pieza definida en coordenadas locales;
- ensamblaje de revisión con transformación global explícita.

### Stage 2 — sistemas externos

- toma principal inferior tipo chin intake;
- área elíptica nominal de 0,1848 m²;
- conducto interno por loft hacia una interfaz de 0,420 × 0,260 m;
- dos escapes laterales compactos tipo D;
- dos entradas NACA pequeñas para ventilación o accesorios;
- las NACA no alimentan el motor principal;
- el diámetro equivalente preliminar del escape se conserva solo como dato pendiente de revisión.

Los capós practicables, bisagras, cierres, paneles de servicio y heat shields corresponden a Stage 3. No se agregan hasta aprobar la forma y la posición de A1.

## Archivos de entrada

El único modelo previo necesario para revisar la integración es el conjunto del ala. No hace falta ninguna nacela previa. El programa crea una pieza nueva y genera desde cero sus croquis, secciones, curvas guía, lofts, cortes y referencias.

Si el ala no está disponible, las piezas de nacela se generan igualmente. Solamente se omite el ensamblaje de revisión.

## Parámetros adoptados

Los parámetros editables están en `config/defaults.ini`:

- motor: PW127XT-M;
- longitud motor: 2,130 m;
- ancho motor: 0,720 m;
- altura motor: 0,840 m;
- masa seca de referencia: 494,7 kg;
- longitud nacela: 2,750 m;
- ancho máximo nacela: 0,900 m;
- altura máxima nacela: 1,100 m;
- posición transversal: 4,300 m;
- plano de hélice: 1,600 m delante del borde de ataque local;
- eje: 0,550 m bajo el plano de cuerda;
- cuerda local: 2,958955 m;
- diámetro preliminar de hélice: 3,600 m;
- régimen: 1200 rpm.

## Ejecución normal

1. `01_ACTUALIZAR.bat`
2. `00_PREPARAR_ALA.bat`, únicamente cuando el ala todavía no está instalada.
3. `02_EJECUTAR_REVISION.bat`
4. `03_ABRIR_ULTIMO_RESULTADO.bat`

`02_EJECUTAR_REVISION.bat` construye Stage 1, Stage 2 y el ensamblaje de revisión.

Para revisar solamente la piel exterior antes de los conductos:

```text
04_EJECUTAR_SOLO_OML.bat
```

## Resultados

La revisión se guarda en `generated/A1/`:

- `NACELA_DERECHA_A1_STAGE1_OML.SLDPRT`
- `NACELA_DERECHA_A1_STAGE2_SISTEMAS.SLDPRT`
- `ALA_REVIEW_NACELA_DER_A1.SLDASM`
- vistas lateral, frontal, planta e isométrica de Stage 1 y Stage 2;
- `VALIDACION_STAGE1_A1.txt`
- `VALIDACION_STAGE2_A1.txt`

## Referencias de criterio

La trazabilidad y las decisiones de diseño están en `docs/CRITERIO_DISENO_A1.md`.

## Compilación

`BUILD.bat` busca `csc.exe` de .NET Framework 4.x y la DLL de interoperabilidad de SOLIDWORKS. Después de compilar, copia la DLL junto al ejecutable en `bin/`.

## Qué enviar para la siguiente iteración

- vista lateral estricta;
- vista frontal estricta;
- vista superior;
- isométrica desde delante y abajo;
- `ultimo_ejecucion.log` si aparece un error.

## Alcance

Es un modelo preliminar de integración y no un installation drawing certificado. La geometría OEM interna, cargas, fuego, vibración, bird strike, distorsión de entrada, pérdidas de conducto y temperatura de escape requieren documentación del fabricante y análisis específicos.
