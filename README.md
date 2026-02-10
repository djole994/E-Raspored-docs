# E-Raspored — Scheduling that prevents conflicts

E-Raspored is an academic scheduling and exam management system that helps institutions plan classes and exams faster, track room/lecturer workload, and keep everyone synced via Google Calendar integration.

> **Public Showcase / Curated Subset**
>
> This repository contains a **curated public subset** of the project (selected code modules, docs, screenshots, and examples).  
> The full production codebase is **private/proprietary** and includes additional modules, internal configuration, and institution-specific details.

✅ No secrets (keys, connection strings, tokens) are included.  
✅ No real institution data is included.  
✅ Some implementation details are intentionally omitted.

---

## Who this is for & what problem it solves

E-Raspored is built for three groups:

- **Scheduling office / administrators** — build timetables and exam sessions faster, detect room conflicts and lecturer workload at a glance, and coordinate changes safely.
- **Teaching staff** — view only your own classes/exams, see room & group assignments, and get notified when something changes.
- **Students** — access a personalized timetable (year/group), upcoming exams, and submit quick feedback on courses and exam sessions.

**Goal:** reduce manual work, prevent scheduling conflicts, and make schedules transparent for everyone.

---

## Who this is for & what problem it solves

E-Raspored is built for three groups:

- **Scheduling office / administrators** — build timetables and exam sessions faster, detect room conflicts and lecturer workload at a glance, and coordinate changes safely.
- **Teaching staff** — view only your own classes/exams, see room & group assignments, and get notified when something changes.
- **Students** — access a personalized timetable (year/group), upcoming exams, and submit quick feedback on courses and exam sessions.

**Goal:** reduce manual work, prevent scheduling conflicts, and make schedules transparent for everyone.

---

## 📈 Adoption (Production Use)
- 10+ study programs
- 2,500+ students, 240+ professors
- Thousands of scheduled events per semester


## 🚀 Key Features
- Centralized scheduling for classes and exams
- Conflict detection for rooms and time slots
- Two-way synchronization with Google Calendar
- Role-based access control (Admin / Organizer / Professor / Student)
- Client-side and server-side validation
- Notifications for admin
- Student feedback and rating for classes and exams
---

## 🖥️ Technologies (Production Version)
- ASP.NET Core (MVC + API)
- Entity Framework Core (SQL)
- Google Calendar API
- Bootstrap 5 (responsive UI)
- jQuery/JavaScript form validation

---

## 📸 Screenshots

<details>
<summary><b>📊 Dashboard - click to expand</b></summary>

<br/>

<table>
<thead>
<tr>
<th align="center">Dashboard - Desktop</th>
<th align="center">Dashboard - Mobile</th>
</tr>
</thead>
<tbody>
<tr>
<td align="center">
<a href="media/screenshots/01-02-Dashboard-desktop.png">
<img src="media/screenshots/01-02-Dashboard-desktop.png" width="520" alt="Dashboard — Desktop">
</a>
</td>
<td align="center">
<a href="media/screenshots/01-01-Dashboard-mobile.png">
<img src="media/screenshots/01-01-Dashboard-mobile.png" width="120" alt="Dashboard — Mobile">
</a>
</td>
</tr>
</tbody>
</table>
</details>





<details>
<summary><b>📅 Schedule - click to expand</b></summary>

<br/>

<table>
<thead>
<tr>
<th align="center">Schedule - Desktop</th>
<th align="center">Schedule - Mobile</th>
</tr>
</thead>
<tbody>
<tr>
<td align="center">
<a href="media/screenshots/02-01-Schedule-Desktop.png">
<img src="media/screenshots/02-01-Schedule-Desktop.png" width="520" alt="Schedule — Desktop">
</a>
</td>
<td align="center">
<a href="media/screenshots/02-02-Schedule-mobile.png">
<img src="media/screenshots/02-02-Schedule-mobile.png" width="120" alt="Schedule — Mobile">
</a>
</td>
</tr>
</tbody>
</table>
</details>




<details>
<summary><b>📝 Exam - click to expand</b></summary>

