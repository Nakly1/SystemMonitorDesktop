# Politica de seguridad

## Versiones soportadas

Solo la ultima version mayor recibe correcciones de seguridad.

| Version | Soportada |
| ------- | --------- |
| 1.0.x   | Si        |
| < 1.0   | No        |

## Reportar una vulnerabilidad

Si encuentras un fallo de seguridad **no abras un issue publico**. En su lugar:

1. Usa el formulario privado de
   [GitHub Security Advisories](https://github.com/Nakly1/SystemMonitorDesktop/security/advisories/new).
2. Incluye:
   - Descripcion del problema.
   - Pasos para reproducirlo.
   - Impacto estimado (que puede leer / modificar / ejecutar un atacante).
   - Version afectada.

Intentaremos responder en un plazo de **7 dias** y publicar un parche o
mitigacion en cuanto sea viable.

## Alcance

Esta app se ejecuta localmente y no expone servicios de red. Los vectores
relevantes son:

- Lectura de archivos / registro fuera del ambito esperado.
- Ejecucion de comandos por entradas externas.
- Escalada de privilegios al ejecutar la accion de "limpiar temporales".
- Inyeccion via WMI / consultas dinamicas.

Quedan fuera del alcance los problemas que requieran ya tener acceso
administrativo a la maquina o modificar binarios firmados localmente.
