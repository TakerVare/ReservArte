---
name: estado
description: "Arranque de sesión y parte de estado de ReservArte. Sincroniza git, localiza la tarea en curso aunque se empezara en el otro equipo, lee el estado.md que manda, consulta ClickUp y resume dónde estamos, qué se hizo, qué toca y qué espera a Guillermo. Úsala al empezar cualquier sesión, al volver de cambiar de equipo y siempre que Guillermo pregunte por el estado, el avance, qué falta, cómo vamos o dónde nos quedamos, aunque no diga «estado»."
---

# /estado — arranque y parte de estado

Guillermo alterna entre un Mac y un Windows. El `estado.md` que se cargó con `CLAUDE.md` es la copia
del disco de este equipo y puede ir por detrás del remoto: esta skill reconstruye el estado real
antes de hacer nada.

## 1. Sincroniza git

1. `git status`. Si hay cambios sin commitear, identifica de quién son (IA de documentación, trabajo
   a medias de otra sesión) y no los pierdas ni los mezcles.
2. `git fetch --prune`.
3. Con el árbol limpio, `git pull` de la rama actual (normalmente `develop`).

## 2. Localiza la tarea en curso

Cruza tres fuentes:
- `git show origin/develop:.claude/contexto/estado.md` → «Tarea en curso».
- `git branch -r --no-merged origin/develop` → ramas `feature/*` vivas.
- ClickUp: tareas en `in development`, `in progress` o `in review`, con una sola consulta de
  `clickup_filter_tasks` (cuida el límite diario de llamadas).

Si hay una rama en curso, su estado es el que manda:
`git show origin/<rama>:.claude/contexto/estado.md`. Si este equipo no la tiene en local,
`git checkout <rama>` (seguirá a `origin/<rama>`). Si las tres fuentes no coinciden, dilo y pregunta
antes de actuar.

## 3. Revisa el entorno de este equipo

- Si el pull trajo cambios en `ReservArte-Infrastructure/Persistence/Migrations/`
  (`git diff --name-only HEAD@{1} HEAD`), ejecuta `dotnet ef migrations list` y avisa de las
  `(Pending)`.
- Si cambió `reservarte-web/package-lock.json`, recuerda `npm ci`.
- En la primera sesión del mes, recuerda la auditoría mensual de documentación.
- Revisa en `estado.md` los trámites externos y las decisiones pendientes cuyo disparador esté cerca.

## 4. Informa

Con el formato de `.claude/contexto/gestion.md` §12, breve. No empieces ninguna tarea: espera el OK
de Guillermo.
