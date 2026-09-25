---
name: traspaso
description: "Deja el trabajo de ReservArte listo para seguir en el otro equipo de Guillermo (Mac o Windows) o en otra sesión, a mitad de una tarea. Guarda el trabajo con un commit convencional, escribe en el estado.md de la rama qué está hecho, qué falta y el siguiente paso exacto, y lo sube todo. Úsala cuando Guillermo diga que cambia de equipo, que lo deja por hoy, que se va o que hará una pausa larga con una tarea a medias."
---

# /traspaso

Objetivo: que la siguiente sesión, en cualquiera de los dos equipos, retome sin preguntar nada.

1. `git status`. Comprueba que no se cuela nada que no deba subirse: secretos, `.env` locales,
   `sent-emails/`, bases de datos o volcados.
2. Si la build no compila o hay tests en rojo, dilo en el estado: no lo ocultes.
3. Commit del trabajo con un mensaje convencional que marque que está a medias, por ejemplo
   `feat(citas): endpoints de alta y consulta (wip)`.
4. `estado.md` de la rama → «Tarea en curso», con:
   - ID de ClickUp, rama y objetivo;
   - hecho, con la evidencia que haya;
   - pendiente, en orden;
   - **siguiente paso exacto** (fichero, método o comando);
   - decisiones tomadas en la sesión y dudas abiertas;
   - cuidados: migraciones sin aplicar en el otro equipo, datos de prueba creados, servicios
     levantados.
5. Commit `chore(contexto): traspaso <id>` y `git push -u origin <rama>`.
6. Confirma a Guillermo el commit y la rama, y recuérdale qué hacer al llegar al otro equipo: abrir
   Claude Code en el repo y ejecutar `/estado`.

Sin tarea en curso, basta con comprobar que `develop` está subido y `estado.md` al día.