<br/>

<table>
<thead>
<tr>
<th align="center">Exam Create - Desktop</th>
<th align="center">Exam Options - Mobile</th>
</tr>
</thead>
<tbody>
<tr>
<td align="center">
<a href="media/screenshots/03-01-ExamCr-Desktop.png">
<img src="media/screenshots/03-01-ExamCr-Desktop.png" width="520" alt="ExamCr — Desktop">
</a>
</td>
<td align="center">
<a href="media/screenshots/03-02-ExamOpt-mobile.png">
<img src="media/screenshots/03-02-ExamOpt-mobile.png" width="120" alt="ExamOpt — Mobile">
</a>
</td>
</tr>
</tbody>
</table>
</details>



<details>
<summary><b>🧑‍🎓 Student - click to expand</b></summary>

<br/>

<table>
<thead>
<tr>
<th align="center">Student Dashboard - Desktop</th>
<th align="center">Student HelpWidget - Mobile</th>
</tr>
</thead>
<tbody>
<tr>
<td align="center">
<a href="media/screenshots/04-01-Student-Desktop.png">
<img src="media/screenshots/04-01-Student-Desktop.png" width="520" alt="StudentDash — Desktop">
</a>
</td>
<td align="center">
<a href="media/screenshots/04-02-Student-HelpWidget-mobile.png">
<img src="media/screenshots/04-02-Student-HelpWidget-mobile.png" width="120" alt="HelpWidget — Mobile">
</a>
</td>
</tr>
</tbody>
</table>
</details>



<details>
<summary><b>🖨️ Print - click to expand</b></summary>

<br/>

<table>
<thead>
<tr>
<th align="center">Print - Desktop</th>
</tr>
</thead>
<tbody>
<tr>
<td align="center">
<a href="media/screenshots/05-01-Print.png">
<img src="media/screenshots/05-01-Print.png" width="520" alt="Print — Desktop">
</a>
</tr>
</tbody>
</table>
</details>




<details>
<summary><b>🛡️ Admin - click to expand</b></summary>

<br/>

<table>
<thead>
<tr>
<th align="center">Admin Sync GCalendar - Desktop</th>
<th align="center">Admin Notifications - Desktop</th>
<th align="center">Admin Manage Users - Mobile</th>
  <th align="center">Admin Audit Logs - Desktop</th>
</tr>
</thead>
<tbody>
<tr>
<td align="center">
<a href="media/screenshots/06-02-Admin-SyncGCal.png">
<img src="media/screenshots/06-02-Admin-SyncGCal.png" width="520" alt="Sync GCal — Desktop">
</a>
</td>
  <td align="center">
<a href="media/screenshots/06-03-Admin-Not.png">
<img src="media/screenshots/06-03-Admin-Not.png" width="520" alt="Admin Notifications — Desktop">
</a>
</td>
<td align="center">
<a href="media/screenshots/06-01-Admin-ManageUsers.png.png">
<img src="media/screenshots/06-01-Admin-ManageUsers.png" width="120" alt="ManageUsers — Mobile">
</a>
</td>
  <td align="center">
<a href="media/screenshots/06-04-Audit-log.png">
<img src="media/screenshots/06-04-Audit-log.png" width="120" alt="Audit Logs — Desktop">
</a>
</td>
</tr>
</tbody>
</table>
</details>


More in the **[screenshot gallery](media/screenshots/)**.

---

## 🎥 Quick tour
[▶ Watch the full 1:05 demo (MP4)(download link)](https://github.com/djole994/E-Raspored-docs/releases/download/v0.1.0/E-Raspored.mp4)

[▶ Validation - save + calendar exam](media/gif/01-exam-create.gif)  


---


## 🏗️ Architecture


<details open>
  <summary><b>1) Overview</b></summary>

