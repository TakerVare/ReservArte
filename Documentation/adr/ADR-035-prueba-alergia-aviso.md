# ADR-035: Prueba de alergia: aviso sin bloqueo

**Fecha:** 2026-09-30

**Estado:** aceptada

## Contexto

Servicios como el tinte exigen una prueba previa con N horas de antelación (`RequiresAllergyTest`, `AllergyTestHoursBefore`).

## Decisión

La fecha de la última prueba vive en la ficha de la clienta (`LastAllergyTestAt`, UTC): una sola fecha, sin histórico, registrada por el personal (H-41). La cita avisa (`warnings`) sin bloquear si la prueba falta o no llega a tiempo. La prueba no caduca.

La regla y el contrato de los avisos están en el volumen 1 §3.1.5 y §5.1. El dato entra en la EIPD (volumen 1 §6.1.4).

## Alternativas descartadas

- Bloquear la reserva. El centro decide en cada caso y a veces la prueba se hace el mismo día de la reserva.
- Histórico de pruebas con resultado. Es más de lo que necesita el piloto.
- Caducidad. El centro no la aplica.

## Consecuencias

La SPA debe mostrar los avisos. El dato es de salud y entra en la EIPD. Si hiciera falta histórico, iría en una tabla aparte.

## Tareas relacionadas

H-41. Tarea `869f9cu2x`.
