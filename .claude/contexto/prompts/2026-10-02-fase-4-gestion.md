# Prompt para la IA de documentación — cierre del bloque de la Fase 4 (2026-10-02)

> Preparado por Claude Code en `/cerrar-bloque` (bloque `869d7edt7`). Guillermo lo pega entero, en
> **modo Agent**, en un chat nuevo de Cursor. Copia solo lo que va entre las dos líneas `~~~`.

~~~text
# Documentación del bloque «Gestión: empleados, clientes y servicios» (869d7edt7)

## 0. Auditoría de coherencia (obligatoria, antes de cambiar nada)
Comprueba que está aplicado el prompt de correcciones de la auditoría de octubre (2026-10-01).
Señales: existe Documentation/adr/ADR-040 y figura en el índice; ADR-020 está «sustituida por
ADR-040»; el vol. 3 §12.1 dice eu-south-2 y no eu-west-1. Si no lo está, detente y repórtalo.

Fuentes que puedes LEER (no editar) para contrastar: .claude/contexto/decisiones.md (texto exacto de
H-47), .claude/rules/*.md (contrato-api.md tiene los endpoints exactos), reservarte-web/src y los
controladores de ReservArte-API. Si algo de este prompt contradice esas fuentes, manda el código:
repórtalo sin corregirlo.

## 1. Qué se ha hecho (contexto; no lo copies como registro de estado)
- 869d7fbyt + 869d7fc0h — Empleados: lista con foto (o iniciales), rol y filtro Activos/De baja;
  ficha con Datos (añade la fecha de alta en el centro), Horario semanal (tramos por día, lunes = 0,
  la semana entera en un PUT) y Ausencias (vacaciones, baja, asuntos propios, formación u otro; se
  piden en hora del centro y viajan en UTC). El alta lleva a la ficha nueva.
- 869faz10y — Servicios que presta cada empleado: GET y PUT /api/v1/employees/{id}/services (el PUT
  reemplaza el conjunto entero; lo que sale se da de baja lógica y conserva su nivel; lo nuevo nace
  con nivel 1; servicio inexistente, retirado o de otro centro → 400 serviceIds[i] UnknownService) y
  pestaña «Servicios» en la ficha, solo con casillas (el nivel de destreza no se muestra). Antes, un
  empleado dado de alta desde la app no aparecía en la reserva.
- 869d7fc34 + 869d7fc51 — Clientes: lista con foto, categoría, bloqueo y baja; filtros de estado y
  categoría; ficha con Datos (alta con consentimientos RGPD: tratamiento de datos obligatorio,
  comerciales, fotografías y WhatsApp; sin tarjeta guardada en el piloto), Notas (con su autora:
  CustomerNoteDto.employeeName), Alergias e historial de Citas.
- 869fazwwe — Ficha de clienta completa: PUT /api/v1/customers/{id}/consents/{consentType} con
  { granted } (Admin y Manager; retirar conserva grantedAt y sella revokedAt; retirar
  data_processing da de baja la ficha, H-47); POST/PUT/DELETE /api/v1/customers/{id}/allergies (todo
  el personal; baja lógica) y POST /api/v1/customers/{id}/block ({ reason } obligatorio, ≤500) y
  /unblock (Admin y Manager). En desarrollo, el seeder da ficha de empleado a guille@svalero.com.
- 869d7fc6b — Servicios: lista con categoría, duración y precio; ficha con Datos (categoría nueva
  desde un diálogo; prueba de alergia con su antelación) y Variaciones (ajuste de precio y duración y
  total resultante). Las tarifas por nivel, los paquetes y la gestión completa de categorías no
  tienen pantalla.
- El dashboard (pantalla) salió del bloque: es opcional en la Fase 6 (869fb3r11, con 869f7axcv y
  869f6r6nx).

## 2. Cambios por documento
### Vol. 1 — Análisis
- §3.1.2 Gestión de Empleados: servicios que presta cada empleado (quién sale en la reserva de cada
  servicio), horario semanal por tramos y ausencias por tipo, en hora del centro.
- §3.1.3 Gestión de Clientes: consentimientos que se dan y se retiran después del alta, con sus
  fechas; retirar el de tratamiento de datos da de baja la ficha (H-47); alergias con gravedad;
  bloqueo con motivo (bloqueada, no reserva); notas con su autora.
- §3.1.4 Catálogo de Servicios: alta de categorías desde la ficha; variaciones con su total
  resultante. Las tarifas por nivel no se aplican en el piloto (si el texto dice otra cosa, repórtalo).
- §5.1 (bloque de contratos de la API): añade los endpoints nuevos con el formato de los demás
  (rol, cuerpo, códigos): GET/PUT /employees/{id}/services; PUT /customers/{id}/consents/{consentType};
  POST/PUT/DELETE /customers/{id}/allergies[/{allergyId}]; POST /customers/{id}/block y /unblock. En
  POST /customers/{id}/notes, CustomerNoteDto pasa a llevar employeeName.
- §6.1.3 Consentimientos Necesarios: la retirada del consentimiento de tratamiento de datos da de
  baja la ficha y conserva el historial; la supresión completa es un trámite aparte (enlaza ADR-041).
- §5.2: el DDL sigue en T-SQL (NVARCHAR, UNIQUEIDENTIFIER, NEWID()); sustitúyelo por un enlace a
  data/schema/create_ReservArteDB.sql como fuente única (motor PostgreSQL 18). (Pendiente de la
  auditoría de octubre.)
- Nota menor de SQL Server: el paquete de Hangfire retirado. (Pendiente de la auditoría.)

### Análisis de pantallas y estructura.md
- §3 Empleados, §4 Clientes y §5 Servicios: describe las pantallas reales: /empleados, /empleados/:id
  (Datos, Servicios, Horario, Ausencias), /clientes, /clientes/:id (Datos con consentimientos y
  bloqueo, Notas, Alergias, Citas), /servicios, /servicios/:id (Datos, Variaciones). Lista y ficha
  siguen el patrón de Figma «CRUD» (387:56720) y «Detalle usuario» (387:56778); se entra desde el
  Área de administración de «Mi cuenta».
- Resumen por prioridad: marca estas pantallas como las del MVP de gestión; el dashboard es opcional.
- Árbol de carpetas: añade pages/employees, pages/customers y pages/services y quita PublicLayout.vue,
  Footer.vue, CalendarView.vue y AppointmentWizard.vue, que no existen. (Lo segundo, pendiente de la
  auditoría.)

### Vol. 2 — Implementación y desarrollo
- §9.2.5 Patrones de la SPA: fichas con pestañas en la URL (?tab=), listados con DataList y foto
  (Avatar: foto o iniciales), ConfirmDialog para las bajas, apiPagedRequest para meta.pagination,
  CENTER_TIME_ZONE (Europe/Madrid) para pasar ausencias y pruebas de alergia a UTC, y páginas de
  gestión con carga diferida. Los <input type="number"> vacíos cuentan como «falta», no como 0.
- §9.6 Empleados: la asignación de servicios (tabla EmployeeServices) se reemplaza entera, con baja
  lógica y nivel conservado; en desarrollo, DevSeeder asegura la ficha de empleado del admin del
  piloto (sin horario ni servicios).
- §9.7 Clientes: consentimientos (una fila vigente por finalidad; retirar conserva la fecha de
  concesión), alergias (baja lógica; una retirada no se edita) y bloqueo; repositorios acotados al
  centro también para consentimientos y alergias.
- §9.8 Servicios: lo que consume la pantalla (categorías sin filtro para no perder la retirada de un
  servicio ya guardado; variaciones con duración resultante positiva).

### Vol. 3 — Planificación y gestión
- Fase 4 (plan, sin estado): el paso de empleados incluye los servicios que presta cada uno y el de
  clientes la ficha completa (consentimientos, alergias y bloqueo); el dashboard queda opcional en la
  Fase 6. No añadas PRs, fechas de entrega ni recuentos.
- Mes 1: «Crear solución con Clean Architecture» → remite al vol. 1 §4.1 y a ADR-015. (Pendiente de la
  auditoría.)
- Nota menor de SQL Server del PR #76: quítala. (Pendiente de la auditoría.)

### Otros
- reservarte-testing-strategy.md: en los E2E con la API simulada, un spec que no simula todas sus
  llamadas registra primero una ruta de reserva (**/api/v1/** → respuesta vacía) y después las suyas;
  sin ella, con la API real en marcha, una llamada no simulada recibe 401 y cierra la sesión.

## 3. ADR
- Nuevo ADR-041: «Retirada del consentimiento de tratamiento de datos» (H-47). Contexto: el
  consentimiento de tratamiento de datos es obligatorio para tener ficha y se podía dar pero no
  retirar. Decisión: retirarlo desde la ficha la da de baja (sale de la reserva y de las listas) y
  guarda la fecha de retirada; el historial se conserva por obligación legal; la supresión completa
  es un trámite aparte, cuando se cierre el procedimiento de RGPD (869f6r7b3). Alternativas
  descartadas: no permitir retirarlo desde la ficha; retirarlo sin efecto (clienta activa sin base
  legal). Consecuencias: «Reactivar» todavía no exige el consentimiento, porque las altas con Google
  no lo tienen; se resuelve con el trámite de RGPD. Añádelo al índice y enlázalo desde §6.1.3.

## 4. Restricciones
- No añadas registros de estado, PRs ni recuentos a los volúmenes.
- Un dato, una fuente: enlaza en vez de copiar (los contratos exactos están en el vol. 1 §5.1).
- No verifiques IDs de ClickUp.
- No reescribas ADR aceptados.
- Si algo contradice otro documento o una decisión, repórtalo sin corregirlo.

## 5. Advertencias
Señala todo lo que veas dudoso o desfasado, aunque no esté en este prompt.
~~~
