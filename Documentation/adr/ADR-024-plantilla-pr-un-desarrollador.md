# ADR-024: Plantilla de pull request para un desarrollador

**Fecha:** 2026-09-24

**Estado:** aceptada

## Contexto

La plantilla de pull request pedía un revisor, una prueba en staging y una cobertura antes y después. Nada de eso existe: hay una persona, no hay staging y no hay un umbral de cobertura que el CI mida.

## Decisión

La plantilla de pull request se adapta a un solo desarrollador: revisión propia con evidencia y revisión de Claude Code.

## Alternativas descartadas

- Mantener una definición de hecho con revisor obligatorio, staging y cobertura que no existen.

## Consecuencias

La plantilla pide evidencia (comandos, SQL, HTTP, E2E), no una firma de otra persona. El CI cubre build, tests, formato y lint; las casillas no sustituyen a esos jobs.

## Tareas relacionadas

D-24. Tarea `869f6r4hm`.
