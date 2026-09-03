# Apoyo Docentes - Windows App

Aplicación de escritorio para Windows (WPF, .NET 8) orientada a la gestión de cargas horarias docentes, directorio de contactos, envío de correos y consulta de horarios.

## Funcionalidades principales

- Carga de archivos Excel con información de cargas horarias.
- Generación de cargas por docente.
- Directorio local de contactos mediante SQLite.
- Envío de correos mediante Gmail API.
- Lectura de horarios desde Google Sheets.
- Selección de hojas de cálculo desde la aplicación.
- Tema claro/oscuro.
- Autenticación de Google mediante OAuth 2.0.
- Persistencia local de la autorización de Google mediante almacenamiento protegido por DPAPI de Windows.
- Registro local de errores para diagnóstico.
- Verificación de actualizaciones de la aplicación.

## Instalación para usuarios finales

### Requisitos

- Windows 10 u 11 de 64 bits.
- Una cuenta de Google si deseas utilizar Gmail o Google Sheets.
- Acceso a Internet durante la vinculación inicial de Google.

### Instalación

1. Descarga el archivo `.zip` correspondiente desde **Releases**.
2. Descomprime el archivo en una carpeta de tu elección.
3. Ejecuta `ApoyoDocentes.exe`.
4. No necesitas instalar .NET por separado si utilizas el paquete publicado por el proyecto.

> **Importante:** los usuarios finales **no necesitan descargar, crear ni copiar `client_secret.json`**. La aplicación publicada contiene la configuración necesaria para iniciar el flujo OAuth.
>
> Tampoco necesitas crear un proyecto de Google Cloud para utilizar la versión publicada.

## Vincular una cuenta de Google

La aplicación utiliza **OAuth 2.0**. Cada usuario autoriza su propia cuenta de Google desde el navegador.

La aplicación no solicita la contraseña de Google ni necesita que la introduzcas dentro de Apoyo Docentes.

Para vincular la cuenta:

1. Abre **Configuración** dentro de Apoyo Docentes.
2. Pulsa **Agregar cuenta Gmail** o la opción equivalente para vincular Google.
3. Se abrirá el navegador con la página oficial de Google.
4. Inicia sesión con la cuenta que deseas utilizar.
5. Revisa los permisos solicitados y confirma la autorización.
6. Regresa a Apoyo Docentes. La cuenta deberá aparecer como vinculada.

La autorización normalmente solo necesita realizarse una vez. Puede ser necesario autorizar nuevamente si revocas el acceso, eliminas la autorización local o Google solicita una nueva autorización.

### Permisos utilizados

| Función | Permiso |
| --- | --- |
| Enviar correos desde Gmail | `gmail.send` |
| Leer hojas de cálculo de Google | `spreadsheets.readonly` |

La aplicación no solicita acceso completo a Gmail ni acceso general a Google Drive.

> Aunque una hoja de cálculo pueda estar almacenada en Google Drive, la aplicación utiliza Google Sheets API para acceder a la hoja seleccionada.

Para conocer con mayor detalle cómo se manejan los datos y la autorización de Google, consulta la [Política de Privacidad](privacy.md).

## Cargar cargas horarias desde Excel

La aplicación permite cargar un archivo Excel que contenga la información general de las cargas horarias.

El archivo se procesa localmente para generar la información necesaria para las vistas de cargas y envío.

Después de cargar el archivo:

1. Selecciona el archivo Excel.
2. Espera a que termine el procesamiento.
3. Revisa la información cargada.
4. Selecciona la información correspondiente al período que deseas utilizar.
5. Continúa con las funciones disponibles en la aplicación.

## Directorio de contactos

Apoyo Docentes incluye un directorio local de contactos.

Los contactos pueden incluir:

- Nombre del docente.
- Correo electrónico.
- Teléfono.
- Estado del contacto.

El directorio permite agregar, modificar, seleccionar y eliminar contactos.

La aplicación también registra la fecha de última actualización de cada contacto.

Los cambios realizados en el directorio se reflejan en la información utilizada por la vista de envío.

## Enviar correos desde Gmail

1. Vincula tu cuenta de Google desde **Configuración**.
2. Carga la información de las cargas horarias.
3. Abre la vista de envío.
4. Confirma que la cuenta aparece como vinculada.
5. Selecciona los docentes destinatarios.
6. Revisa las direcciones de correo y la información mostrada.
7. Envía los correos.

