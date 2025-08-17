# FAQ

**Table of contents**
- [How does Google Calendar sync work?](#how-does-google-calendar-sync-work)
- [Can we limit scheduling hours or days?](#can-we-limit-scheduling-hours-or-days)
- [How are conflicts detected?](#how-are-conflicts-detected)
- [What roles does the system have?](#what-roles-does-the-system-have)
- [Is there audit logging?](#is-there-audit-logging)
- [How do students see their timetable?](#how-do-students-see-their-timetable)
- [What happens if Google API is down?](#what-happens-if-google-api-is-down)
- [Security & privacy?](#security--privacy)

---

### How does Google Calendar sync work?
eRaspored pushes class and exam events to dedicated calendars (per program and study year).  
Updates and deletions are propagated. Failures are retried via an outbox job until delivery succeeds.

### Can we limit scheduling hours or days?
Yes. Admins can configure business hours (e.g., 08:00–21:00) and allowed days per location or program.

### How are conflicts detected?
We check room occupancy and lecturer availability across both classes and exams.  
Conflicts are highlighted with actionable suggestions (change room, time, or duration).

### What roles does the system have?
Typical RBAC: **Admin**, **Scheduler**, **Lecturer**, **Student**.  
Permissions are granular (edit, publish, export, integrate).

### Is there audit logging?
Yes. Every change (create / update / delete) is recorded with who / when / what, and a diff preview is available.

### How do students see their timetable?
Each student gets a filtered view by year / group and can subscribe to a read-only iCal / Google feed.

### What happens if Google API is down?
Events are queued to the outbox and retried. The local schedule is always the source of truth.

### Security & privacy?
Role-based access, short-lived tokens, audit logs, and least-privilege service accounts for calendar sync.  
Backups and data-retention policies are configurable.
