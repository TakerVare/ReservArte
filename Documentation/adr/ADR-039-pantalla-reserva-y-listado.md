# ADR-039: Una sola pantalla de reserva y un listado de citas en lugar de agenda y wizard

**Fecha:** 2026-10-01

**Estado:** aceptada

## Contexto

El plan de la agenda preveía un calendario con FullCalendar, un wizard de seis pasos, un modal de reagendar y arrastrar citas. Los diseños de Figma de la clienta concentran la reserva en una sola pantalla («Selección de cita»).

## Decisión

Hay una sola pantalla de reserva y modificación, `/reservar` (H-45). La abren «Reservar Cita» y «Modificar» de Mis citas, y «Nueva cita» del listado. Reagendar es «Modificar» esa cita (`/reservar?cita=<id>`).

El personal consulta y gestiona las citas del centro en el listado `/citas` (día, semana de lunes a domingo y mes), no en una agenda de arrastre. El calendario de la reserva empieza en lunes; hoy va en `primary` y los días con hueco en `accent`; los pasados y los de fuera de la ventana van deshabilitados. Los huecos se agrupan por la empleada que puede prestar el servicio.

## Alternativas descartadas

- Agenda con FullCalendar. No es la pantalla dibujada y duplicaría la reserva.
- Wizard de seis pasos. La selección de servicio, día y hueco cabe en una pantalla.
- Arrastrar citas para cambiarlas de hora. Reagendar pasa por la misma pantalla de reserva.

## Consecuencias

FullCalendar sigue en las dependencias sin usarse (DP-07: se decide después del MVP si se retira o se recupera para una vista de agenda del personal). El listado `/citas` y los componentes base no tienen diseño en Figma: siguen el estilo de la aplicación. La lista de espera queda fuera del piloto.

## Tareas relacionadas

H-45. DP-07.
