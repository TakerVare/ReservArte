# ADR-037: Clientes y empleados por separado, sin gestión de usuarios genéricos

**Fecha:** 2026-10-01

**Estado:** aceptada

## Contexto

El Área de administración tenía una entrada «Usuarios» pensada como gestión genérica de cuentas. Clientes y empleados ya son módulos distintos en el dominio y en la API.

## Decisión

No hay gestión de usuarios genéricos (H-43). Clientes y empleados se gestionan por separado. En el Área de administración, «Usuarios» pasa a «Clientes» y abre `/clientes`. No existe `/usuarios`.

Sustituye la parte de H-42 que daba a «Usuarios» una pantalla propia. [ADR-036](ADR-036-pantallas-clienta-mis-citas.md) no recoge esa parte: se limita al aterrizaje, a Contacto y al nombre «Mi cuenta».

## Alternativas descartadas

- Una pantalla de usuarios que mezcle clientas y personal. Obliga a tratar igual dos fichas con reglas distintas (rol, invitación, bloqueo).
- Mantener `/usuarios` como alias de clientes. El nombre promete una gestión que el producto no tiene.

## Consecuencias

El menú de administración lista Clientes y Empleados, cada uno con su ruta. Quien busque una administración de cuentas de acceso no la encontrará en el piloto.

## Tareas relacionadas

H-43.
