# ADR-034: Alta de citas por el personal

**Fecha:** 2026-09-29

**Estado:** aceptada

## Contexto

La primera API de citas llega antes de la reserva pública. El centro tiene que registrar citas tomadas por teléfono o en el mostrador, también las que ya ocurrieron.

## Decisión

Crean y editan citas solo Admin, Manager y Employee, en cualquier fecha (H-40). El precio de cada línea es el `BasePrice` del servicio más el `PriceModifier` de la variación, mientras las empleadas no tengan nivel. `CreatedById` guarda quién creó la cita: entero, nulo, sin FK, como `CancelledById`. DELETE es baja lógica. Cancelar es una transición aparte, sin penalización económica en el piloto.

Las reglas y el contrato están en el volumen 1 §3.1.5 y §5.1. La capa de API, en el volumen 2 §9.9.

## Alternativas descartadas

- Que la clienta reserve ya por esta API. Llega con la reserva pública.
- Prohibir fechas pasadas. Impide registrar lo ocurrido.
- DELETE = cancelar. Mezcla la retirada de la cita con la cancelación.

## Consecuencias

La reserva pública necesitará sus propias reglas (antelación, sin fechas pasadas). Las tarifas por nivel (`ServicePricing`) se aplicarán cuando exista el nivel de la empleada. La penalización por cancelación queda fuera de este piloto y llega con Redsys.

## Tareas relacionadas

H-40. Tarea `869d7f519`.
