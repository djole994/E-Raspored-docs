# Security 

## Access & Authentication
- RBAC roles: Admin / Organizer / Professor / Student
- Sensitive actions restricted (create/edit/delete)

## Google Calendar Access
- Limited to an admin Google account with 2FA enabled
- API credentials stored outside source control (e.g., environment variables)

## Validation & Integrity
- Client- and server-side validation + database constraints
- Conflict detection for time/room overlaps

## Transport & Configuration
- HTTPS enabled in production
- Sensitive config (e.g., connection strings) kept out of the repo

## Backup & Recovery
- Regular database backups (per institution policy)
- Periodic restore tests to validate integrity
