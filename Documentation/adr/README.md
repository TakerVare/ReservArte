# Decisiones de arquitectura (ADR)

Un ADR (Architecture Decision Record) registra una decisión de arquitectura de ReservArte: el contexto en el que se tomó, qué se decidió, qué alternativas se descartaron y qué consecuencias tiene. Sirve para que la decisión no se vuelva a debatir desde cero y para que un cambio posterior quede trazado.

Los volúmenes de `Documentation/` describen el sistema. Los ADR registran las decisiones que lo explican. Una decisión aceptada vive aquí; los volúmenes pueden enlazarla, no copiarla.

## Numeración

Un fichero por decisión, en esta carpeta:

```
ADR-NNN-titulo-corto.md
```

- `NNN` es un número correlativo de tres dígitos (`001`, `002`, …). El siguiente ADR toma el número siguiente al último del índice, aunque alguno esté sustituido.
- `titulo-corto` es un slug en minúsculas, con guiones, que nombra la decisión (no el problema).

## Estados

Cada ADR declara uno de estos estados:

| Estado | Significado |
|---|---|
| `propuesta` | Redactada y pendiente de aceptación. |
| `aceptada` | Vigente. Es la decisión que hay que seguir. |
| `sustituida por ADR-NNN` | Dejó de estar vigente. El ADR que la sustituye es el indicado. |

## Un ADR aceptado no se reescribe

El texto de un ADR aceptado queda congelado. Si la decisión cambia, se crea otro ADR que la sustituye y el anterior pasa a `sustituida por ADR-NNN`. No se corrige el original para que diga lo nuevo: el historial de la decisión es el valor del registro.

## Índice

| Número | Título | Estado | Fecha |
|---|---|---|---|
| | | | |
