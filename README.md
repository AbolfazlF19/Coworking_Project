# Coworking Space Management System

A comprehensive **coworking space management platform** built with ASP.NET Core 8 MVC. This system enables users to book workspaces, manage reservations, handle pricing, and maintain spaces—all with a modern glassmorphism UI.

---

## 📌 Table of Contents

- [Overview](#-overview)
- [Live Demo (Development)](#-live-demo-development)
- [Features](#-features)
- [Tech Stack](#-tech-stack)
- [User Roles](#-user-roles)
- [Database Schema](#-database-schema)
- [Installation & Setup](#-installation--setup)
- [Project Structure](#-project-structure)
- [Key Functionalities in Detail](#-key-functionalities-in-detail)
  - [Reservation System](#reservation-system)
  - [Space Management with Image Gallery](#space-management-with-image-gallery)
  - [Pricing Management](#pricing-management)
  - [User Management](#user-management)
- [Screenshots](#-screenshots)
- [Known Limitations](#-known-limitations)
- [Future Improvements](#-future-improvements)
- [Contributing](#-contributing)
- [License](#-license)

---

## 📖 Overview

The **Coworking Space Management System** is a full-featured web application designed for managing coworking spaces, memberships, reservations, and maintenance. The system supports three distinct user roles with different levels of access:

- **Members** can browse spaces, make reservations, view their booking history, and manage their profile.
- **Staff** can manage reservations and payments on behalf of members.
- **Admins** have full control over spaces, equipment, pricing, members, staff, and maintenance records.

The project follows the **MVC (Model-View-Controller)** architectural pattern with **Entity Framework Core** for database operations and **SQL Server** as the database engine.

---

## 🖥️ Live Demo (Development)

> The application runs locally on `https://localhost:44303` during development.

**Demo Accounts:**

| Role | Email | Password |
|------|-------|----------|
| Admin | admin@coworking.com | Admin@123 |
| Member | member@coworking.com | Member@123 |

> ⚠️ **Note:** Demo accounts are seeded automatically on first run. You can disable seeding by commenting out `SeedData.InitializeAsync(services)` in `Program.cs`.

---

## ✨ Features

### Core Features
- **User Authentication & Authorization** – Identity-based login with three roles: Admin, Staff, Member.
- **Space Management** – CRUD operations for workspaces with image upload, drag-and-drop reordering, and primary image selection.
- **Reservation System** – Book spaces with real‑time availability checks, 30‑minute cleaning buffer, and cancellation policies.
- **Pricing Management** – Define hourly rates with effective date ranges and automatic conflict detection.
- **Maintenance Scheduling** – Schedule maintenance for spaces; automatically blocks reservations during maintenance windows.
- **Member Profile** – Members can view their details and change their full name and password.
- **Payment Records** – Payment records are stored (mock mode; no real payment gateway integration).
- **Glassmorphism UI** – Modern, responsive UI with blur effects and gradient backgrounds.

### Advanced Features
- **Real-time Availability** – AJAX-based busy slot display when booking, showing reservations and maintenance conflicts.
- **30‑Minute Cleaning Buffer** – Reservations automatically have a 30‑minute buffer before and after to prevent overlaps.
- **24‑Hour Cancellation Policy** – Members can only cancel reservations at least 24 hours before start time.
- **Closed Hours** – Reservations are blocked between 11:00 PM and 8:00 AM daily.
- **Image Gallery** – Lightbox full-screen viewer for space images on both public and member pages.
- **Drag-and-Drop Image Reordering** – Admins can reorder space images via drag-and-drop and set a primary image.

---

## 🛠️ Tech Stack

| Technology | Purpose |
|------------|---------|
| **ASP.NET Core 8 MVC** | Backend framework |
| **Entity Framework Core 8** | ORM for database operations |
| **SQL Server** | Relational database |
| **Identity Framework** | User authentication & role management |
| **Bootstrap 5** | Frontend CSS framework |
| **jQuery** | Client-side scripting |
| **Bootstrap Icons** | Icon library |
| **HTML5 / CSS3** | Markup & styling |
| **JavaScript (Vanilla)** | AJAX, form validation, image handling |

---

## 👤 User Roles

| Role | Capabilities |
|------|--------------|
| **Member** | – Browse and book spaces<br>– View and cancel own reservations (≥24h notice)<br>– View own payments<br>– View space details (with gallery, no price history)<br>– Update profile (full name, password)<br>– Cancel pending reservations |
| **Staff** | – Manage all reservations (Confirm, Complete, Cancel)<br>– View all payments<br>– View space details (full admin view)<br>– No access to system configuration |
| **Admin** | – Full CRUD: Spaces, Equipment, SpaceEquipment, Prices, Members, Staff, Maintenance<br>– Manage reservations (Confirm, Complete, Cancel, Edit, Delete)<br>– Upload and manage space images<br>– Full system control |

---

## 🗄️ Database Schema

The database contains the following core tables:

- **AspNetUsers** – Identity users with role assignments.
- **Members** – Member profiles linked to AspNetUsers via `UserId`.
- **Staff** – Staff profiles with roles (`Manager`, `Receptionist`, `Cleaner`).
- **Spaces** – Workspaces with name, type, capacity, location, and active status.
- **SpaceImages** – Image metadata (path, display order, primary flag) linked to Spaces.
- **Equipment** – Equipment items available for assignment.
- **SpaceEquipment** – Junction table linking spaces and equipment with quantity.
- **Prices** – Pricing rules per space with effective date ranges.
- **Reservations** – Booking records with member, space, time range, status, and applied pricing.
- **Payments** – Payment records linked to reservations.
- **SpaceMaintenance** – Maintenance schedules linked to spaces and staff.

**Relationships:**
- `Spaces` ↔ `SpaceImages`: One-to-many (cascade delete).
- `Spaces` ↔ `Prices`: One-to-many.
- `Spaces` ↔ `SpaceEquipment`: One-to-many (junction).
- `Spaces` ↔ `Reservations`: One-to-many.
- `Members` ↔ `Reservations`: One-to-many.
- `Reservations` ↔ `Payments`: One-to-one.
- `Staff` ↔ `SpaceMaintenance`: One-to-many.

---

## ⚙️ Installation & Setup

### Prerequisites
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [SQL Server](https://www.microsoft.com/en-us/sql-server/sql-server-downloads) (or SQL Server Express)
- [Visual Studio 2022](https://visualstudio.microsoft.com/) (recommended) or VS Code

### Step-by-Step Installation

1. **Clone the repository:**
```bash
git clone https://github.com/yourusername/coworking-space-management.git
cd coworking-space-management
```

2. **Restore dependencies:**
```bash
dotnet restore
```

3. **Update the connection string** in `appsettings.json`:
```json
"ConnectionStrings": {
  "DefaultConnection": "Server=YOUR_SERVER;Database=CoworkingDB;Trusted_Connection=True;TrustServerCertificate=True;"
}
```

4. **Apply database migrations:**
```bash
dotnet ef database update
```

5. **Seed initial data (optional):**  
The project includes a `SeedData` class that automatically creates demo accounts and sample data on first run. To disable seeding, comment out `SeedData.InitializeAsync(services)` in `Program.cs`.

6. **Run the application:**
```bash
dotnet run
```
Navigate to `https://localhost:44303` in your browser.

---

## 📂 Project Structure

```
CoworkingSpace.Web/
├── Controllers/
│   ├── AccountController.cs
│   ├── DashboardController.cs
│   ├── EquipmentController.cs
│   ├── HomeController.cs
│   ├── MaintenanceController.cs
│   ├── MembersController.cs
│   ├── PaymentsController.cs
│   ├── PricesController.cs
│   ├── ProfileController.cs
│   ├── ReservationsController.cs
│   ├── SpacesController.cs
│   ├── StaffController.cs
│   └── MemberSpacesController.cs
├── Views/
│   ├── Account/
│   │   ├── Login.cshtml
│   │   ├── Register.cshtml
│   │   └── AccessDenied.cshtml
│   ├── Home/
│   │   └── Index.cshtml
│   ├── Reservations/
│   │   ├── Index.cshtml
│   │   ├── Create.cshtml
│   │   ├── Details.cshtml
│   │   ├── Edit.cshtml
│   │   └── Delete.cshtml
│   ├── Spaces/
│   │   ├── Index.cshtml
│   │   ├── Create.cshtml
│   │   ├── Edit.cshtml
│   │   ├── Details.cshtml
│   │   └── Delete.cshtml
│   ├── Staff/
│   │   ├── Index.cshtml
│   │   ├── Create.cshtml
│   │   ├── Edit.cshtml
│   │   ├── Details.cshtml
│   │   └── Delete.cshtml
│   ├── Prices/
│   │   ├── Index.cshtml
│   │   ├── Create.cshtml
│   │   ├── Edit.cshtml
│   │   └── Delete.cshtml
│   └── Shared/
│       ├── _Layout.cshtml
│       └── _LoginPartial.cshtml
├── Models/
│   ├── Space.cs
│   ├── SpaceImage.cs
│   ├── Reservation.cs
│   ├── Price.cs
│   ├── Staff.cs
│   ├── Member.cs
│   ├── Equipment.cs
│   └── ViewModels/
│       ├── RegisterViewModel.cs
│       ├── LoginViewModel.cs
│       └── ReservationCreateViewModel.cs
├── Data/
│   ├── ApplicationDbContext.cs
│   └── SeedData.cs
├── wwwroot/
│   ├── css/
│   ├── images/
│   └── lib/
├── Migrations/
├── Program.cs
├── appsettings.json
└── CoworkingSpace.Web.csproj
```

---

## 🧩 Key Functionalities in Detail

### Reservation System

- **Real‑time Availability Check:** When a member selects a space and time range, the system fetches busy slots via AJAX and displays them immediately.
- **30‑Minute Buffer:** Reservations automatically extend by 30 minutes before and after to allow cleaning and preparation.
- **Closed Hours:** Reservations are blocked between 23:00 and 08:00.
- **Min. 2‑Hour Advance:** Members can only make reservations at least 2 hours in advance.
- **Cancellation Policy:** Members can cancel their reservations **only if** the start time is at least 24 hours away.
- **Conflict Detection:** Prevents double-booking and conflicts with maintenance.

### Space Management with Image Gallery

- **Image Upload:** Admins can upload multiple images when creating or editing a space.
- **Drag‑and‑Drop Reorder:** Images can be reordered by dragging.
- **Primary Image Selection:** One image can be marked as primary (used as thumbnail across the site).
- **Lightbox Viewer:** Full‑screen gallery viewer with navigation and counter.
- **Member‑Facing Details Page:** Members see space details without price history, and images are displayed larger than in the admin view.

### Pricing Management

- **Effective Date Ranges:** Each price has a start and end date.
- **Conflict Detection:** The system prevents overlapping price periods for the same space. Adjacent periods (e.g., one ends at 23:59 and another starts at 00:00) are allowed.
- **Automatic Price Resolution:** When a reservation is created, the system automatically finds the applicable price based on the selected time range.

### User Management

- **Member Profile:** Members can view their details and change their full name and password.
- **Staff Management:** Admins can add, edit, and delete staff members (Role field is now a string field to allow custom role names, including Persian).
- **Member Management:** Admins can view, edit, and delete members.

---

## 📸 Screenshots

> *(Add screenshots here once you have them – e.g., home page, reservation form, admin dashboard, space gallery)*

---

## ⚠️ Known Limitations

| Limitation | Explanation |
|------------|-------------|
| **Email Verification** | Not implemented due to lack of SMTP/email API integration. Users are registered without email confirmation. |
| **Password Reset** | Password reset flow is missing (no "Forgot Password" link) for the same reason. |
| **Payment Gateway** | No real payment gateway integrated; payment records are stored for demonstration purposes only. |
| **Staff Role as String** | The `Staff.Role` field is stored as a string to allow custom role names, including Persian text. |
| **Image Storage** | Images are stored in `wwwroot/images/spaces` and not optimized (no thumbnails). Future improvement: use image processing to generate thumbnails. |
| **Localization** | UI is in English, but some data (like Staff roles) may be entered in Persian. |

---

## 🔮 Future Improvements

- **Email Service Integration:** Add SMTP or SendGrid for email confirmation and password reset.
- **Real Payment Integration:** Integrate with Stripe or local payment gateways (e.g., Zarinpal).
- **Thumbnail Generation:** Optimize images by generating multiple sizes.
- **Filtering & Search:** Add search/filter functionality to space listings and reservation history.
- **Notifications:** Send in-app or email notifications for upcoming reservations.
- **Reporting Dashboard:** Advanced analytics for admins (revenue, occupancy rates, etc.).
- **Staff Dashboard:** A dedicated dashboard for staff members with quick actions.
- **Multi-language Support:** Add Persian (Farsi) as a secondary language.

---

## 👥 Contributing

This project is a university assignment and is not currently open for contributions. However, if you have suggestions or find bugs, feel free to open an issue or reach out.

---

## 📄 License

This project is licensed under the **MIT License** – see the [LICENSE](LICENSE) file for details.

---

## 🙏 Acknowledgments

- Built as a university project to demonstrate ASP.NET Core MVC, Entity Framework Core, and modern web development practices.
- Inspired by real‑world coworking space management platforms.

---

**Developed with ❤️ using ASP.NET Core 8 MVC**

---

> **Note:** This README reflects the current state of the project. For the latest updates, please refer to the commit history and documentation.