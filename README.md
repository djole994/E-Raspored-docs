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



flowchart LR
  %% Klasteri radi preglednosti
  subgraph SEC[Sigurnost & RBAC]
    U[Users]
    R[Roles]
    PU[ProjectUsers]
    Pm[Permissions]
    U---PU
    R---PU
    U---Pm
  end

  subgraph PRJ[Projekti & Board]
    PR[Projects]
    BC[Columns]
    T[Tasks]
    L[Labels]
    TL[TaskLabels]
    TL---T
    TL---L
    PR---BC
    PR---T
    PR---L
    BC---T
  end

  subgraph SPRINT[Sprintovi]
    SP[Sprints]
    TS[TaskSprints]
    SP---TS
    TS---T
  end

  subgraph COLLAB[Saradnja]
    C[Comments]
    M[Mentions]
    A[Attachments]
    W[Watchers]
    CL[ChecklistItems]
    C---M
    C---A
    T---C
    T---A
    T---W
    T---CL
  end

  subgraph LINKS[Relacije Issue‑a]
    TK[TaskLinks]
    T---TK
  end

  subgraph AUDIT[Audit & Historija]
    AL[AuditLogs]
    TH[TaskHistory]
    T---TH
  end

  subgraph NOTIF[Notifikacije]
    N[Notifications]
  end

  subgraph INTEG[Integracije]
    OX[OutboxMessages]
    WH[Webhooks]
    WD[WebhookDeliveries]
    WH---WD
    OX---WD
  end

  %% Povezivanja između klastera
  U---PR
  PU---PR
  PR---SP
  C---N
  M---N
  OX-.generišu se iz događaja u->PRJ
  OX-.generišu se iz događaja u->COLLAB
  AL-.audit svih promjena.->PRJ
  AL-.audit svih promjena.->COLLAB

