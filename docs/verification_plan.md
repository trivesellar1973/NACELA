# Plan de verificación pendiente

## Estado actual

El repositorio cierra la **coherencia geométrica**, no la aeronavegabilidad. El modelo puede usarse para detectar interferencias gruesas y preparar análisis, pero todavía no permite afirmar que la toma alimente correctamente al motor ni que la estructura resista.

## 1. Datos que deben obtenerse antes de congelar el CAD

- Installation drawing y zona de accesorios del PW127XT-M.
- Posiciones y rigidez de puntos de montaje OEM.
- Caudal másico corregido, presión y temperatura en cada régimen.
- Límites de distorsión y recuperación de presión en la cara del motor.
- Caudal, presión y temperatura del escape.
- Geometría certificada de la hélice 568F-1, cono y plano de referencia.
- Envolvente cinemática completa del tren principal.
- Requisitos de acceso para mantenimiento y remoción del motor.

## 2. Aerodinámica externa

Casos mínimos:

- despegue con flap 35°;
- aproximación/aterrizaje con flap 40°;
- crucero limpio;
- vigilancia a alto CL;
- motor operativo y hélice en bandera;
- resbalamiento lateral y viento cruzado.

Resultados:

- incremento de CD0 de cada nacela;
- interferencia ala–nacela;
- presión sobre carenado y puertas;
- separación cerca de la salida posterior;
- influencia en flap y spoiler.

## 3. Toma de aire

Modelar el volumen interno, no solo el exterior. Verificar:

- recuperación de presión total;
- distorsión circunferencial y radial;
- separación en el codo/difusor;
- sensibilidad a ángulo de ataque, sideslip y flujo de hélice;
- ingestión de agua, hielo y FOD;
- drenaje y operación del separador inercial.

No usar un único punto de crucero: la condición crítica puede ser despegue, baja velocidad o viento cruzado.

## 4. Escape y térmica

- mapa de temperatura en piel, ala, flap, líneas y tren;
- radiación y convección en estacionamiento y vuelo;
- dilatación del conducto;
- juntas flexibles;
- ventilación de la zona motor;
- materiales y barreras térmicas;
- riesgo de reingestión.

## 5. Estructura y cargas

Casos mínimos:

- peso del motor y cargas inerciales;
- par motor y torque reactivo;
- empuje y cargas giroscópicas de hélice;
- pérdida de pala / desequilibrio según requisitos aplicables;
- aterrizaje, rodaje y frenado transmitidos por el tren;
- presión aerodinámica sobre puertas y paneles;
- vibración, fatiga y flutter local.

## 6. Fuego y sistemas

Definir zona de fuego, mamparo, sellados, drenajes, detección/extinción, líneas de combustible/aceite/hidráulicas, cableado, ventilación y descarga de fluidos. El volumen actual del firewall es solo una referencia geométrica.

## 7. Criterio de salida de la fase preliminar

La revisión puede pasar a diseño detallado cuando:

1. los datos OEM sustituyan todas las dimensiones de confianza media;
2. no haya interferencias en todas las posiciones del tren y dispositivos;
3. la toma cumpla objetivos de recuperación/distorsión con margen;
4. la térmica mantenga materiales y sistemas dentro de límites;
5. una ruta de cargas viable conecte motor, ala y tren;
6. accesos de mantenimiento hayan sido demostrados;
7. masa y CD0 de nacela estén incorporados al balance del avión.
