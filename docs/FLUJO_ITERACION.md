# Flujo de iteración con capturas

1. Ejecutar `01_ACTUALIZAR.bat`.
2. Ejecutar `02_EJECUTAR_REVISION.bat`.
3. Abrir `generated\REVISION\ALA_REVIEW_NACELA_DER_REVISION.SLDASM`.
4. Ocultar temporalmente la semiala izquierda si molesta.
5. Capturar lateral, frontal e isométrica.
6. Informar también cualquier error de `ultimo_ejecucion.log`.
7. No editar manualmente el SLDPRT generado: las correcciones se realizan en GitHub y se regeneran.

## Qué se revisa en Stage 1

- longitud aparente;
- ubicación de máximo volumen;
- altura de la panza;
- continuidad de la cola;
- transición del morro;
- ancho frontal;
- tamaño y forma del fairing;
- interferencia visual con el ala, flap y spoiler;
- líneas de loft o quiebres no deseados.

## Criterio de aceptación

Stage 1 se aprueba cuando:

- hay un único cuerpo sólido;
- no hay cuerpos de superficie;
- la silueta es creíble en las tres vistas;
- el fairing no domina visualmente la nacela;
- no se observan quiebres, facetas ni costuras de cuadrantes;
- la pieza se edita como operaciones nativas de SOLIDWORKS.
