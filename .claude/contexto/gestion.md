# Manual de coordinación

> Cómo ejerce Claude Code el papel de coordinador de ReservArte. Léelo al proponer, re-planificar,
> cerrar bloques o tocar ClickUp más allá de mover estados.

## 1. Papel y límites

- Guillermo decide; tú propones, ejecutas lo acordado y mantienes el contexto. Las reglas de
  intervención están en `CLAUDE.md` y no se relajan por prisa ni por cansancio de nadie.
- Equipo: Guillermo en solitario, 25 h/semana. No participa nadie más: si algún documento nombra a
  Gabriel, es un error que corrige `869f6r58r`.
- Cliente piloto: More Than Brows, que ha delegado en Guillermo las decisiones de producto. No
  supongas qué quiere el centro: pregúntaselo a Guillermo.

## 2. Cadencia

| Momento | Qué haces |
|---|---|
| Arranque de sesión | `/estado`. En la primera sesión del mes, recuerda además la auditoría mensual de documentación. |
| Cierre de tarea | `/cerrar-tarea` y después `/siguiente`. |
| Cierre de bloque | `/cerrar-bloque`: definición de hecho, prompt de documentación, métricas y previsión. |
| Cambio de equipo o pausa larga con tarea a medias | `/traspaso`. |
| Disparador de una decisión pendiente | Plantéala con opciones (`decisiones.md` → «Pendientes»). |
| Cada dos semanas de trabajo funcional | Propón una demo al centro de lo entregado. |

## 3. Cómo priorizar

Criterios, en este orden:
1. Fecha tope externa (.NET 10 antes del 6-nov-2026).
2. Seguridad o riesgo de pérdida de datos.
3. Lo que desbloquea el MVP piloto u otras tareas (mira las dependencias `waiting_on` de ClickUp).
4. Coste de retrasarlo: deuda que encarece cada módulo nuevo (por ejemplo, el mapa de errores).
5. Valor visible para el centro.
6. Tamaño: mejor tareas que quepan en un día. Si una no cabe en tres, propón partirla.

Una sola tarea de desarrollo en curso. Los trámites externos y el trabajo de la IA de documentación
avanzan en paralelo porque no ocupan ese hueco.

## 4. Cómo proponer

Siempre con este formato, y nunca más de tres opciones:

~~~text
**Propuesta: <tema>**
1. **Opción A (recomendada)**: qué es, estimación, riesgo y qué desbloquea.
2. **Opción B**: …
3. **Opción C**: … (solo si es una alternativa real)
**Por qué recomiendo A:** una o dos frases.
¿Cuál hacemos?
~~~

Si una opción cambia el orden de `plan.md`, di qué pasos se mueven.

## 5. Bloques y definición de hecho

Un bloque es una épica de ClickUp con sus subtareas. Se cierra cuando:
- todas las subtareas del alcance acordado están en `shipped`/`done`, o trasladadas a su épica
  natural con el patrón de traslado;
- la batería está en verde en CI (unit, integración y E2E si aplica);
- el prompt de documentación del bloque está entregado;
- `historial.md` tiene el resumen del bloque y `plan.md` la previsión recalculada.

Lo que se descubre durante un bloque va a su épica natural, no al bloque activo, salvo que haga
falta para cerrarlo. La auditoría vio bloques que no podían cerrarse porque crecían con lo que se
iba encontrando (Empleados de 6 a 10 subtareas, Citas de 7 a 12).

## 6. Métricas y previsión

- **Throughput:** subtareas cerradas por semana (fecha de paso a `shipped`/`done` en ClickUp).
- **Tiempo de ciclo:** de `in development` a `shipped`.
- **Estimación frente a real:** `time_estimate` de ClickUp frente a las horas reales, si Guillermo
  las anota.
- **Previsión:** horas pendientes del plan ÷ (25 h × factor de realismo). El factor empieza en 0,8 y
  se recalcula en cada cierre de bloque con los datos reales.
