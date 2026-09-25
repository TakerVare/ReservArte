---
name: cerrar-tarea
description: "Cierra una tarea de ReservArte siguiendo el flujo estricto en dos mitades. Antes del merge, reúne la evidencia, rellena la plantilla de PR, abre el PR, pasa la tarea a in review y se detiene. Tras el aviso de merge, hace pull y build, pasa la tarea a shipped, actualiza y sube estado.md e historial.md, decide la documentación del bloque y propone la siguiente. Úsala siempre que una tarea esté terminada, cuando Guillermo diga que ha mergeado un PR, o cuando pida cerrar, entregar o abrir el PR."
---

# /cerrar-tarea

Dos mitades, separadas por el merge, que hace Guillermo.

## Mitad 1 — antes del merge

1. **Reúne la evidencia real** según el tipo de cambio; no la supongas:
   - Backend: `dotnet build` sin errores ni avisos nuevos, `dotnet test` con el recuento y
     `dotnet format --verify-no-changes` con código 0 (sin `| tail`).
   - API: respuestas HTTP reales con envelope, por rol, y aislamiento entre organizaciones si aplica.
   - Base de datos: base desechable creada con los scripts de `data/` y la API arrancada contra
     ella; nunca `ReservArteDB`.
   - Frontend: `npm run lint`, `npm run build` y los E2E afectados (`npm run test:e2e` en el Mac).
2. Si es una tarea padre, comprueba que todas sus subtareas están cerradas.
3. Rellena `.github/PULL_REQUEST_TEMPLATE.md` con esa evidencia y abre el PR contra `develop`.
4. ClickUp: Backend, Frontend y Mobile → `in review`. Infra sigue en `in progress` hasta el merge.
   Docs → `in review` al entregar el prompt a la IA de documentación.
5. En la rama: `estado.md` → «PR #N abierto, esperando revisión», más lo que quede por decidir.
   Commit `chore(contexto): pr <id>` y push.
6. **Para.** Dile a Guillermo qué conviene revisar y espera su aviso de merge.

## Mitad 2 — después del merge

1. `git status`: si hay cambios de la IA de documentación sin commitear, avisa y no los mezcles con
   los tuyos.
2. `git checkout develop && git pull && dotnet build` (y `npm ci` si cambió el lockfile).
3. ClickUp → `shipped` (Infra: `done`; Docs: `publish` cuando Guillermo confirme que está aplicado).
   Si era la última subtarea de un padre, dilo: puede cerrar el bloque.
4. `historial.md` → nueva entrada arriba, en «Entradas»: fecha, ID, PR, qué se hizo, decisiones (si
   hay nuevas, añádelas también a `decisiones.md` con su ID), evidencia y batería.
5. `estado.md`: «Tarea en curso: ninguna», último cierre, batería, siguiente paso del plan y, si la
   tarea no cierra su bloque, lo que habrá que documentar en «Documentación acumulada».
6. Añade solo esos ficheros (`git add .claude/contexto/...`), commit `chore(contexto): cierra <id>`
   y push.
7. Si la tarea cierra su bloque → `/cerrar-bloque`. Si no → `/siguiente`.
