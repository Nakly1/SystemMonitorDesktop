<div align="center">

# System Monitor Desktop

Monitor de hardware para **Windows 10 / 11**. Interfaz oscura, rápida y sin instalador.
Muestra en tiempo real RAM, CPU, GPU, red, batería, discos y procesos — y levanta **actas de
hardware** con el número de serie de cada pieza, para saber si te cambiaron algo.

**v2.0 — Rediseño completo y actas de hardware**

[![Descargar](https://img.shields.io/badge/⬇%20Descargar-v2.0-7C3AED?style=for-the-badge)](AppRelease/SystemMonitorDesktop.exe)
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

## Características

La app se organiza en seis secciones con barra lateral.

### Resumen
Memoria y procesador con su cifra grande, barra y gráfico de los últimos 2 minutos; gráficos,
red, energía y la ficha de identidad del equipo (fabricante, modelo, placa base, BIOS).

### Memoria
- Uso en vivo con historial
- **Cada módulo físico por separado**: fabricante, **número de parte**, **número de serie**,
  ranura, banco, capacidad, tipo (DDR3 / DDR4 / DDR5 / LPDDR5), velocidad nominal y real,
  formato (DIMM / SODIMM) y voltaje
- Ranuras usadas y libres, para saber si se puede ampliar
- Si la BIOS sólo publica el código JEDEC del fabricante (`802C`, `80CE`…), se traduce a la
  marca real (Micron, Samsung, SK hynix…)

### Almacenamiento
Volúmenes con espacio ocupado, y las **unidades físicas** reales con su número de serie,
interfaz y firmware.

### Procesos
Los que más memoria consumen, con peso relativo y botón para finalizarlos.

### Evidencia de hardware
La razón principal de la v2. Antes de dejar el equipo en un servicio técnico:

1. **Generar acta** — se guardan dos archivos: un `.smev.json` para verificar automáticamente y
   un `.txt` imprimible con espacio para firmar en la entrega. El acta registra el número de
   serie de procesador, cada módulo de RAM, tarjetas gráficas, discos, placa base, BIOS y MAC
   de cada adaptador de red.
2. Cada acta lleva una **huella SHA-256**. Si alguien edita el archivo después, la huella deja
   de cuadrar y la app lo avisa.
3. **Verificar el equipo** — al recogerlo, se carga el acta y la app compara pieza por pieza:
   marca lo que sigue igual, lo que **cambió** (misma ranura, otro serial), lo que **falta** y
   lo que **apareció**. El resultado se puede exportar a `.txt`.

Las actas se guardan en `Documentos\System Monitor\Evidencias`.

### Herramientas
- **Limpiar temporales** — borra archivos de `%TEMP%` y `C:\Windows\Temp` con más de 1 h
- **Compactar memoria** — recolector de basura de .NET del propio proceso
- **Exportar informe** — un `.txt` legible con todo el estado del sistema

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

## Diseño

Sistema visual propio, en negro, morado profundo y blanco cálido:

- Superficies en negro con matiz violeta (`#0A0810` … `#1A1526`) en lugar de negro puro, que
  produce halación y cansa la vista en pantallas OLED
- Un único acento saturado (violeta `#8B5CF6`) sobre una base desaturada
- Tipografía **Segoe UI Variable Display / Text**, el equivalente más cercano a SF Pro en
  Windows: titulares en *Display*, cuerpo en *Text*, cifras tabulares en toda métrica en vivo
  para que los dígitos no bailen entre refrescos
- Sentence case en todos los títulos, tres pesos como máximo, iconografía dibujada sobre una
  retícula propia de 24 px
- Chrome de ventana personalizado con esquinas redondeadas nativas de Windows 11

---

## Cómo funciona por dentro

- **WPF** sobre .NET 8 (XAML + code-behind, sin MVVM pesado)
- **WMI** (`System.Management`) para CPU, RAM por módulo, GPU, discos, placa base, BIOS y SO
- **Registro de Windows** para detectar VRAM correctamente en GPUs de más de 4 GB
- **PerformanceCounter** para el uso de CPU
- **NetworkInterface** para contadores de red y direcciones MAC
- **P/Invoke `GetSystemPowerStatus`** para la batería
- **`System.Text.Json` + SHA-256** para las actas de hardware
- Un solo `MonitorService` con un `DispatcherTimer` a 2 s: muestrea fuera del hilo de interfaz
  y reparte la lectura por evento a las vistas abiertas

---

## Estructura del proyecto

```
SystemMonitorDesktop/
├── App.xaml                       ← combina los diccionarios de tema
├── MainWindow.xaml(.cs)           ← shell: chrome, barra lateral, navegación
├── Theme/
│   ├── Palette.xaml               ← colores y degradados
│   ├── Typography.xaml            ← escala tipográfica
│   └── Controls.xaml              ← tarjetas, botones, medidores, iconos
├── Views/
│   ├── OverviewView               ← Resumen
│   ├── MemoryView                 ← Memoria y módulos físicos
│   ├── StorageView                ← Volúmenes y unidades
│   ├── ProcessesView              ← Procesos
│   ├── EvidenceView               ← Actas de hardware
│   └── ToolsView                  ← Mantenimiento
├── Controls/
│   ├── Sparkline.cs               ← gráfico compacto por OnRender
│   └── UiKit.cs                   ← fichas técnicas construidas en código
├── Services/
│   ├── HardwareModels.cs          ← registros de datos
│   ├── HardwareService.cs         ← todas las consultas WMI
│   ├── JedecVendors.cs            ← códigos JEDEC → marca de RAM
│   ├── EvidenceService.cs         ← capturar, firmar y comparar actas
│   ├── SystemReport.cs            ← informe legible del sistema
│   ├── MonitorService.cs          ← muestreo periódico
│   └── AppServices.cs             ← servicios compartidos
└── AppRelease/                    ← build publicado listo para usar
    └── SystemMonitorDesktop.exe
```

---

## Roadmap

- [ ] Uso de CPU por núcleo individual
- [ ] Temperaturas de CPU / GPU (vía LibreHardwareMonitor)
- [ ] Gráfico histórico también para red
- [ ] Bandeja de sistema (minimizar al tray)
- [ ] Tema claro / modo auto
- [ ] Alertas configurables (ej. avisar si RAM > 90 % durante 30 s)
- [ ] Firma digital del acta con certificado del usuario
- [ ] Localización a inglés

Los PRs con mejoras son bienvenidos.

---

## Preguntas frecuentes

**¿Necesita permisos de administrador?**
No para el uso normal. Sólo al limpiar `C:\Windows\Temp` algunos archivos bloqueados no se podrán
borrar sin ejecutarla como admin — pero la app funciona igual.

**¿El acta de hardware sirve como prueba legal?**
No es un documento con validez jurídica por sí solo. Es una constancia técnica fechada y firmada
con SHA-256 de lo que había en el equipo en ese momento: sirve para reclamar con datos concretos
(«este módulo tenía el serial X») y para detectar el cambiazo. Imprime el `.txt`, fírmalo con
quien recibe el equipo y guarda el `.smev.json` para la verificación automática.

**¿Qué pasa si una pieza no tiene número de serie?**
Algunas BIOS no lo publican. En ese caso la app lo dice explícitamente y compara esa pieza por
ranura y modelo, que es menos concluyente pero sigue detectando sustituciones.

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
