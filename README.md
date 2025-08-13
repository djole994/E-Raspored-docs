# eRaspored — Academic Scheduling & Exam Management

eRaspored is a modern academic scheduling system for managing classes, exams, and resources in educational institutions.  
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



## 1) System overview (high level)

%%{init: {"theme":"neutral","themeVariables":{
  "primaryColor":"#6096B4",
  "secondaryColor":"#93BFCF",
  "tertiaryColor":"#BDCDD6",
  "lineColor":"#2f4f60",
  "fontFamily":"Inter,Segoe UI,Arial"
}}}%%
flowchart TB
  %% Clusters
  subgraph RBAC[Security & RBAC]
    U[Users]
    R[Roles]
    P[Permissions]
  end

  subgraph Core[Domain core]
    PRG[Study Programs]
    Y[Study Years]
    SBJ[Subjects]
    PROFS[Professors]
    STUDS[Students]
    RM[Rooms]
    CLS[Class Sessions]
    EX[Exams]
    BK[Bookings/Reservations]
    CC[Conflict Checker]
  end

  subgraph Dash[Dashboards]
    DPROF[Professor dashboard]
    DSTUD[Student dashboard]
    DORG[Organizer dashboard]
    DADM[Admin dashboard]
  end

  subgraph FB[Feedback]
    FBK[Feedback & ratings]
    AN[Feedback analytics]
  end

  subgraph Notif[Notifications]
    NT[Notifications (email/push)]
  end

  subgraph Int[Integrations]
    OUTB[Outbox event store]
    GCAL[Google Calendar sync]
    WH[Webhooks]
  end

  subgraph AUD[Audit & history]
    AL[Audit logs]
  end

  %% Edges
  U --> R --> P
  PRG --> Y --> SBJ
  SBJ --> CLS
  SBJ --> EX
  RM --> BK
  CLS --> BK
  EX  --> BK
  PROFS --> CLS
  PROFS --> EX
  STUDS --> FBK
  EX --> FBK
  CLS --> FBK

  DORG --> CLS
  DORG --> EX
  CLS --> CC
  EX  --> CC
  CC -->|OK| OUTB
  OUTB --> GCAL
  OUTB --> WH

  CLS --> NT
  EX  --> NT
  NT --> DPROF
  NT --> DSTUD

  CLS --> DPROF
  EX  --> DPROF
  CLS --> DSTUD
  EX  --> DSTUD
  PRG --> DADM
  Y   --> DADM
  BK  --> DADM

  CLS -.audit.-> AL
  EX  -.audit.-> AL
  FBK -.audit.-> AL
  NT  -.audit.-> AL
