# Sistema de coordenadas

El origen está en el borde de ataque de la sección local del ala, sobre el eje longitudinal del motor.

- **x:** positivo hacia popa.
- **y:** positivo hacia el exterior de la semiala derecha.
- **z:** positivo hacia arriba.

La geometría se modela centrada en `y = 0`. Para montar la nacela derecha en el avión completo debe trasladarse a `y = +4.30 m`; la izquierda se refleja o se traslada a `y = -4.30 m` y debe invertirse el lado del escape si se exige simetría exterior.

## Referencias longitudinales

| Referencia | x [m] |
|---|---:|
| Plano de hélice | -1.48 |
| Inicio de nacela | -1.36 |
| Frente de envolvente motor | -1.25 |
| Borde de ataque local del ala | 0.00 |
| Final de envolvente motor | 0.88 |
| Mamparo cortafuego | 0.98 |
| Frente de reserva del tren | 1.72 |
| Bisagra del flap | 1.923 aprox. |
| Borde de fuga local | 2.959 aprox. |
| Final de reserva del tren | 3.28 |
| Final de nacela | 3.55 |

La incidencia del ala se aplica únicamente al sólido de referencia. El eje de motor permanece horizontal en esta revisión; cualquier ángulo de instalación debe convertirse en un parámetro separado, no introducirse corrigiendo estaciones manualmente.
