# Apoyo Docentes - Windows App

Aplicacion de escritorio (WPF, .NET 8) para automatizar cargas horarias docentes, directorio de contactos, envio de correos y consulta de horarios.

## Funcionalidades principales

- Carga de Excel con tabla general.
- Generacion de cargas por docente.
- Directorio de contactos local (SQLite).
- Envio de correos mediante Gmail API.
- Lectura de horarios desde Google Sheets.
- Tema claro/oscuro.

## Instalacion y ejecucion

Requisitos: Windows 10 u 11.

1. Descarga el paquete de *releases*, por ejemplo `ApoyoDocentes-v1.0.0-win-x64.zip`.
2. Descomprime el archivo.
3. Ejecuta `ApoyoDocentes.exe`.
4. Completa la configuracion de Google descrita a continuacion si usaras correo o Google Sheets.

## Vincular Google: guia paso a paso

La aplicacion usa OAuth 2.0. Cada persona autoriza su propia cuenta de Google: la aplicacion **no solicita ni almacena la contrasena** de Gmail.

Al usarla se solicitan exclusivamente estos permisos:

| Funcion | Permiso solicitado |
| --- | --- |
| Enviar correos | `https://www.googleapis.com/auth/gmail.send` |
| Leer hojas de calculo | `https://www.googleapis.com/auth/spreadsheets.readonly` |

> Importante: esta version lee contenido de **Google Sheets**. Aunque la hoja este guardada en Google Drive, no descarga ni explora archivos arbitrarios de Drive y no requiere habilitar Google Drive API.

### 1. Crear o seleccionar un proyecto de Google Cloud

1. Ingresa a [Google Cloud Console](https://console.cloud.google.com/).
2. En el selector de proyectos, crea un proyecto o selecciona el proyecto institucional que administrara esta integracion.
3. Usa un proyecto dedicado a esta aplicacion; no reutilices uno de produccion que pertenezca a otro sistema.

### 2. Habilitar las APIs necesarias

1. En Google Cloud Console abre **APIs y servicios > Biblioteca**.
2. Busca **Gmail API**, abrela y pulsa **Habilitar**.
3. Busca **Google Sheets API**, abrela y pulsa **Habilitar**.

No habilites Google Drive API para la funcionalidad actual: no es necesaria.

### 3. Configurar la pantalla de consentimiento OAuth

1. Abre **Google Auth platform** y completa las secciones de configuracion de OAuth (Branding, Audience y Data Access).
2. Define un nombre reconocible, por ejemplo `Apoyo Docentes`, y un correo de soporte administrado por la institucion.
3. En **Audience**, selecciona **Internal** si todos los usuarios pertenecen al mismo Google Workspace institucional. Selecciona **External** si tambien se autorizaran cuentas ajenas a ese dominio.
4. En **Data Access**, agrega solamente los dos permisos indicados arriba: `gmail.send` y `spreadsheets.readonly`.
5. Si la aplicacion esta en modo de prueba, agrega las cuentas que podran autorizarla como usuarios de prueba.
6. Guarda los cambios.

### 4. Crear la credencial para aplicacion de escritorio

1. En **Google Auth platform > Clients**, selecciona **Create client**.
2. En tipo de aplicacion elige **Desktop app**.
3. Escribe un nombre, por ejemplo `Apoyo Docentes Windows`.
4. Pulsa **Create** y descarga el archivo JSON de la credencial.
5. Conserva el nombre `client_secret.json`. No lo subas a GitHub ni lo compartas en canales publicos.

### 5. Registrar la credencial en Apoyo Docentes

1. Abre la aplicacion y entra a **Configuracion**.
2. Selecciona la opcion para vincular o cargar `client_secret.json` y elige el archivo descargado.
3. La aplicacion lo copiara en `%AppData%\AppParaUniversidad\client_secret.json`.
4. Al enviar un correo o cargar una hoja de Google por primera vez, se abrira el navegador para iniciar sesion.
5. Inicia sesion con la cuenta que utilizaras y revisa los permisos solicitados.
6. Pulsa **Permitir**. Regresa a la aplicacion al terminar la autorizacion.

La autorizacion de Gmail y la de Sheets pueden mostrarse por separado la primera vez que se use cada funcion; esto es normal porque tienen permisos distintos.

### 6. Enviar correos desde Gmail

1. Vincula la cuenta siguiendo los pasos anteriores.
2. En la vista de envio, confirma que se muestra la cuenta vinculada.
3. Selecciona los docentes destinatarios y revisa la vista previa.
4. Envia los correos. Gmail los enviara desde la cuenta que autorizo el acceso.

No utilices una clave de API para esta funcion: una clave de API no sustituye la autorizacion OAuth para enviar correo desde una cuenta de Gmail.

### 7. Leer una hoja guardada en Google Drive

1. En Google Drive, abre la hoja de calculo de Google que contiene los horarios o contactos.
2. Confirma que la cuenta autorizada en la aplicacion tiene, como minimo, permiso de **Lector** sobre esa hoja.
3. Copia el ID de la hoja desde su URL. En una direccion como `https://docs.google.com/spreadsheets/d/ID_DE_LA_HOJA/edit`, copia el texto entre `/d/` y `/edit`.
4. En la vista de horarios, elige **Google Sheets** como origen.
5. Pega el ID en el campo **ID Sheet** y pulsa **Cargar hojas**.
6. Selecciona la pestana (hoja) que deseas utilizar y continua con la carga.
7. Si usaras esa hoja con frecuencia, guardala con un alias desde la misma vista.

Si la hoja no aparece o falla la carga, verifica que se trate de una hoja de calculo de Google, que el ID sea correcto y que la cuenta autorizada tenga acceso. Los permisos de Google Cloud no reemplazan los permisos de uso compartido de la hoja en Drive.

## Seguridad y datos locales

- `%AppData%\AppParaUniversidad\app.db`: directorio y datos locales de la aplicacion.
- `%AppData%\AppParaUniversidad\client_secret.json`: configuracion del cliente OAuth.
- `%AppData%\AppParaUniversidad\tokens`: autorizacion para Gmail, cifrada mediante DPAPI para el usuario actual de Windows.
- `%AppData%\AppParaUniversidad\tokens_sheets`: autorizacion para Google Sheets, cifrada mediante DPAPI para el usuario actual de Windows.

No copies estas carpetas a equipos compartidos ni las incluyas en repositorios, copias publicas o tickets de soporte. Para retirar acceso, revoca el permiso de la aplicacion desde la cuenta de Google y elimina las carpetas de tokens locales; al volver a usar la funcion se solicitara autorizacion otra vez.

Las actualizaciones automaticas solo aceptan assets de GitHub que publiquen un digest `sha256`. La aplicacion verifica ese hash tras la descarga y antes de abrir un instalador o reemplazar archivos.

## Desarrollo

1. Abre la solucion en Visual Studio o VS Code.
2. Compila en configuracion Debug o Release.
3. Ejecuta el proyecto principal.

## Referencias oficiales

- [Habilitar APIs de Google Workspace](https://developers.google.com/workspace/guides/enable-apis)
- [Crear credenciales OAuth de escritorio](https://developers.google.com/workspace/guides/create-credentials)

## Imagenes

![Pantalla principal](docs/images/pantalla-principal.png)
