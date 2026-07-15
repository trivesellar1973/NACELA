# Criterio geométrico — revisión A0

## 1. Decisión principal

La nacela se modela como una cubierta turbohélice bajo ala y próxima al intradós, pero no profundamente enterrada. El cuerpo principal permanece separado del ala y un fairing corto cierra la transición.

La coordenada vertical se fija inicialmente en:

```text
Z_EJE_MOTOR = -0.880 m
```

Con una altura máxima de 1.340 m, la corona del cuerpo queda aproximadamente en `z = -0.250 m`. El intradós medido en la estación del motor está alrededor de `z = -0.12 ... -0.16 m`; el fairing completa la separación sin deformar el OML principal.

## 2. Por qué se descartó la geometría anterior

Se descartan explícitamente:

- una silueta lateral extruida;
- secciones construidas con cuatro cuadrantes independientes;
- entradas y escapes como salientes pegados;
- puertas modeladas de manera independiente a la piel;
- un recorte saddle profundo dentro de la nacela;
- paneles y triángulos decorativos antes de cerrar la forma principal.

## 3. Construcción del OML

El cuerpo usa diez perfiles cerrados. Cada perfil es una única spline 3D y posee:

- ancho;
- altura;
- centro vertical;
- exponente lateral;
- exponente superior;
- exponente inferior.

El exponente inferior es mayor que el superior para obtener una panza más llena sin convertir la sección en una esfera.

Cuatro guías controlan:

- corona;
- panza;
- lateral exterior;
- lateral interior.

## 4. Fairing ala–nacela

El fairing tiene cuatro secciones. Su borde superior sigue valores derivados del intradós real y conserva aproximadamente 8–10 mm de separación. No debe envolver el ala ni parecer un pylon rectangular.

## 5. Orden de desarrollo

### Stage 1 — activo

- OML;
- fairing;
- posición en el ensamblaje;
- control visual y bounding box.

### Stage 2 — bloqueado hasta aprobar Stage 1

- toma inferior real;
- labio, garganta y difusor;
- escapes cortados en la piel;
- heat shields.

### Stage 3 — bloqueado hasta aprobar Stage 2

- capó exterior e interior derivados de la piel;
- gap uniforme;
- bisagras y configuraciones abiertas;
- paneles de servicio mínimos.

## 6. Referencias técnicas

- SOLIDWORKS API 2021: `IFeatureManager.InsertProtrusionBlend2`, `IAssemblyDoc.AddComponent5` e `IModelDoc2.SaveAs3`.
- Roskam, *Airplane Design*, Part II y Part III: integración preliminar del sistema propulsor, accesibilidad y configuración de nacelas.
- Informes THR-001/26 a THR-003/26: configuración, ala, estación del motor y dimensiones del TFG.
- AAIB, DHC-8/Q400: arquitectura de puertas de acceso de la nacela, usada únicamente como referencia funcional para una etapa posterior.
