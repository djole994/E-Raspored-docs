## 🚧 Future Roadmap

<details>
  <summary><b>1) Realtime updates (SignalR)</b></summary>

- [ ] Live updates for schedule changes on all dashboards  
- [ ] In-app toasts/badges for conflicts and changes  
- [ ] Optional Redis backplane for scale-out  
- [ ] Instrumentation (Serilog + OpenTelemetry)
</details>

<details>
  <summary><b>2) Attendance Tracking</b></summary>

Enable professors and students to record attendance for lectures and exams, with options for viewing and exporting reports.  
- [ ] Student QR / code entry  
- [ ] Bulk import/export (CSV)  
- [ ] Audit trail per session
</details>

<details>
  <summary><b>3) Professor Workload Monitoring</b></summary>

Automatically track the number of teaching hours and activities per professor, generating reports for payroll calculations.  
- [ ] Hour aggregation by subject/program/year  
- [ ] Overtime rules and exceptions  
- [ ] Export to PDF/CSV
</details>

<details>
  <summary><b>4) Improving Teaching Quality</b></summary>

Add an option for students to leave comments alongside their ratings, in order to collect constructive feedback.  
- [ ] Anonymous mode (per policy)  
- [ ] Spam/abuse filtering  
- [ ] Topic tagging for analysis
</details>

<details>
  <summary><b>5) Analytics & Reporting</b></summary>

Add a data analytics module to provide:  
• Course attendance statistics  
• Room utilization reports  
• Student performance reports by subject
</details>

<details>
  <summary><b>6) Localization (English)</b></summary>

Plan to add full English UI and prepare the app for multiple languages (i18n).

- [ ] Extract all UI strings (backend .resx, frontend JSON)
- [ ] ASP.NET Core: IStringLocalizer + middleware for culture
- [ ] React: i18next/react-i18next setup
- [ ] Language switcher in header (persist to profile/localStorage)
- [ ] Date/number/time formatting via Intl API
- [ ] Pluralization & interpolation rules
- [ ] Fallback for missing keys + logging
- [ ] Translation workflow (files + review)
</details>
