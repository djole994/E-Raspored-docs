Q1. How does Google Calendar sync work?
eRaspored pushes class/exam events to dedicated calendars (per program/year). Changes and deletions are propagated, and failed pushes are retried via an outbox job.

Q2. Can we limit scheduling hours or days?
Yes. Admins can configure business hours (e.g., 08:00–21:00) and allowed days per location or program.

Q3. How are conflicts detected?
We check room occupancy and lecturer availability across both classes and exams. Conflicts are highlighted with actionable suggestions (change room, time or duration).

Q4. What roles does the system have?
Typical RBAC: Admin, Scheduler, Lecturer, Student. Permissions are granular (edit, publish, export, integrate).

Q5. Is there audit logging?
Yes. Every change (create/update/delete) is recorded with who/when/what, and a diff preview is available.

Q6. How do students see their timetable?
Each student gets a filtered view by year/group and can subscribe to a read-only iCal/Google feed.

Q7. What happens if Google API is down?
Events are queued to the outbox and retried. Local schedule is always the source of truth.

Q8. Security & privacy?
We use role-based access, short-lived tokens, audit logs and least-privilege service accounts for calendar sync. Backups and data-retention policies are configurable.
