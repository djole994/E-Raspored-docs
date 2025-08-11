# Architecture

eRaspored follows a multi-layered approach:
- **UI (Web/MVC)** – user interface and client-side validations
- **API layer** – HTTP endpoints for working with events and metadata
- **Application logic** – business rules, conflict checks, authorization
- **Database access (EF Core)** – entity mapping and queries
- **Database (PostgreSQL)** – persistence for events, users, and rooms
- **Integration (Google Calendar API)** – event synchronization

## High-Level Diagram

```mermaid
flowchart LR
A[Browser (Users)] --> B["ASP.NET Core MVC and API"]
B --> C[Application / Domain Logic]
C --> D["EF Core"]
D --> E["PostgreSQL"]
C --> F["Google Calendar API"]

