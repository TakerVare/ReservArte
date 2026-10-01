# Prompt para la IA de documentación — auditoría completa mensual de octubre de 2026

> Preparado por Claude Code el 2026-10-01 (primera sesión del mes). **Se aplica después del prompt de
> la Fase 3** (`prompts/2026-10-01-fase-3-frontend.md`). Guillermo lo pega entero, en **modo Agent**,
> en un chat nuevo de Cursor. Copia solo lo que va entre las dos líneas `~~~`. Esta pasada **no
> corrige nada**: entrega un informe para que Guillermo decida.

~~~text
# Auditoría completa mensual de la documentación — octubre de 2026

## 0. Auditoría de coherencia
Lista los prompts que no estén aplicados del todo. El último es el de la Fase 3 (2026-10-01).
Señales: existen Documentation/adr/ADR-036 a ADR-039 y figuran en el índice de
Documentation/adr/README.md; «Análisis de pantallas y estructura.md» describe /mis-citas, /reservar
y /citas. Si no está aplicado, repórtalo al principio del informe y sigue con la revisión.

## 1. Revisión completa
Aplica la lista de «Auditoría completa mensual» de tus instrucciones a todos los documentos de
Documentation/, incluidas las secciones que ningún prompt ha tocado:
- secciones estáticas: stack y estructura, metodología, equipo y cifras;
- aritmética de las cifras de negocio (totales, MRR frente a precios);
- datos duplicados entre documentos y si coinciden;
- referencias normativas (accesibilidad, RGPD) y cabeceras (versión, fecha, autor);
- enlaces internos rotos.

Comprueba además, en concreto, estos puntos que ya sabemos dudosos (confirma o descarta cada uno):
1. Vol. 3, bloque de Citas del MVP: aún incluye la penalización, la lista de espera y el contador de
   no-shows, dice «5/12» y que faltan los endpoints. Esas partes salieron del piloto (la lista de
   espera, a post-piloto; la penalización, con Redsys en la Fase 7) y los endpoints existen.
2. Vol. 3, meses 6-7 y cuadro de costes: siguen con «Mobile Developer (React Native)», 480 h y
   19.200 € dentro de los 211.140 €, contra el ADR-020 (PWA).
3. Registros de estado en los volúmenes 1-3 y en el checklist del vol. 3 (PRs, recuentos de tests,
   «siguiente», fechas de entrega), incluidas las notas históricas «Runtime (PR #nn): SQL Server…».
   Vol. 2 §9.9 en concreto.
4. Vol. 3 §12.1: «Configurar VPC en región eu-west-1»; la región es eu-south-2 (D-29).
5. Vol. 3 §11.2, §11.6 y §11.7: filas de RDS de 5 y 50 centros, sus totales, el break-even y el ROI
   marcados «por recalcular»: comprueba que lo están y que la aritmética del resto cuadra.
6. «Análisis de pantallas y estructura.md»: versión y fecha de la cabecera.
7. Cualquier mención a FullCalendar, al wizard de citas de 6 pasos, al Sidebar o Header, a la
   gestión de usuarios genéricos o a VITE_API_BASE_URL que sobreviva al prompt de la Fase 3.
8. Coherencia entre el stack de los volúmenes y el real del repo (reservarte-web/package.json y los
   .csproj): .NET 10, PostgreSQL 18, Vue 3.5, Vite 8, Tailwind 3.4, Reka UI, vue-i18n 11, Vitest,
   AwesomeAssertions, Mapperly; sin MediatR, AutoMapper ni FluentAssertions.

Fuentes que puedes LEER para contrastar (no editar): .claude/contexto/decisiones.md,
.claude/rules/*.md, reservarte-web/package.json, los .csproj y data/schema/create_ReservArteDB.sql.

## 2. Informe
Por hallazgo: documento y sección, qué pasa, gravedad (alta, media o baja) y propuesta.
No corrijas nada en esta pasada.
~~~

## Al recibir el informe

Claude Code lo repasa con Guillermo contrastando cada hallazgo con el código y las decisiones (hay
falsos positivos conocidos: `AspNet.Security.OAuth.Apple` 10.0.0 tiene numeración propia; ADR-032 cita
los 133 € como presupuesto antiguo; ADR-021 sigue «pendiente» porque no se reescribe). Lo que se
acepte va en un prompt de correcciones.
