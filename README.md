# E-Raspored — Academic Scheduling & Exam Management

E-Raspored is a modern academic scheduling system for managing classes, exams, and resources in educational institutions.  
It allows administrators, professors, and students to plan and track academic activities in one place, with Google Calendar integration.

> **Note:**  
> This repository contains **only documentation, screenshots, and demo materials**.  
> The source code is proprietary and not publicly available.

---

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
<summary>📸 Admin Dashboard</summary>

![Dashboard](media/screenshots/01-admin-dashboard.png)

</details>

<details>
<summary>📸 Admin Users</summary>

![Admin-Users](media/screenshots/02-admin-users.png)

</details>

<details>
<summary>📸 Schedule – Desktop</summary>

![Nastava-Desktop](media/screenshots/03-nastava-desktop.png)

</details>

<details>
<summary>📸 Schedule – Mobile</summary>

![Nastava-Mobile](media/screenshots/04-nastava-mobile.png)

</details>

<details>
<summary>📸 Exam – Overview</summary>

![Ispit-View](media/screenshots/05-ispit-view.png)

</details>

<details>
<summary>📸 Exam – Create</summary>

![Ispit-Create](media/screenshots/06-ispit-create.png)

</details>

<details>
<summary>📸 Student Dashboard & Feedback & Help Widget</summary>

![Student-Dashboard](media/screenshots/07-studentdash-rate-helpwidget.png)

</details>


More in the **[screenshot gallery](media/screenshots/)**.

---

## 🎥 Demo Video
[▶ Watch the demo](media/demo.mp4)  
*(or request a live demo via email)*

---


## Architecture <img src="docs/assets/architecture-icon.svg" width="22" alt="Architecture icon" />

<details open>
  <summary><b>1) Overview</b></summary>

```mermaid 

%%{init: {"theme":"neutral","themeVariables":{
  "primaryColor":"#6096B4","secondaryColor":"#93BFCF","tertiaryColor":"#BDCDD6",
  "lineColor":"#2f4f60","fontFamily":"Inter,Segoe UI,Arial","fontSize":"14px"
}, "flowchart": { "useMaxWidth": false } }}%%
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

  CORE -.audit.-> AUD
  FB   -.audit.-> AUD
  OUTB -.audit.-> AUD
  RECON -.audit.-> AUD

  linkStyle default stroke:#8aa3af,stroke-width:1.1,opacity:0.75;


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
- [Architecture](docs/architecture.md)
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


## Overview



## Core domain
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


## Integrations, Notifications, Audit, RBAC, Dashboards
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
