# Privacy Policy - Apoyo Docentes

**Last updated:** September 2, 2026

Apoyo Docentes is a Windows desktop application designed to help manage teaching schedules, local contact directories, email delivery, and Google Sheets-based schedule data.

This policy explains what information the application accesses, how that information is handled, and where application data is stored.

## 1. Information accessed

Depending on the features you use, Apoyo Docentes may process:

- Information associated with the Google account used to authorize the application.
- Email addresses used as recipients of messages.
- Contact information entered into the local application directory.
- Teaching schedule information loaded into the application.
- Spreadsheet data from Google Sheets that the authorized user explicitly selects.
- Application settings and locally stored configuration.
- Technical information contained in local error logs.

The application requests only the following Google OAuth permissions:

- `https://www.googleapis.com/auth/gmail.send`
- `https://www.googleapis.com/auth/spreadsheets.readonly`

The application does not request full Gmail access or general Google Drive access.

## 2. Google authentication

Apoyo Docentes uses Google's OAuth 2.0 authorization flow.

The application does **not** ask users to provide their Google password.

Authentication is performed through Google's authorization page in the user's web browser. Google determines which account is authorized and which permissions are granted.

Users authorize their own Google account and may revoke that authorization at any time.

## 3. Gmail

If Gmail access is authorized, Apoyo Docentes uses the Gmail API to send messages explicitly initiated by the user.

The application does not require permission to:

- Read the Gmail inbox.
- Modify existing Gmail messages.
- Delete Gmail messages.
- Browse the user's Gmail mailbox.

Email addresses may be processed locally when preparing messages for delivery.

Messages are sent through Google's Gmail API and are subject to Google's own systems, policies, and terms.

## 4. Google Sheets

If the Google Sheets integration is used, Apoyo Docentes reads spreadsheet data through the Google Sheets API.

The application does not use Google Drive API to browse arbitrary files.

The user must already have access to the spreadsheet with the Google account authorized in the application.

Access to a spreadsheet therefore depends on both:

1. The OAuth permission granted to Apoyo Docentes.
2. The sharing permissions assigned to the Google account for that spreadsheet.

## 5. Local storage

Application data is stored locally on the user's Windows computer under:

`%AppData%\AppParaUniversidad\`

Depending on the features used, this directory may contain:

- `app.db` — local SQLite database containing application data such as contacts.
- `settings.json` — application settings.
- `tokens-google\` — locally stored Google OAuth authorization data.
- `app.log` — local diagnostic and error log.

The application does not require an Apoyo Docentes online account.

The application does not intentionally upload the contents of these local files to GitHub or to an Apoyo Docentes server.

## 6. OAuth token protection

OAuth authorization data is stored locally and protected using **Windows Data Protection API (DPAPI)**.

The authorization data is associated with the current Windows user account.

Users should protect their Windows account and avoid copying the application's local authorization data to shared or untrusted computers.

## 7. Error logs

Apoyo Docentes may generate a local log file to assist with troubleshooting and diagnosing application errors.

The log may contain technical information such as:

- Date and time of an error.
- Application component where the error occurred.
- Exception type.
- Error message.

The application applies sanitization to error messages to avoid directly storing certain email addresses in the log.

The log remains local to the user's computer unless the user voluntarily shares it when requesting technical support.

## 8. Data sharing

Apoyo Docentes does not intentionally sell application data or share it with third parties.

Google API requests are sent to Google's services when the user uses features that require Google integration.

Those requests are subject to Google's own privacy policies and terms.

The application does not require a separate Apoyo Docentes online account or central application server for its normal operation.

## 9. Data retention

Local application data remains on the user's computer until it is removed.

This may include:

- Contact records.
- Application settings.
- Locally stored authorization data.
- Error logs.
- Other locally stored application information.

The user can remove the locally stored Google authorization by deleting:

`%AppData%\AppParaUniversidad\tokens-google`

After removing the local authorization data, Google authorization will be required again the next time a Google feature needs access.

Removing the local application data does not delete emails that have already been sent or information stored directly in Google services.

## 10. Revoking Google access

Users can revoke Apoyo Docentes' access from their Google Account security settings.

After access is revoked, the application will no longer be able to use the previously granted Google authorization.

Users may also delete the local `tokens-google` folder to remove the stored authorization from the computer.

## 11. Security

The application uses several mechanisms intended to protect locally stored information, including:

- OAuth 2.0 instead of requesting Google passwords.
- Windows DPAPI for locally stored OAuth authorization data.
- Local storage of the application database.
- Sanitization of certain information written to error logs.
- Exclusion of development credentials from the public source repository.

No system can guarantee absolute security.

Users are responsible for protecting their Windows account, Google account, and physical access to their computer.

## 12. User control

Users can:

- Choose whether to authorize Google integration.
- Revoke Google authorization.
- Remove locally stored Google authorization.
- Remove local application data.
- Review the local error log.
- Decide whether to share diagnostic information when requesting support.

Google features cannot be used without the corresponding authorization and access required by Google.

## 13. Changes to this policy

This policy may be updated when application functionality or data handling changes.

The date at the top of this document indicates the latest revision.

## 14. Contact

For questions, bug reports, or privacy concerns, use the project's GitHub repository and issue tracker.