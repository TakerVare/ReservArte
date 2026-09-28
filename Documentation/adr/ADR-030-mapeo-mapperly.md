# ADR-030: Mapeo entidad a DTO con Mapperly

**Fecha:** 2026-09-28

**Estado:** aceptada

## Contexto

D-10 saca AutoMapper por su licencia comercial. El mapeo a mano no avisa en compilación si un DTO gana un campo y el mapeo no. AutoMapper tampoco: un perfil incompleto se veía al arrancar, no al compilar.

## Decisión

El mapeo entidad → DTO lo hace Mapperly. Los mappers son estáticos, sin inyección, en `ReservArte-Application/Mapping/`. RMG012 (propiedad del DTO sin origen) y RMG020 (propiedad de la entidad sin destino) son errores de compilación. Lo que no se expone se ignora de forma explícita. Los tests de mapeo son `MappingCharacterizationTests`.

## Alternativas descartadas

- Mapeo a mano.
- Comprar la licencia de AutoMapper.

## Consecuencias

No hay `IMapper` ni perfiles. Un campo nuevo sin origen o sin destino rompe el build. Este ADR no sustituye al ADR-010.

## Tareas relacionadas

H-36. Tarea `869f6r7vw`. Relacionado con ADR-010; no lo sustituye.
