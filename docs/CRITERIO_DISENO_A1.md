# Criterio de diseño — revisión A1

## 1. Problema corregido

La revisión A0 mezclaba coordenadas globales del ensamblaje con coordenadas de pieza. Al insertar después el componente en el origen, la nacela aparecía atravesando la zona central del ala. Además, las secciones alcanzaban muy pronto el ancho y la altura máximos, produciendo una forma casi esférica.

A1 usa dos sistemas separados:

- **pieza local**: origen en el plano de hélice, eje del motor en Y=0 y Z=0;
- **montaje global**: traslación calculada con el borde de ataque local, la posición transversal y la distancia vertical adoptada.

## 2. Datos del TFG conservados

- y_motor = 4,300 m;
- c_local = 2,958955 m;
- plano de hélice = 1,600 m delante del borde de ataque;
- eje = 0,550 m bajo el plano de cuerda;
- L_eng = 2,130 m;
- W_eng = 0,720 m;
- H_eng = 0,840 m;
- L_nac = 2,750 m;
- W_nac = 0,900 m;
- H_nac = 1,100 m;
- área de captura requerida = 0,1846585 m².

La posición longitudinal resultante es:

```text
X_montaje = X_BA_local - 1,600 m
X_frente_global = -2,339330 m
X_final_global = +0,410670 m
x_aft respecto del BA = 1,150 m
x_aft/c_local = 0,38865
```

## 3. Arquitectura externa

Se eligió una nacela close-coupled bajo ala, propia de un turbohélice regional:

- morro pequeño alrededor de la interfaz de caja reductora;
- crecimiento progresivo de sección;
- volumen máximo en la zona del motor y accesorios;
- techo contenido para no invadir el ala;
- panza más llena;
- cola corta y convergente;
- fairing superior corto alrededor del borde de ataque.

No se copia un ATR ni un Dash 8. Se usa su lógica de integración como referencia tipológica.

## 4. Toma principal

La toma principal es inferior y frontal. No se usan las entradas NACA para alimentar el motor.

Dimensiones nominales de captura:

```text
ancho = 0,650 m
alto = 0,362 m
área elíptica = pi/4 × 0,650 × 0,362 = 0,1848 m²
```

El corte se genera mediante tres secciones y un loft:

1. perfil exterior ligeramente mayor;
2. sección de captura;
3. interfaz interna de 0,420 × 0,260 m.

Esto produce una abertura real y un difusor editable, no un pod adherido.

## 5. Entradas NACA

Se agregan dos entradas sumergidas pequeñas para ventilación o accesorios. El estudio NACA RM A7I30 identifica como variables principales la divergencia de paredes, el ángulo de rampa, la relación ancho/profundidad y el espesor de capa límite. Por eso se modelan como entradas flush convergentes hacia un fondo interno y se mantienen pequeñas.

No representan la toma de aire principal del PW127XT-M.

## 6. Escapes

Se crean dos cortes laterales tipo D en el cuarto trasero. La boca visible es compacta, 0,360 × 0,160 m. El valor preliminar de 0,60677 m de diámetro equivalente por salida no se fuerza sobre la OML porque es incompatible con una nacela de 0,900 m de ancho sin revisar antes el método de cálculo y las condiciones termodinámicas.

Stage 3 añadirá boquilla corta y heat shield después de validar la ubicación de las bocas.

## 7. Mantenimiento

El informe AAIB del Q400 describe dos grandes puertas delanteras por nacela, una interior y otra exterior, articuladas arriba, con un puntal telescópico y cuatro cierres rápidos por puerta. Ese principio se reserva para Stage 3.

Las puertas deberán derivarse de la piel terminada mediante Split Line, Offset Surface = 0 y espesor, para que cierren enrasadas. No se crearán como lofts independientes.

## 8. Fuentes principales

- Pratt & Whitney, PW127XT Engine Series: https://www.rtx.com/en/prattwhitney/products/regional-aviation-engines/pw127xt
- AAIB Bulletin 9/2017, DHC-8-402 Q400 G-PRPC: https://assets.publishing.service.gov.uk/media/59afe6e440f0b6174109f635/DHC-8-402_Dash_8__Q400__G-PRPC_09-17.pdf
- NACA RM A7I30, *An Experimental Investigation of the Design Variables for NACA Submerged Duct Entrances*: https://ntrs.nasa.gov/api/citations/19930093809/downloads/19930093809.pdf
- Roskam, *Airplane Design Part III*, capítulos de integración del sistema propulsivo, instalación, mantenimiento y accesibilidad.

## 9. Secuencia de desarrollo

- **A1 Stage 1:** OML, boss, envolvente motor, fairing y posición.
- **A1 Stage 2:** toma principal, ducto, escapes y entradas NACA.
- **A2 Stage 3:** capós derivados de piel, bisagras, cierres, panel de aceite, firewall, boquillas y heat shields.
- **A3:** nacela izquierda, comprobación de interferencias y configuración de mantenimiento.
