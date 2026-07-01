# Security

This document describes the main security concepts used in **E-Raspored**.

The system uses **ASP.NET Core Identity** for authentication and **role-based authorization** for controlling access to different parts of the application.

---

## 1. Authentication

User authentication is handled through **ASP.NET Core Identity**.

Identity is responsible for:

- user login
- password hashing
- user accounts
- role assignment
- authentication cookies / sessions
- account-level access control

No passwords are stored in plain text.

---

## 2. Authorization

The system uses **role-based authorization**.

Main roles:

- **Admin**
- **Organizer**
- **Professor**
- **Student**

Each role has access only to the parts of the system needed for that user type.

---

## 3. Permission Model

### Admin

Admins have full access to system administration.

They can:

- manage users
- manage roles and permissions
- view audit logs
- monitor Google Calendar synchronization
- access system notifications
- manage global application settings

---

### Organizer

Organizers manage schedules, but only for assigned study programs.

They can:

- create and update classes
- create and update exams
- manage rooms and time slots
- publish schedule changes
- trigger Google Calendar synchronization

An organizer cannot manage study programs that are not assigned to them.

---

### Professor

Professors have limited access to their own academic obligations.

They can:

- view their own classes
- view their own exams
- see assigned rooms and time slots

They cannot edit schedules or manage other users.

---

### Student

Students can access only student-facing features.

They can:

- view their own schedule
- view upcoming exams
- submit anonymous feedback for classes and exams

Students cannot access administrative or scheduling modules.

---

## 4. Data Validation

Validation is implemented on multiple levels:

- client-side validation for better user experience
- server-side validation for data integrity
- database consistency rules
- conflict detection before saving schedule events

Server-side validation is always treated as the final authority.

---

## 5. Schedule Protection

Before saving a class or exam, the system checks whether the event conflicts with existing data.

The system prevents:

- professor time conflicts
- room time conflicts
- study program overlaps
- invalid time ranges
- unauthorized schedule changes

This protects the database from invalid scheduling records.

---

## 6. Audit Logs

Important actions are recorded in audit logs.

Audit logs can include:

- user management changes
- schedule changes
- permission changes
- Google Calendar synchronization actions
- recovery actions
- administrative operations

Audit logs help track what happened in the system, when it happened and which user performed the action.

---

## 7. External Service Access

Google Calendar access is restricted to synchronization-related operations.

The application stores synchronization metadata such as:

- Google Calendar event ID
- synchronization status
- last sync attempt
- error message if synchronization fails

Secrets, tokens and connection strings are not included in this public repository.

---

## 8. Summary

The security model is based on:

- ASP.NET Core Identity
- role-based authorization
- study-program-level organizer permissions
- server-side validation
- audit logs
- controlled access to Google Calendar synchronization

The goal is to ensure that each user can access only the data and actions relevant to their role.