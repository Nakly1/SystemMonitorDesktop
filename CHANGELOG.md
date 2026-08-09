# Changelog

Todos los cambios notables de este proyecto se documentan aqui.

El formato sigue [Keep a Changelog](https://keepachangelog.com/es-ES/1.1.0/) y
el versionado sigue [SemVer](https://semver.org/lang/es/).

## [Unreleased]

## [2.0.0] - 2026-08-09

### Added
- **Evidencia de hardware**: actas con el numero de serie de procesador, cada
  modulo de RAM, GPUs, discos, placa base, BIOS y MAC de cada adaptador.
  Se guardan como `.smev.json` verificable y `.txt` imprimible en
  `Documentos\System Monitor\Evidencias`.
- Huella SHA-256 en cada acta: detecta si el archivo fue editado despues de
  generarse.
- Verificacion automatica contra un acta guardada, con veredicto pieza a pieza
  (intacta / cambiada / falta / nueva) y exportacion del informe.
- Detalle por modulo fisico de RAM: fabricante, numero de parte, numero de
  serie, ranura, banco, capacidad, tipo, velocidad nominal y configurada,
  formato y voltaje.
- Traduccion de codigos JEDEC de fabricante de RAM a la marca comercial.
- Inventario de unidades fisicas con serial, interfaz y firmware.
- Placa base, BIOS y UUID del equipo en el resumen.
- Navegacion lateral con seis secciones y vistas independientes.
- Grafico historico tambien para CPU.

### Changed
- Rediseno completo: paleta negra, morada y blanca; tipografia Segoe UI
  Variable Display/Text con cifras tabulares; iconografia propia; chrome de
  ventana personalizado con esquinas redondeadas de Windows 11.
- El panel de memoria pasa de una tarjeta de tres datos a una seccion propia
  con uso en vivo e inventario de modulos.
- Un unico `MonitorService` muestrea el sistema y reparte la lectura por
  evento, en lugar de un temporizador acoplado a la ventana.
- La lectura de hardware se reparte en `HardwareModels`, `HardwareService`,
  `JedecVendors`, `EvidenceService` y `SystemReport`.
- Manifiesto con reconocimiento de PPP por monitor (PerMonitorV2).

### Added (infraestructura)
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

[Unreleased]: https://github.com/Nakly1/SystemMonitorDesktop/compare/v2.0.0...HEAD
[2.0.0]: https://github.com/Nakly1/SystemMonitorDesktop/compare/v1.0.0...v2.0.0
[1.0.0]: https://github.com/Nakly1/SystemMonitorDesktop/releases/tag/v1.0.0
