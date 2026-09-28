# ADR-026: RGPD del piloto

**Fecha:** 2026-09-24

**Estado:** aceptada

## Contexto

El piloto trata datos de clientas, y las alergias son datos de salud. La auditoría tenía parados el contrato de encargo, el registro de actividades y la EIPD, que son trámites externos y no horas de código.

## Decisión

Para el piloto: contrato de encargo de tratamiento, registro de actividades y EIPD. El cifrado se decide según el resultado de la EIPD, no antes.

## Alternativas descartadas

No quedó registrada una alternativa descartada.

## Consecuencias

No se fija un cifrado «por si acaso» al margen de la EIPD. Los textos y el encargo forman parte de los trámites que arrancan ya (ADR-022).

## Tareas relacionadas

D-26. Tareas `869f6r7b3`, `869f74ua4`. Relacionado con ADR-022.
