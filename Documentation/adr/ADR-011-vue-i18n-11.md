# ADR-011: vue-i18n pasa a la versión 11

**Fecha:** 2026-09-24

**Estado:** aceptada

## Contexto

El `package-lock` marca vue-i18n 9 como sin soporte y pide pasar a la 11. La internacionalización ya está en el arranque de la SPA, con el español como locale base.

## Decisión

vue-i18n pasa a la versión 11.

## Alternativas descartadas

- Mantener la versión 9, sin soporte.

## Consecuencias

Hasta que la migración esté hecha, la SPA sigue en la 9. Los documentos que describen la instalación nombran la 9 y enlazan esta decisión, para no instalar la 11 por adelantado ni presentar la 9 como destino.

## Tareas relacionadas

D-11. Tarea `869f6r6dk`.
