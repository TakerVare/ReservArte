# Plantillas de prompt para la IA de documentación

> Las usa Claude Code en `/cerrar-bloque` y en la auditoría mensual. Rellena lo que va entre `<>` y
> borra lo que no aplique. Las reglas de fondo están en `gestion.md` §9.

## A. Prompt de cierre de bloque

~~~text
# Documentación del bloque <nombre> (<ID de ClickUp>)

## 0. Auditoría de coherencia (obligatoria, antes de cambiar nada)
Comprueba que está aplicado el prompt anterior (<bloque o tarea, fecha>). Señales: <dos o tres
cambios concretos que deberían verse>. Si no lo está, detente y repórtalo.

## 1. Qué se ha hecho
<Por tarea: ID, PR y qué cambia para la documentación. Es contexto para ti: no lo copies como
registro de estado en los volúmenes.>

## 2. Cambios por documento
### Vol. 1 — Análisis
- §<x.y>: <cambio concreto>
### Vol. 2 — Implementación y desarrollo
- §<x.y>: <cambio concreto>
### Vol. 3 — Planificación y gestión
- <solo plan y decisiones de gestión; nada de estado>
### Otros (estrategia de testing, guía de secrets, accesibilidad, Redsys, scripts)
- <documento, sección y cambio>

## 3. ADR
- Nuevo: <título>. Contexto: … Decisión: … Alternativas descartadas: … Consecuencias: …
- Sustituido: ADR-<NNN> por <nuevo>.

## 4. Restricciones
- No añadas registros de estado, PRs ni recuentos a los volúmenes.
- Un dato, una fuente: enlaza en vez de copiar.
- No verifiques IDs de ClickUp.
- Si algo contradice otro documento o una decisión, repórtalo sin corregirlo.

## 5. Advertencias
Señala todo lo que veas dudoso o desfasado, aunque no esté en este prompt.
~~~

## B. Prompt de auditoría completa mensual

~~~text
# Auditoría completa mensual de la documentación — <mes y año>

## 0. Auditoría de coherencia
Lista los prompts que no estén aplicados del todo.

## 1. Revisión completa
Aplica la lista de «Auditoría completa mensual» de tus instrucciones a todos los documentos de
Documentation/, incluidas las secciones que ningún prompt ha tocado.

## 2. Informe
Por hallazgo: documento y sección, qué pasa, gravedad (alta, media o baja) y propuesta.
No corrijas nada en esta pasada.
~~~

## C. Comprobación antes de entregarlo

- Cada cambio indica documento y sección.
- Están todas las tareas del bloque y sus decisiones, con su ID de `decisiones.md`.
- Pide la auditoría de coherencia y las advertencias.
- No pide verificar IDs de ClickUp ni registrar estado en los volúmenes.
- Si es largo, entrégalo también como fichero.
