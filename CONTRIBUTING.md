# Contribuir a System Monitor Desktop

Gracias por tu interes en contribuir. Cualquier mejora — desde una correccion
de tipo a una funcionalidad nueva — es bienvenida.

## Como reportar un bug

1. Abre un [issue](https://github.com/Nakly1/SystemMonitorDesktop/issues/new/choose)
   con la plantilla de bug.
2. Incluye:
   - Version de Windows (10 / 11) y arquitectura.
   - Version de la app (la encontraras en la barra de titulo).
   - Pasos exactos para reproducirlo.
   - Captura de pantalla si aplica.

## Como proponer una funcionalidad

Abre un issue con la plantilla de feature request. Explica el caso de uso
antes de la solucion: que problema resuelve, no como lo implementarias.

## Pull Requests

1. Haz fork del repo y crea una rama desde `main`:
   ```bash
   git checkout -b feat/mi-cambio
   ```
2. Haz commits pequenos y con mensaje claro. Convencion sugerida:
   - `feat:` nueva funcionalidad
   - `fix:` correccion de bug
   - `docs:` cambios solo en documentacion
   - `refactor:` reorganizacion sin cambio funcional
   - `chore:` tareas de mantenimiento (deps, configuracion, etc.)
3. Verifica que compila: `dotnet build -c Release`.
4. Abre el PR contra `main` describiendo el cambio y por que.

## Estilo de codigo

- C# 12 / .NET 8.
- Identacion: 4 espacios.
- Nombres en ingles para codigo (variables, metodos, clases). Comentarios y
  textos de UI en espanol como el resto del proyecto.
- Evita anadir dependencias nuevas salvo que sean necesarias.

## Compilar localmente

```bash
dotnet restore
dotnet build -c Release
dotnet publish -c Release -r win-x64 --self-contained false -o AppRelease
```

El ejecutable queda en `AppRelease/SystemMonitorDesktop.exe`.

## Licencia

Al contribuir aceptas que tu codigo se distribuye bajo la licencia MIT del
proyecto.
