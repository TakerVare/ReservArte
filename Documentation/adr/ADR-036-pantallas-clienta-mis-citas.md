# ADR-036: Pantallas de la clienta y aterrizaje en Mis citas

**Fecha:** 2026-10-01

**Estado:** aceptada

## Contexto

La navegación de la SPA es plana: un `BottomNav` con tres destinos y la gestión desde la pantalla de Usuario. Los diseños de Figma de la clienta (Mis citas, Contacto y Usuario) ya estaban dibujados y no encajaban con un panel de métricas como inicio.

## Decisión

Tras iniciar sesión, todo el mundo aterriza en `/mis-citas` (H-42). «Inicio» del `BottomNav` lleva ahí cuando hay sesión. Contacto (`/contacto`) muestra el mapa de Google con la dirección del centro. El destino «Cuenta» del `BottomNav` pasa a llamarse «Mi cuenta» y sigue abriendo `/cuenta`.

Las pantallas y sus nodos de Figma están en [Análisis de pantallas y estructura.md](../Análisis%20de%20pantallas%20y%20estructura.md) §2. La navegación, en el volumen 2 §9.2.4.

## Alternativas descartadas

- Un inicio distinto para el personal desde el primer día. El inicio propio del personal queda para más adelante; de momento todos aterrizan en Mis citas.
- Contacto sin mapa, solo con texto. El diseño de Figma incluye el mapa.

## Consecuencias

Los datos del centro (dirección, horario, teléfono) van provisionales en la SPA hasta que salgan de la base de datos. El mapa de Google exige contemplar sus cookies en los textos legales (volumen 1 §6.3 aún no las lista).

## Tareas relacionadas

H-42.
