# Changelog

Todos los cambios notables de este proyecto se documentan aqui.

El formato sigue [Keep a Changelog](https://keepachangelog.com/es-ES/1.1.0/) y
el versionado sigue [SemVer](https://semver.org/lang/es/).

## [Unreleased]

### Added
- Estructura inicial de archivos de la comunidad: `LICENSE`, `CONTRIBUTING.md`,
  `CHANGELOG.md`, plantillas de issues y PR.

## [1.0.0] - 2026-04-18

### Added
- Primera version publica.
- Monitor en tiempo real de RAM, CPU, GPU, red, bateria, discos y procesos.
- Mini grafico historico de RAM (60 s).
- Top 10 procesos por uso de memoria con boton para finalizarlos.
- Acciones: limpiar archivos temporales, forzar Garbage Collection y exportar
  informe a `.txt`.
- Lectura de hardware via WMI, registro de Windows y P/Invoke.
- Build self-contained-false para .NET 8 Desktop Runtime.

[Unreleased]: https://github.com/Nakly1/SystemMonitorDesktop/compare/v1.0.0...HEAD
[1.0.0]: https://github.com/Nakly1/SystemMonitorDesktop/releases/tag/v1.0.0
