# Base de diseño y trazabilidad

## 1. Objetivo

Definir una nacela paramétrica de integración preliminar para el avión del TFG, evitando dos errores habituales:

- dibujar una forma exterior sin reservar motor, flujo, anclajes, fuego, mantenimiento y tren;
- copiar una nacela existente sin adaptarla a la geometría local del ala propia.

El modelo se expresa en metros y conserva por separado los volúmenes funcionales. Es apto para iteraciones de layout, generación de superficies CFD y discusión del Capítulo 4; no reemplaza una definición OEM ni una substanciación CS/FAR 25.

## 2. Datos del avión fijados por el TFG

| Dato | Valor usado | Procedencia |
|---|---:|---|
| MTOW | 23 100 kg | THR-002/26 |
| Superficie alar | 79.0 m² | THR-002/26 y THR-003/26 |
| Envergadura | 29.5 m | THR-003/26 |
| Alargamiento | 11.0 | THR-003/26 |
| Ahusamiento | 0.600 | THR-003/26 |
| Cuerda raíz / punta | 3.35 / 2.01 m | THR-003/26 |
| Perfil raíz / punta | NACA 23015 / NACA 23012 | THR-003/26 |
| Posición transversal del motor | ±4.30 m | THR-003/26 |
| Flap | 35 % de cuerda local | THR-003/26 |
| Configuración | bimotor, ala alta, tren retráctil alojado en nacelas | THR-001/26 |

Para una planta trapezoidal sin flecha al cuarto de cuerda:

- semienvergadura: 14.75 m;
- posición adimensional del motor: η = 4.30 / 14.75 = 0.2915;
- cuerda local interpolada: 2.959 m;
- t/c local interpolado: 0.1413;
- espesor máximo local: 0.418 m;
- bisagra del flap: x ≈ 1.923 m desde el borde de ataque local.

Estos valores se recalculan en `src/geometry.py`; no se duplican como constantes ocultas.

## 3. Motor y hélice

### Datos confirmados

La ficha oficial de Pratt & Whitney publica para el PW127XT-M:

- 3360 ESHP de clase termodinámica;
- 2750 SHP de potencia mecánica;
- 1200 rpm de hélice;
- aplicaciones ATR 42 y ATR 72.

El TCDS EASA.A.084 Issue 14 establece que el ATR 72-212A con MOD 10016 puede llevar dos PW127XT-M y lista dos hélices Hamilton Sundstrand 568F-1 para el ATR 72-212A.

### Datos preliminares

Pratt & Whitney publica para la familia PW127 una envolvente de aproximadamente 84 × 33 × 26 in. Esto se usa solo como reserva inicial. El XT-M puede diferir en accesorios, soportes, tuberías y zonas de mantenimiento. El archivo `config/nacelle.yaml` marca esa confianza como **media**.

El diámetro de hélice de 3.93 m se adopta como referencia de integración ATR-like. Debe verificarse contra el TCDS FAA P8BO o la documentación específica de la hélice antes de congelar el diseño.

## 4. Arquitectura de la nacela

### 4.1 Piel exterior

La piel se define mediante estaciones elípticas en x. Es una aproximación controlable, no una afirmación de que la sección final deba ser exactamente elíptica. El máximo se ubica alrededor del motor y accesorios, y el carenado se reduce gradualmente después del ala.

### 4.2 Toma de aire

Se adopta una toma inferior tipo *chin inlet* porque:

- es coherente con la arquitectura de instalación de la familia PW100/PW127 en ATR;
- separa la captura del escape lateral;
- permite reservar un separador inercial y drenajes en la parte baja;
- evita una entrada puramente estética sin conducto interno.

La toma contiene tres secciones: labio, garganta y salida del difusor. El área geométrica del labio se hace coincidir con 0.305 m² dentro de ±5 %. La recuperación de presión y distorsión **no están calculadas**: faltan el caudal corregido y la cara de entrada real del motor.

### 4.3 Escape

Se reserva un escape lateral exterior modular. Su posición es preliminar. La salida definitiva depende de:

- temperatura total y caudal de gases;
- expansión y pérdida de carga admisible;
- separación térmica de ala, flap, tren y estructura;
- contribución de empuje residual;
- restricciones acústicas y de mantenimiento.

### 4.4 Mamparo y anclajes

Se incluyen dos anillos de montaje conceptuales y un mamparo cortafuego. No representan la geometría de los herrajes reales. Su función es impedir que el CAD esconda el problema de transmisión de cargas y separar la zona motor del volumen posterior.

### 4.5 Tren principal

Se reserva una caja posterior al mamparo, dentro de la extensión de nacela. No se modelan rueda, amortiguador ni cinemática porque pertenecen al dimensionamiento del tren. La caja evita diseñar un carenado posterior que después resulte inutilizable.

## 5. Fuentes web

- EASA TCDS A.084: https://www.easa.europa.eu/en/document-library/type-certificates/aircraft-cs-25-cs-22-cs-23-cs-vla-cs-lsa/easaa084-atr-42atr-72
- Pratt & Whitney PW127XT: https://www.rtx.com/en/prattwhitney/products/regional-aviation-engines/pw127xt
- Pratt & Whitney PW100/150: https://www.rtx.com/en/prattwhitney/products/regional-aviation-engines/pw100-150
- CadQuery: https://cadquery.readthedocs.io/

## 6. Jerarquía de confianza

1. **Alta:** geometría del avión definida en los informes TFG; designación y potencia del motor; compatibilidad motor–ATR y modelo de hélice listados por EASA.
2. **Media:** envolvente externa genérica PW127 y diámetro de hélice de referencia.
3. **Preliminar:** estaciones del OML, toma, escape, anclajes, mamparo y reserva del tren.

Los datos de nivel 2 y 3 deben reemplazarse por documentación de instalación, análisis o decisiones explícitas antes del diseño detallado.
