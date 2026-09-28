# ADR-010: Salir de las licencias comerciales de librerías

**Fecha:** 2026-09-24

**Estado:** aceptada

## Contexto

AutoMapper y MediatR pasaron a doble licencia, con edición comercial de pago. FluentAssertions, desde la versión 8, exige licencia de pago para uso comercial. MediatR estaba instalado y no se usaba. La regla de no fijar versión había metido FluentAssertions 8. El proyecto se vende: una dependencia de pago no examinada no cabe.

## Decisión

MediatR sale del proyecto. AutoMapper se sustituye por Mapperly. FluentAssertions 8 se sustituye por una librería de aserciones con licencia permisiva. Toda dependencia entra con versión fijada y licencia revisada para uso comercial.

## Alternativas descartadas

- Comprar las licencias comerciales de AutoMapper, MediatR y FluentAssertions 8.

## Consecuencias

Este ADR decide salir de esas licencias. No elige el sustituto concreto del mapeo ni el de las aserciones: eso está en ADR-030 y ADR-029, que no sustituyen a este. No se reintroducen MediatR, AutoMapper ni FluentAssertions.

## Tareas relacionadas

D-10. Tarea `869f6r5eu` y sus subtareas. Relacionado con ADR-029 y ADR-030.
