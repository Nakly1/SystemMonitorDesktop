<div align="center">

# System Monitor Desktop

Monitor de hardware para **Windows 10 / 11**. Interfaz oscura, rapida y sin instalador.
Muestra en tiempo real RAM, CPU, GPU, red, bateria, discos y procesos — todo en una ventana.

**v1.0 — Primera version publica**

[![Descargar](https://img.shields.io/badge/⬇%20Descargar-v1.0-238636?style=for-the-badge)](AppRelease/SystemMonitorDesktop.exe)
[![.NET 8](https://img.shields.io/badge/.NET-8.0-512BD4?style=for-the-badge&logo=dotnet)](https://dotnet.microsoft.com/es-es/download/dotnet/8.0)
[![Windows](https://img.shields.io/badge/Windows-10%20%2F%2011-0078D4?style=for-the-badge&logo=windows)](#)
[![License](https://img.shields.io/badge/License-MIT-blue?style=for-the-badge)](#licencia)
[![Build](https://img.shields.io/github/actions/workflow/status/Nakly1/SystemMonitorDesktop/build.yml?branch=main&style=for-the-badge&logo=github)](https://github.com/Nakly1/SystemMonitorDesktop/actions/workflows/build.yml)

</div>

---

## Que es

**System Monitor Desktop** es una app de escritorio hecha en C# + WPF que te muestra el estado
de tu PC en tiempo real. No necesita instalacion, no se queda en segundo plano, no envia datos
a ningun sitio — solo lee lo que Windows ya sabe de tu propio hardware y lo presenta de forma
clara.

Util para diagnosticar lentitud, ver cuanta RAM consume un programa o tener a mano un informe
de tu equipo.

---

## Caracteristicas

### En tiempo real (refresco cada 2 s)
- **RAM** — uso total, disponible, porcentaje y **mini grafico historico** de los ultimos 60 s
- **CPU** — uso global en % con barra de progreso
- **Red** — velocidad de bajada y subida en Kbps / Mbps
- **Bateria** — porcentaje y estado (cargando / descargando), se oculta en equipos de sobremesa
- **Uptime** — tiempo que lleva encendido tu equipo
- **Top 10 procesos** por uso de RAM, con **boton para finalizar** cualquiera de ellos

### Informacion del sistema
- **CPU** — modelo, nucleos, hilos y velocidad maxima
- **GPU** — modelo y VRAM (con fallback al registro para GPUs de mas de 4 GB)
- **RAM** — tipo (DDR3 / DDR4 / DDR5), velocidad en MHz y modulos instalados
- **Sistema operativo** — nombre, build, arquitectura y nombre del equipo
- **Discos** — espacio usado y libre de cada unidad local

### Acciones utiles
- **Limpiar temporales** — borra archivos de `%TEMP%` y `C:\Windows\Temp` con mas de 1 h
- **Forzar Garbage Collection** — libera memoria del propio proceso .NET
- **Exportar informe** — genera un `.txt` con todo el estado del sistema

---

## Descargar y usar

### Opcion A — Descarga directa (recomendado)
1. Descarga el repositorio como ZIP desde el boton verde **`<> Code`** → **Download ZIP**
2. Extrae el ZIP donde quieras (Escritorio, por ejemplo)
3. Entra a la carpeta **`AppRelease/`**
4. Doble clic en **`SystemMonitorDesktop.exe`**

No necesita instalacion, no modifica el registro, no crea accesos directos.

> **Requisito unico:** [.NET 8 Desktop Runtime](https://dotnet.microsoft.com/es-es/download/dotnet/8.0/runtime?cid=getdotnetcore&os=windows&arch=x64) (gratis, ~55 MB).
> Si al abrir la app te sale un mensaje de "no se encuentra .NET", instala el runtime y vuelve a probar.

### Opcion B — Compilar desde el codigo fuente
Necesitas el [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0):

```bash
git clone https://github.com/Nakly1/SystemMonitorDesktop.git
cd SystemMonitorDesktop
dotnet publish -c Release -r win-x64 --self-contained false -o AppRelease
```

El ejecutable queda en `AppRelease/SystemMonitorDesktop.exe`.

---

## Capturas

La app tiene una unica ventana organizada en tarjetas:

```
+-----------------------------------------------------------+
|  SM  System Monitor  [v1.0]      Uptime 2d 04h | HH:MM:SS |
+-----------------------------------------------------------+
|                                                           |
|   MEMORIA RAM — TIEMPO REAL                               |
|   12.4 GB / 16.0 GB                     77.5%             |
|   [======================================----]   /-\-/‾\ |
|                                                           |
|   [ CPU  42.1% ]                     [ GPU  NVIDIA ... ] |
|                                                           |
|   [ RAM specs ]   [ OS ]   [ RED ]   [ BATERIA 87% ]     |
|                                                           |
|   [ ALMACENAMIENTO ]                                      |
|   [ TOP 10 PROCESOS ]            [finalizar]              |
|   [ ACCIONES: limpiar / GC / exportar ]                   |
+-----------------------------------------------------------+
```

---

## Como funciona por dentro

- **WPF** para la interfaz (XAML + code-behind, sin MVVM pesado)
- **WMI** (`System.Management`) para leer CPU, RAM, GPU, discos y SO
- **Registro de Windows** para detectar VRAM correctamente en GPUs modernas
- **PerformanceCounter** para el uso de CPU en tiempo real
- **NetworkInterface** para contadores de red
- **P/Invoke `GetSystemPowerStatus`** para la bateria (sin depender de WinForms)
- **`DispatcherTimer`** a 2 s para refrescar la UI sin trabarla

Todo corre sobre **.NET 8** en un unico proyecto — simple y facil de modificar.

---

## Estructura del proyecto

```
SystemMonitorDesktop/
├── App.xaml / App.xaml.cs         ← entry point de WPF
├── MainWindow.xaml                ← UI (tarjetas, estilos, layout)
├── MainWindow.xaml.cs             ← logica de UI y timer
├── Services/
│   └── HardwareService.cs         ← toda la lectura de hardware
├── app.manifest                   ← manifiesto de ejecucion
├── SystemMonitorDesktop.csproj    ← configuracion del proyecto
└── AppRelease/                    ← build publicado listo para usar
    └── SystemMonitorDesktop.exe
```

---

## Roadmap

Ideas para futuras versiones (v1.x / v2):

- [ ] Uso de CPU por nucleo individual
- [ ] Temperaturas de CPU / GPU (via LibreHardwareMonitor)
- [ ] Grafico historico tambien para CPU y red
- [ ] Bandeja de sistema (minimizar al tray)
- [ ] Tema claro / modo auto
- [ ] Alertas configurables (ej. avisar si RAM > 90% durante 30 s)
- [ ] Localizacion a ingles

Los PRs con mejoras son bienvenidos.

---

## Preguntas frecuentes

**¿Necesita permisos de administrador?**
No para el uso normal. Solo al limpiar `C:\Windows\Temp`, algunos archivos bloqueados no se podran
borrar sin ejecutarla como admin — pero la app funciona igual.

**¿Funciona en Linux / macOS?**
No. Usa WMI y el registro de Windows, asi que es solo para Windows 10 / 11.

**¿Envia datos a internet?**
No. La app lee solo de tu propia maquina y no hace ninguna conexion saliente.



---

## Licencia

**MIT** — haz con este codigo lo que quieras, solo no me culpes si algo sale mal :)

---

<div align="center">

Si te es util, deja una ⭐ en el repo. Reportes de bugs o sugerencias en **Issues**.

</div>