Los mensajes se envían utilizando la API oficial de Gmail desde la cuenta de Google autorizada.

Durante el envío, la aplicación muestra el estado de cada destinatario y puede informar errores específicos, por ejemplo:

- Correo no válido.
- Credenciales de Google no válidas.
- Sin permisos para enviar correos.
- Límite de envíos de Google alcanzado.
- Error de conexión con Google.
- Operación cancelada o con tiempo de espera excedido.

## Leer horarios desde Google Sheets

1. Abre en Google Sheets la hoja que contiene los horarios.
2. Comprueba que la cuenta de Google vinculada en Apoyo Docentes tenga permiso de **Lector** sobre esa hoja.
3. Copia el ID de la hoja desde su URL.

En una URL con la estructura:

`docs.google.com/spreadsheets/d/ID_DE_LA_HOJA/edit`

el ID es el texto situado entre `/d/` y `/edit`.

4. En la vista de horarios selecciona **Google Sheets** como origen.
5. Introduce el **ID Sheet**.
6. Pulsa **Cargar hojas**.
7. Selecciona la pestaña que deseas utilizar.

Si una hoja no aparece, comprueba que el ID sea correcto y que la cuenta vinculada tenga acceso a ella.

> El permiso OAuth de Google Sheets y el permiso de uso compartido de la hoja son cosas diferentes. La cuenta debe tener acceso a la hoja además de haber autorizado la aplicación.

## Solución de problemas

### El navegador no se abre al vincular Google

- Comprueba que tienes un navegador predeterminado configurado en Windows.
- Comprueba que tienes conexión a Internet.
- Cierra Apoyo Docentes y vuelve a intentar la vinculación desde **Configuración**.
- Si el problema persiste, abre el registro de errores desde **Configuración**.

### Google indica que la aplicación no está disponible para mi cuenta

La disponibilidad depende de la configuración y del estado de publicación/verificación del proyecto OAuth en Google.

Si la aplicación se encuentra temporalmente en modo de pruebas, solamente las cuentas registradas como usuarios de prueba podrán autorizarla.

Esto puede ser una limitación de la configuración del proyecto OAuth y no necesariamente un problema con la cuenta de Google.

### La cuenta aparece vinculada pero no puedo enviar correos

Comprueba:

- Que la cuenta correcta esté vinculada.
- Que la autorización de Gmail haya sido aceptada.
- Que la cuenta no haya revocado el acceso de la aplicación.
- Que la dirección del destinatario sea válida.

Si el problema continúa, puedes revisar el mensaje mostrado en la columna **Estado** y consultar el registro de errores desde **Configuración**.

### Google Sheets no carga una hoja

Comprueba:

- Que el ID de la hoja sea correcto.
- Que la hoja sea accesible con la cuenta vinculada.
- Que la cuenta tenga como mínimo permiso de **Lector**.
- Que la pestaña seleccionada exista.

## Privacidad

Apoyo Docentes utiliza las APIs oficiales de Google únicamente para las funciones que requieren integración con Gmail y Google Sheets.

La aplicación utiliza OAuth 2.0 y almacena localmente la autorización necesaria para las funciones de Google.

Para consultar la información completa sobre datos, permisos, almacenamiento local, seguridad y revocación de acceso:

**[Ver Política de Privacidad](privacy.md)**

## Desarrollo

### Requisitos para compilar desde el código fuente

- Windows 10 u 11 de 64 bits.
- .NET 8 SDK.
- Visual Studio o VS Code.
- Credenciales OAuth de un cliente de tipo **Desktop app** configuradas para el proyecto de desarrollo.

Los secretos de desarrollo no deben guardarse directamente en el código fuente.

Crea un archivo local llamado `GoogleOAuth.local.props` en la raíz del proyecto. Este archivo está excluido de Git mediante `.gitignore`.

Ejemplo:

```xml
<Project>
  <PropertyGroup>
    <GoogleClientId>TU_CLIENT_ID</GoogleClientId>
    <GoogleClientSecret>TU_CLIENT_SECRET</GoogleClientSecret>
  </PropertyGroup>
</Project>