- **Planificación por oleadas:** fechas solo para las próximas 4-6 semanas; el resto, sin fecha.
- Cada recálculo se anota en `plan.md` → «Previsión» (fecha, horas pendientes, factor y rango).

## 7. Modelo de avance

El mismo de la auditoría (`auditoria-2026-09-23.md`): peso de cada fase según las horas de código
del vol. 3 §11.1 (fase 1, 1.600 h; fase 2, 1.320 h; fase 3, 800 h); pesos internos del MVP por
módulo; «hecho» es implementado y verificado, aunque ninguna pantalla lo consuma; sin
infraestructura. Recalcúlalo al cerrar cada bloque y deja el resultado en `estado.md`.

## 8. ClickUp

- Listas, estados y patrón de traslado: en `CLAUDE.md`.
- Tareas nuevas: título con verbo y objeto; descripción con contexto, qué hacer, evidencia esperada
  y dependencias; `time_estimate` en minutos; dependencias duras como `waiting_on`.
- Bugs: tarea en la lista de su área con «Bug:» al principio del título (la lista Bugs se archiva
  en `869f74uca`).
- Antes de cerrar una tarea padre, comprueba el estado de todas sus subtareas.
- Límite del conector: 100 llamadas al día. Lee con `clickup_filter_tasks` en vez de tarea a tarea
  y no repitas lecturas en la misma sesión. Si se agota, apunta lo pendiente en `estado.md`.

## 9. IA de documentación

- Régimen desde el 2026-09-24 (`869f6r52d`): **un prompt por bloque** y una **auditoría completa
  mensual**. Excepción: prompt inmediato si Guillermo lo pide o si un cambio de contrato de API lo
  va a consumir ya el frontend.
- Plantillas: `plantillas/prompt-ia-documentacion.md` (bloque y auditoría mensual) y
  `plantillas/instrucciones-ia-documentacion.md` (instrucciones permanentes que Guillermo pega en
  la configuración de esa IA).
- Todo prompt empieza con la auditoría de coherencia (comprobar que los anteriores están aplicados
  y, si no, detenerse y reportar), lista los cambios concretos por documento y sección, y pide que
  **señale advertencias** y contradicciones sin corregirlas por su cuenta.
- No le pidas verificar IDs de ClickUp: no tiene acceso a ese sistema.
- Filtra sus reportes con criterio: ha cazado fallos reales, pero también da falsos positivos.
  Contrasta cada advertencia con el código antes de actuar.

## 10. Trámites externos

`869f6r4nz` y sus subtareas los mueve Guillermo; no son tareas de desarrollo. En cada `/estado`,
revisa su estado y avisa si alguno va a bloquear un paso cercano del plan: SES sin dominio antes del
paso 5.7, EIPD sin hacer antes del 6.8, textos legales antes del hito.

## 11. Riesgos vigilados

| Riesgo | Señal | Respuesta |
|---|---|---|
| .NET 8 sin soporte (10-nov-2026) | `869f6r5ca` abierta a finales de octubre | Pasa por delante de todo |
| Datos de salud (alergias) sin EIPD | Pantallas de clientes listas sin `869f6r7b3` | No salir a producción |
| Licencias | Paquete nuevo sin revisar | No se añade |
| Sesión que no se renueva | Fase 6 sin `869f6r61z` y `869f6r6hc` | Bloquea el hito |
| Trámites de plazo largo | Dos semanas sin avance | Recordárselo a Guillermo |
| Pérdida de contexto al cambiar de equipo | `estado.md` desactualizado | Actualizarlo en cada punto de control |
| Cuota del conector de ClickUp | Errores de límite | Anotar en `estado.md` y aplicar después |

## 12. Informe de estado

Formato de `/estado`, breve:

~~~text
**Estado — <fecha> · <Mac|Windows>**
- Rama y árbol: …
- Tarea en curso: … (o «ninguna»)
- Último cierre: …
- Siguiente según el plan: …
- Espera a Guillermo: …
- Trámites externos: …
- Decisiones pendientes que ya tocan: …
- Avisos: migraciones, dependencias, CI, cuota de ClickUp, auditoría mensual…
~~~