```mermaid 

%%{init: {"theme":"neutral","themeVariables":{
  "primaryColor":"#906B04","secondaryColor":"#938FCF","tertiaryColor":"#B6DCD6",
  "lineColor":"#2F4F6F","fontFamily":"Inter,Segoe UI,Arial","fontSize":"14px"
}, "flowchart": { "useMaxWidth": false }}}%%
flowchart LR
  RBAC[Security and RBAC]
  CORE[Domain core]
  OUTB[Outbox event store]
  GCAL[Google Calendar sync]
  WH[Webhooks]
  RECON[Sync monitor - calendar reconciliation]
  FB[Feedback and ratings]
  AUD[Audit logs]
  DADM[Admin dashboard]
  DOTH[Other dashboards - Organizer, Professor, Student]

  RBAC --> CORE
  CORE --> FB
  CORE --> OUTB
  OUTB --> GCAL
  OUTB --> WH

  CORE --> RECON
  GCAL --> RECON
  RECON --> DADM
  RECON --> DOTH

  CORE -. audit .-> AUD
  GCAL -. audit .-> AUD
  OUTB -. audit .-> AUD
  RECON -. audit .-> AUD

  linkStyle default stroke:#8aa3af,stroke-width:1.1px,opacity:0.75;



```
</details>
<details open>
  <summary><b>2) Core domain</b></summary>

```mermaid 

%%{init: {"theme":"neutral","themeVariables":{
  "primaryColor":"#6096B4","secondaryColor":"#93BFCF","tertiaryColor":"#BDCDD6",
  "lineColor":"#2f4f60","fontFamily":"Inter,Segoe UI,Arial","fontSize":"14px"
}, "flowchart": { "useMaxWidth": false } }}%%
flowchart LR
  PRG[Study Programs] --> Y[Study Years] --> SBJ[Subjects]
  PROFS[Professors] --> CLS[Class Sessions]
  PROFS --> EX[Exams]
  SBJ --> CLS
  SBJ --> EX

  RM[Rooms] --> BK[Bookings and reservations]
  CLS --> BK
  EX  --> BK

  CC[Conflict checker]
  CLS --> CC
  EX  --> CC

  STUDS[Students]

  linkStyle default stroke:#8aa3af,stroke-width:1.1,opacity:0.75;



```
</details>
<details open>
  <summary><b>3) Integrations & Ops </b></summary>

```mermaid 
%%{init: {"theme":"neutral","themeVariables":{
  "primaryColor":"#6096B4","secondaryColor":"#93BFCF","tertiaryColor":"#BDCDD6",
  "lineColor":"#2f4f60","fontFamily":"Inter,Segoe UI,Arial","fontSize":"14px"
}, "flowchart": { "useMaxWidth": false } }}%%
flowchart LR
  CORE[Domain core] --> OUTB[Outbox event store]
  OUTB --> GCAL[Google Calendar sync]
  OUTB --> WH[Webhooks]

  RECON[Sync monitor - calendar reconciliation]
  CORE --> RECON
  GCAL --> RECON

  subgraph DASH[Dashboards]
    DADM[Admin]
    DORG[Organizer]
    DPROF[Professor]
    DSTUD[Student]
  end
  RECON --> DADM

  subgraph RBAC[Security and RBAC]
    U[Users] --> R[Roles] --> P[Permissions]
  end

  AUD[Audit logs]
  CORE -.audit.-> AUD
  OUTB -.audit.-> AUD
  RECON -.audit.-> AUD

  linkStyle default stroke:#8aa3af,stroke-width:1.1,opacity:0.75;

```
</details>

---

## 🔒 Security Highlights
- HTTPS enforced
- Two-factor authentication on Google accounts
- Role-based permissions per module
- Data validation on multiple levels (client, server, database)
- Restricted access to Google Calendar API

---

## 📄 Documentation
- [System Overview](docs/overview.md)
- [Feature List](docs/features.md)
- [Roadmap](docs/roadmap.md)
- [FAQ](docs/faq.md)

---

## 📬 Contact
📧 Email: djordjeradovic94@gmail.com  
🌐 Demo: *(available upon request)*

---

## 📜 License
© 2025 Đorđe Radović. All rights reserved.  
This repository is for presentation purposes only. Unauthorized use, reproduction, or distribution is prohibited.
