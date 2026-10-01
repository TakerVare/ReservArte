# ADR-038: La clienta reserva y modifica su cita

**Fecha:** 2026-10-01

**Estado:** aceptada

## Contexto

La primera API de citas solo dejaba crear y editar al personal. La clienta tenía que esperar a una reserva pública anónima. El piloto necesita que una clienta autenticada reserve sin ese flujo.

## Decisión

La clienta autenticada crea y modifica su propia cita en la pantalla de reserva (H-44). La API toma la clienta del token: el `customerId` del cuerpo se ignora y solo es obligatorio para el personal. Una sola cita activa (pendiente o confirmada, y que aún no haya empezado); si ya la tiene, la respuesta es 409 `APT_ACTIVE_EXISTS` y se modifica esa. Solo puede reservar dentro de la ventana de su organización.

La ventana es por organización: `CustomerBookingWindowWeeks` (6) y `StaffBookingWindowWeeks` (10), ambas entre 1 y 52. La de la clienta limita su alta y su edición. La del personal limita lo que ve en la disponibilidad; el personal sigue pudiendo registrar cualquier fecha, también pasada ([ADR-034](ADR-034-alta-citas-personal.md)).

Sustituye la parte de ADR-034 que remitía la reserva de la clienta a la reserva pública. El alta por el personal, el precio de línea, `CreatedById`, la baja lógica y la cancelación sin penalización de ADR-034 siguen vigentes.

Las reglas y el contrato están en el volumen 1 §3.1.5 y §5.1. La capa de API, en el volumen 2 §9.9.

## Alternativas descartadas

- Que solo el personal reserve. La clienta no podría pedir cita sin llamar al centro.
- Reserva pública anónima en el piloto. Exige landing, alta durante el flujo y pago; no hace falta para que una clienta ya autenticada reserve.

## Consecuencias

La reserva pública anónima queda fuera del piloto. Las notas de la cita siguen siendo internas: la clienta no las escribe. Una clienta sobre una cita ajena recibe 404.

## Tareas relacionadas

H-44.
