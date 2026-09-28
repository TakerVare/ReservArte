# ADR-002: Claude Code como desarrollador y coordinador

**Fecha:** 2026-09-24

**Estado:** aceptada

## Contexto

El proyecto lo lleva una persona. La auditoría vio que las instrucciones locales de Claude y el `CLAUDE.md` del repositorio se contradecían (cuándo pasa una tarea a shipped, si existe el estado «in review»). Sin reglas de intervención, la coordinación quedaba en la memoria de cada sesión.

## Decisión

Claude Code actúa como desarrollador y como coordinador, con las reglas de intervención del repositorio: una tarea a la vez, propuestas solo en los momentos acordados, y el estado compartido en el repo y en ClickUp, no en la memoria local de cada equipo.

## Alternativas descartadas

- Coordinación manual, sin reglas escritas.
- Usar la IA solo como asistente de código, sin papel de coordinación.

## Consecuencias

Cualquier sesión, en cualquiera de los dos equipos, parte de las mismas reglas. Esas reglas viven en el repositorio; cambiarlas no es un ajuste local.

## Tareas relacionadas

D-02. Tarea `869f6r4ba`.
