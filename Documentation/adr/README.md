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
| ADR-001 | Kanban con una tarea en curso | aceptada | 2026-09-24 |
| ADR-002 | Claude Code como desarrollador y coordinador | aceptada | 2026-09-24 |
| ADR-003 | Entrega en vertical hacia un MVP piloto | aceptada | 2026-09-24 |
| ADR-004 | More Than Brows como cliente piloto | aceptada | 2026-09-24 |
| ADR-005 | 25 horas semanales y un solo desarrollador | aceptada | 2026-09-24 |
| ADR-006 | El estado vive en ClickUp y en el traspaso del repo | aceptada | 2026-09-24 |
| ADR-007 | Documentación por bloque, auditoría mensual y ADR | aceptada | 2026-09-24 |
| ADR-008 | CI obligatorio en cada pull request | aceptada | 2026-09-24 |
| ADR-009 | Migración a .NET 10 LTS | aceptada | 2026-09-24 |
| ADR-010 | Salir de las licencias comerciales de librerías | aceptada | 2026-09-24 |
| ADR-011 | vue-i18n pasa a la versión 11 | aceptada | 2026-09-24 |
| ADR-012 | Gráficas con una librería de Vue | aceptada | 2026-09-24 |
| ADR-013 | Mapa central de errores HTTP | aceptada | 2026-09-24 |
| ADR-014 | Query filters cerrados por defecto | aceptada | 2026-09-24 |
| ADR-015 | Casos de uso en Infrastructure | aceptada | 2026-09-24 |
| ADR-016 | Integración con SQL Server real | sustituida por ADR-031 | 2026-09-24 |
| ADR-017 | Sesión con refresh en cookie httpOnly | aceptada | 2026-09-24 |
| ADR-018 | Puerto 5555 como convención, sin fallback a localhost | aceptada | 2026-09-24 |
| ADR-019 | Endurecimiento antes de producción | aceptada | 2026-09-24 |
| ADR-020 | Aplicación móvil como PWA | sustituida por ADR-040 | 2026-09-24 |
| ADR-021 | Decidir la plataforma de producción antes de montarla | aceptada | 2026-09-24 |
| ADR-022 | Los trámites externos arrancan ya | aceptada | 2026-09-24 |
| ADR-023 | Limpieza de ClickUp y definición de hecho por bloque | aceptada | 2026-09-24 |
| ADR-024 | Plantilla de pull request para un desarrollador | aceptada | 2026-09-24 |
| ADR-025 | Revisar la base legal de accesibilidad | aceptada | 2026-09-24 |
| ADR-026 | RGPD del piloto | aceptada | 2026-09-24 |
| ADR-027 | Traspaso entre equipos a través del repo | aceptada | 2026-09-24 |
| ADR-028 | Checks obligatorios en main | aceptada | 2026-09-28 |
| ADR-029 | Aserciones con AwesomeAssertions | aceptada | 2026-09-28 |
| ADR-030 | Mapeo entidad a DTO con Mapperly | aceptada | 2026-09-28 |
| ADR-031 | Tests de integración contra PostgreSQL real con Testcontainers | aceptada | 2026-09-29 |
| ADR-032 | Plataforma de producción del piloto: AWS simplificado | aceptada | 2026-09-29 |
| ADR-033 | Motor de base de datos: PostgreSQL | aceptada | 2026-09-29 |
| ADR-034 | Alta de citas por el personal | aceptada | 2026-09-29 |
| ADR-035 | Prueba de alergia: aviso sin bloqueo | aceptada | 2026-09-30 |
| ADR-036 | Pantallas de la clienta y aterrizaje en Mis citas | aceptada | 2026-10-01 |
| ADR-037 | Clientes y empleados por separado, sin gestión de usuarios genéricos | aceptada | 2026-10-01 |
| ADR-038 | La clienta reserva y modifica su cita | aceptada | 2026-10-01 |
| ADR-039 | Una sola pantalla de reserva y un listado de citas | aceptada | 2026-10-01 |
| ADR-040 | App móvil nativa en React Native | aceptada | 2026-10-01 |
| ADR-041 | Retirada del consentimiento de tratamiento de datos | aceptada | 2026-10-02 |
