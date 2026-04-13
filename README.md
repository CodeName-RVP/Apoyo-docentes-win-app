# Apoyo Docentes - Windows App

Aplicacion de escritorio (WPF, .NET 8) para automatizar cargas horarias docentes, directorio de contactos, envio de correos y consulta de horarios.

## Usuario final
- Windows 10/11
- Descarga el paquete listo para usar: `ApoyoDocentes-v1.0.0-win-x64.zip`
- No requiere instalar .NET ni herramientas de desarrollo

Pasos:
1) Descargar el `.zip` desde GitHub.
2) Descomprimirlo.
3) Ejecutar `ApoyoDocentes.exe`.
4) Si se usara Gmail o Google Sheets, vincular el archivo `client_secret.json` desde la app.

## Desarrollo
- Windows 10/11
- .NET 8 SDK

## Como ejecutar en desarrollo
1) Abre la solucion en Visual Studio / VS Code
2) Compila en Debug o Release
3) Ejecuta el proyecto principal

## Funcionalidades principales
- Carga de Excel con tabla general
- Generacion de cargas por docente
- Directorio de contactos (SQLite)
- Envio de correos (Gmail API)
- Horarios docente desde Google Sheets
- Tema claro/oscuro

## Google APIs (Gmail + Sheets)
Estas APIs deben habilitarse manualmente en Google Cloud. No se pueden activar automaticamente desde la app.

### 1) Crear proyecto en Google Cloud
1) Ve a Google Cloud Console.
2) Crea un proyecto nuevo (o usa uno existente).

### 2) Habilitar APIs
En el proyecto:
- Habilita **Gmail API**
- Habilita **Google Sheets API**

### 3) Configurar pantalla de consentimiento (OAuth)
1) En "OAuth consent screen" selecciona tipo **External** (o Internal si es cuenta institucional).
2) Completa nombre de la app y correo de soporte.
3) Agrega los scopes:
   - Gmail: `https://www.googleapis.com/auth/gmail.send`
   - Sheets: `https://www.googleapis.com/auth/spreadsheets.readonly`
4) Agrega los usuarios de prueba (si aplica).

### 4) Crear credenciales OAuth
1) Ve a "Credentials" ? "Create Credentials" ? "OAuth client ID".
2) Tipo de aplicacion: **Desktop app**.
3) Descarga el archivo JSON (client_secret.json).

### 5) Vincular la cuenta en la app
1) Abre la app ? Configuracion ? Vincular cuenta Gmail.
2) Selecciona el archivo `client_secret.json`.
3) Se abrira el navegador para autorizar.

## Notas
- El archivo de base de datos se guarda en %AppData%\AppParaUniversidad\app.db
- Las credenciales de Google se configuran desde la app

## Imagenes
Coloca tus capturas en `docs/images/` y referencialas asi:

![Pantalla principal](docs/images/pantalla-principal.png)

## DESCARGA RAPIDA (USUARIO FINAL)
Para usuarios no tecnicos, usa una de estas rutas:
- `Releases` del repositorio
- Carpeta `DESCARGA_AQUI` del repo

Archivo recomendado:
- `ApoyoDocentes-v1.0.0-win-x64.zip`

Pasos:
1. Descargar el `.zip`.
2. Descomprimirlo.
3. Ejecutar `ApoyoDocentes.exe`.
