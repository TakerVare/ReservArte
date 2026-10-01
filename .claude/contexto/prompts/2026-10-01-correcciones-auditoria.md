# Prompt para la IA de documentación — correcciones de la auditoría de octubre (2026-10-01)

> Preparado por Claude Code tras revisar con Guillermo el informe de la auditoría mensual de octubre
> (hallazgos contrastados con el repositorio). Guillermo lo pega entero, en **modo Agent**, en un chat
> nuevo de Cursor. Copia solo lo que va entre las dos líneas `~~~`.

~~~text
# Correcciones de la auditoría de octubre de 2026

## 0. Auditoría de coherencia (obligatoria, antes de cambiar nada)
Comprueba que está aplicado el prompt de la Fase 3 (2026-10-01): existen ADR-036 a ADR-039 en
Documentation/adr/ y en su índice. Si no, detente y repórtalo.

Fuentes que puedes LEER (no editar): .claude/contexto/decisiones.md (texto exacto de H-46 y de las
decisiones citadas), .claude/rules/*.md, reservarte-web/package.json, los .csproj y
reservarte-web/src (no existen DashboardLayout, Sidebar.vue, Header.vue ni AuthLayout). Si algo de este
prompt contradice esas fuentes, manda el código: repórtalo sin corregirlo.

## 1. Decisiones de Guillermo que aplicas en este prompt
- H-46 (2026-10-01, sustituye a D-20/ADR-020): la app móvil será **nativa en React Native**, publicada
  en App Store y Google Play, **después del piloto** (el piloto arranca con la web), **una app por centro**
  (marca blanca) y **para clientas y personal**. Estimación antigua ≈ 480 h, por revisar al planificarla.
- El presupuesto de un equipo de ~3 FTE (vol. 3 §10.4 y §11.1, 211.140 €) se conserva marcado como
  **presupuesto de un equipo externo, descartado**. La capacidad real tiene una sola fuente: ADR-005
  (Guillermo en solitario, 25 h/semana).
- Normativa: se corrigen los hechos ya; la revisión jurídica sigue en el trámite de RGPD y EIPD.
- Registros de estado: el vol. 3 (roadmap y checklist) se limpia ya; en los vol. 1 y 2 salen las notas
  de SQL Server y el resto se limpia al tocar cada sección (no en este prompt).

## 2. Cambios por documento
### ADR
- Nuevo ADR-040: «App móvil nativa en React Native» (H-46). Contexto: necesidad de presencia en las
  tiendas y de imagen de marca. Decisión: lo de H-46. Alternativas descartadas: PWA sobre la SPA (ADR-020)
  y PWA empaquetada con Capacitor. Consecuencias: segundo frontend en React y doble mantenimiento; una
  publicación y una revisión de Apple por centro; cuentas de desarrollador; el piloto no espera a la app.
  Sustituye a ADR-020: cambia su estado a «sustituida por ADR-040» y actualiza el índice.
### Vol. 1 — Análisis
- §1.3 y §3.1.11: alinéalos con H-46 (React Native, tras el piloto, una app por centro, clientas y
  personal). Si §3.1.11 cita Firebase, déjalo solo como opción de notificaciones push, no como decisión.
- §3.1.2: borra la consecuencia sobre `firstDay` de FullCalendar en CalendarPage (tarea cancelada; no
  hay agenda con FullCalendar, ADR-039).
- §6.2: retitúlalo sin «LOPD»: la ley vigente es la Ley Orgánica 3/2018 (LOPDGDD); la LOPD 15/1999 está
  derogada (corrige también la mención del §1 o donde aparezca). Separa el umbral de 250 empleados (art. 30
  RGPD, exención del registro de actividades) del delegado de protección de datos (art. 37). Donde cite
  centros de datos de AWS en Frankfurt o Irlanda, la región del piloto es eu-south-2 (España, ADR-032).
- §6.1.4 (EIPD): quita las penalizaciones como justificación del perfilado: en el piloto no existen
  (llegan con Redsys, en una fase posterior).
- Política de cookies (§6.2, «Política de Cookies»): añade las cookies de terceros del mapa de Google
  incrustado en Contacto (ADR-036), que necesitan consentimiento previo.
### Análisis de pantallas y estructura.md
- Una sola cabecera: sube la versión (1.1) con fecha de octubre de 2026 y quita el pie de «Octubre 2025,
  versión 1.0».
- Resumen por prioridad: rehaz el cuadro con las pantallas reales (/mis-citas, /contacto, /cuenta,
  /reservar, /citas y su detalle, cancelación); fuera Calendar y Create Wizard (6 steps).
- Árbol de carpetas y nota de navegación: quita DashboardLayout, Sidebar.vue, Header.vue y AuthLayout
  (ya no existen) y la nota que pide retirarlos.
- Nota 10: vue-i18n 11, no v9.
- «Aplicación móvil»: alinéala con H-46 (hoy dice PWA y Capacitor).
### Vol. 2 — Implementación y desarrollo
- Índice: el ancla de §9 tiene la «ó» mal codificada (`#9-seguridad-y-protecciÃ³n-de-datos`): reescríbela
  con la ó del encabezado.
- Notas «Runtime (PR #nn): SQL Server…» y verificaciones de los PR #59 a #66 que describen SQL Server:
  quítalas (el motor es PostgreSQL 18, ADR-033). No limpies el resto del volumen en este prompt.
### Vol. 3 — Planificación y gestión
- Índice: anclas de §11 y §12 con la «ó» mal codificada (`#11-estimaciÃ³n-de-costos`,
  `#12-prÃ³ximos-pasos`): reescríbelas.
- Roadmap y checklist sin registros de estado: quita PRs, «shipped», recuentos de tests, «siguiente» y
  fechas de entrega; deja el plan y enlaza el vol. 1 §3.1.5 y el vol. 2 §9.9 para lo implementado. En
  concreto:
  - la «lectura del roadmap» (citas 5/12, Servicios 5/6) y la nota de la máquina de estados «sin
    endpoints»;
  - la nota de empleados que dice «Servicio + validadores + AutoMapper» (el mapeo es Mapperly, ADR de
    Mapperly);
  - mes 4: la política de penalización, su cálculo, la captura parcial y «pre-autorizaciones y
    penalizaciones funcionando» salen del MVP y van a la fase de Redsys;
  - mes 5: la lista de espera, solo como post-piloto, sin casilla de trabajo del piloto;
  - el cierre: quita «Siguiente tarea de desarrollo».
- Mes 1, esquema y cronograma: vue-i18n 11 (ya instalado), no «instalar vue-i18n 9».
- §10.2: quita la descripción del Sidebar y los layouts como código existente.
- Meses 6-7 (app móvil): mantenlos en React Native según H-46, como fase posterior al piloto, una app
  por centro y para clientas y personal; horas por revisar.
- §10.4 y §11.1: márcalos como «presupuesto de un equipo externo, descartado» con un enlace a ADR-005
  para la capacidad real. Corrige su aritmética: las dedicaciones del equipo del MVP suman 3,0 FTE, no
  ~3,5.
- §11.4 (legal): rehaz los totales como suma de sus filas (puntual 1.400-2.700 €; anual con delegado ×
  12, revisión y PCI, 3.460-7.900 €), o ajusta las filas si los totales eran los buenos, y dilo.
- Servicios externos: el subtotal de ~70 €/mes no tiene fila para unos 10 €: añade la fila que falta o
  baja el subtotal a ~60 €.
- §11.5: los 119 € de Apple y Google Play se mantienen (H-46), pero en la fase de la app, no en el
  piloto.
- §12.1: «Configurar VPC en región eu-west-1 (Irlanda)» → eu-south-2 (España, ADR-032).
- Cierre del volumen: alinéalo con la cabecera 1.3 (fecha y versión) y quita «es viable
  económicamente» y «cumple con toda la normativa» hasta recalcular §11.6 y §11.7 y cerrar el trámite
  legal.
- Checklist §12.2: la guía de user secrets enlaza las tarjetas de prueba de Redsys, no las incluye; y no
  hay «Clean Architecture» con carpeta src/: remite al vol. 1 §4.1 y al ADR-015.
### Otros
- Scripts de instalación.md: quita sidebarOpen y toggleSidebar del store de ejemplo. Deja la
  instalación de @fullcalendar/* con una nota: sin uso desde ADR-039; se decide tras el MVP (DP-07).
  Añade versión y fecha a la cabecera (y a la guía de user secrets).
- accessibility-and-i18n.md: fecha de octubre de 2026 en la cabecera (el contenido ya es de Reka UI y
  vue-i18n 11).
- Datos duplicados: la app móvil (ADR-040), la región (ADR-032) y vue-i18n (vol. 1 §4.1.2) quedan con
  una sola fuente: en el resto, una frase y un enlace.

## 3. Restricciones
- No reescribas ADR aceptados salvo el estado de ADR-020. ADR-018, ADR-021, ADR-032 y ADR-034 se quedan
  como están (su texto es historia; ya lo comprobó la auditoría).
- No recalcules AWS, break-even ni ROI: siguen «por recalcular» hasta tener el precio de RDS en
  eu-south-2.
- No añadas registros de estado, PRs ni recuentos. Un dato, una fuente: enlaza en vez de copiar.
- No verifiques IDs de ClickUp. Si algo contradice otro documento o una decisión, repórtalo sin
  corregirlo.

## 4. Advertencias
Señala todo lo que veas dudoso o desfasado, aunque no esté en este prompt.
~~~
