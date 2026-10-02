# ADR-041: Retirada del consentimiento de tratamiento de datos

**Fecha:** 2026-10-02

**Estado:** aceptada

## Contexto

El consentimiento de tratamiento de datos (`data_processing`) es obligatorio para tener ficha de clienta. Se podía dar en el alta y no se podía retirar desde la ficha.

## Decisión

Retirarlo desde la ficha da de baja la ficha de la clienta (H-47): sale de la reserva y de las listas, y se guarda la fecha de retirada. El historial se conserva por obligación legal. La supresión completa es un trámite aparte, cuando se cierre el procedimiento de RGPD (tarea `869f6r7b3`).

El contrato está en el volumen 1 §5.1 y el encaje legal en el volumen 1 §6.1.3.

## Alternativas descartadas

- No permitir retirarlo desde la ficha.
- Retirarlo sin efecto: la clienta seguiría activa sin base legal.

## Consecuencias

«Reactivar» todavía no exige el consentimiento, porque las altas con Google no lo tienen. Se resuelve con el trámite de RGPD.

## Tareas relacionadas

H-47. Tarea `869fazwwe`. Trámite de supresión: `869f6r7b3`.
