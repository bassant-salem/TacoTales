# 🌮 TacoTales — Restaurant Management System

A full-stack ASP.NET Core MVC web application for managing a restaurant's complete operations: menu management, ingredient tracking, cart, order processing, order status lifecycle, and stock management.

[![.NET](https://img.shields.io/badge/.NET-9.0-purple.svg)](https://dotnet.microsoft.com/)
[![Status](https://img.shields.io/badge/status-live-success.svg)](https://tacotales.runasp.net/)
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)

🔗 **Live Demo**: [https://tacotales.runasp.net](https://tacotales.runasp.net)

---

## 📋 Table of Contents

- [Features](#-features)
- [Tech Stack](#-tech-stack)
- [Architecture](#-architecture)
- [Project Structure](#-project-structure)
- [Database Schema](#-database-schema)
- [Getting Started](#-getting-started)
- [Routes](#-routes)
- [Security](#-security)
- [What I Learned](#-what-i-learned)

---

##  Features

### Customer
- 🛒 **Session-based Cart** — add, update, remove items with quantity validation and toast notifications
- 📦 **Order Placement** — full checkout with price snapshot, stock verification, and DB persistence
- 📜 **Order History** — visual 4-step status stepper per order (Pending → Preparing → Ready → Delivered)
- ❌ **Order Cancellation** — cancel own Pending orders with automatic stock restore
- 🔐 **Authentication** — secure registration and login via ASP.NET Core Identity

### Admin
- 🧾 **Order Management Dashboard** — view all orders, filter by status, advance status with single click
- 🚫 **State Machine Enforcement** — orders can only advance forward; cancellation blocked past Pending
- 🍽️ **Menu Management** — full CRUD with image uploads stored as binary in DB (survives redeploys)
- 🥗 **Ingredient Tracking** — manage ingredients linked to products via many-to-many
- 📊 **Stock Control** — stock decrements on order placement, restores on cancellation

### Technical
- 🧩 **CartService** — all cart business logic extracted from controller into a dedicated service
- 👤 **Per-user Session Isolation** — cart key namespaced by `userId` (`Cart_{userId}`) to prevent bleed-over when switching accounts
- 🔒 **Input Hardening** — every cart action validated before touching the DB (qty range, stock cap, item cap, product existence)
- 📝 **Structured Logging** — `ILogger<T>` throughout with named parameters on every significant action
- ⚡ **Async/Await** — all data operations fully asynchronous

---

##  Tech Stack

| Layer | Technology |
|---|---|
| Framework | ASP.NET Core 9.0 MVC |
| Language | C# 12 |
| ORM | Entity Framework Core 9.0 |
| Database | SQL Server |
| Auth | ASP.NET Core Identity |
| Session | Distributed Memory Cache |
| Frontend | Razor Views, Bootstrap 5, jQuery |
| Hosting | runasp.net |
| Version Control | Git & GitHub |

---

## Architecture

### Layered Structure

```
HTTP Request
     │
     ▼
Controller (thin — HTTP only, no business logic)
     │
     ▼
CartService (all cart business logic, validation, logging)
     │
     ▼
Repository<T> (generic data access layer)
     │
     ▼
ApplicationDbContext → SQL Server
```

### Key Design Decisions

**1. CartService Pattern**
All cart logic lives in `Services/CartService.cs`, not in the controller. The controller calls the service, reads the result, sets TempData, and redirects. This makes the logic testable and keeps controllers thin.

**2. Per-User Session Keys**
Cart data is stored in session under `Cart_{userId}` rather than a shared key. This prevents cart data persisting across account switches in the same browser — a subtle but real bug in many implementations.

**3. Order Status State Machine**
`Order.cs` contains the state transition logic directly on the model:
- `CanAdvance()` — returns true only for non-terminal states
- `CanCancel()` — only valid from Pending
- `Advance()` — moves to next state, throws on terminal state
- `NextStatus()` — preview of next state for button labels

**4. Generic Repository with QueryOptions**
`Repository<T>` accepts a `QueryOptions<T>` object for dynamic includes, filters, and ordering — avoiding N+1 queries and keeping data access consistent across the app.

**5. Stock Lifecycle**
- Stock decrements atomically with order placement in `PlaceOrderAsync`
- Stock restores in `RestoreStockAsync` called by both admin and user cancel paths — single source of truth

---

##  Project Structure

```
TacoTales/
├── Controllers/
│   ├── HomeController.cs
│   ├── ProductController.cs
│   ├── IngredientController.cs
│   ├── OrderController.cs        # User-facing order + cart actions
│   └── AdminOrderController.cs   # Admin order management + status advancement
│
├── Services/
│   └── CartService.cs            # All cart logic, validation, stock, logging
│
├── Models/
│   ├── Order.cs                  # Includes state machine methods
│   ├── OrderStatus.cs            # Pending/Preparing/Ready/Delivered/Cancelled enum
│   ├── OrderItem.cs
│   ├── OrderViewModel.cs
│   ├── OrderItemViewModel.cs
│   ├── Product.cs
│   ├── Category.cs
│   ├── Ingredient.cs
│   ├── ProductIngredient.cs
│   ├── CartValidationResult.cs   # Result object for cart validation
│   ├── Repository.cs
│   ├── IRepository.cs
│   ├── QueryOptions.cs
│   ├── SessionExtentions.cs
│   ├── ApplicationUser.cs
│   └── ErrorViewModel.cs
│
├── Data/
│   └── ApplicationDbContext.cs
│
├── Views/
│   ├── Home/
│   ├── Order/
│   │   ├── Create.cshtml         # Menu page with stock badges
│   │   ├── Cart.cshtml
│   │   ├── ViewOrders.cshtml     # Status stepper + cancel button
│   │   └── OrderDetails.cshtml
│   ├── AdminOrder/
│   │   ├── Index.cshtml          # Orders dashboard with filter tabs
│   │   └── Details.cshtml        # Order detail + advance/cancel
│   ├── Product/
│   ├── Ingredient/
│   └── Shared/
│       └── _Layout.cshtml        # Toast notifications, per-user cart badge
│
└── wwwroot/
    ├── css/
    └── js/
```

---

##  Database Schema

```
Category ──< Product >── ProductIngredient ──< Ingredient
                │
                └──< OrderItem >── Order ──> ApplicationUser
```

### Key Entities

| Entity | Key Fields |
|---|---|
| Product | ProductId, Name, Price, Stock, CategoryId, ImageData |
| Order | OrderId, UserId, OrderDate, TotalAmount, Status |
| OrderItem | OrderItemId, OrderId, ProductId, Quantity, Price (snapshot) |
| Category | CategoryId, Name |
| Ingredient | IngredientId, Name, Stock |

> Note: `OrderItem.Price` is snapshotted at order time — price changes after ordering don't affect existing orders.

---

##  Getting Started

### Prerequisites
- .NET 9.0 SDK
- SQL Server (Express or full)
- Visual Studio 2022

### Setup

```bash
# 1. Clone
git clone https://github.com/bassant-salem/TacoTales.git
cd TacoTales

# 2. Update connection string in appsettings.json
# "DefaultConnection": "Server=...;Database=TacoTalesDB;..."

# 3. Apply migrations
dotnet ef database update

# 4. Run
dotnet run
```

### Default Admin Account
The database seeder creates an admin account on first run.
Check `Data/DbSeeder.cs` for credentials (do not commit real credentials to public repos).

---

##  Routes

### Customer (Authenticated)

| Method | Route | Description |
|---|---|---|
| GET | `/Order/Create` | Menu page with stock badges |
| POST | `/Order/AddItem` | Add to cart with full validation |
| GET | `/Order/Cart` | View cart |
| POST | `/Order/UpdateQuantity` | Update item quantity |
| POST | `/Order/RemoveItem` | Remove item |
| POST | `/Order/PlaceOrder` | Checkout — decrements stock |
| GET | `/Order/ViewOrders` | Order history with status stepper |
| GET | `/Order/OrderDetails/{id}` | Single order detail |
| POST | `/Order/CancelOrder/{id}` | Cancel Pending order + restore stock |

### Admin Only

| Method | Route | Description |
|---|---|---|
| GET | `/AdminOrder` | Orders dashboard with status filter |
| POST | `/AdminOrder/Advance/{id}` | Advance order status (state machine) |
| POST | `/AdminOrder/Cancel/{id}` | Cancel order + restore stock |
| GET | `/AdminOrder/Details/{id}` | Order detail with advance/cancel controls |
| GET/POST | `/Product/AddEdit` | Add or edit menu item |
| POST | `/Product/Delete/{id}` | Delete product |
| GET/POST | `/Ingredient/...` | Full ingredient CRUD |

---

##  Security

- **CSRF Protection** — `[ValidateAntiForgeryToken]` on every POST
- **Role-Based Authorization** — `[Authorize(Roles = "Admin")]` on all admin routes
- **Ownership Checks** — users cannot view or cancel other users' orders (returns 403)
- **Input Validation** — all cart inputs validated server-side before DB access
- **SQL Injection Prevention** — parameterized queries via EF Core
- **HTTPS** — enforced in production

---

##  What I Learned

### Patterns & Architecture
- Extracting business logic from controllers into a dedicated service class (`CartService`) — keeping controllers as thin HTTP handlers
- Implementing a **state machine** directly on a domain model (`Order.CanAdvance()`, `Order.Advance()`) for enforced, predictable transitions
- Using a `CartValidationResult` result object instead of exceptions for control flow in validation paths
- Generic repository with `QueryOptions<T>` for flexible, reusable data access without repeating query logic

### ASP.NET Core Specifics
- Session isolation per user using namespaced keys — preventing subtle cart bleed-over bugs across accounts
- `IHttpContextAccessor` for accessing session outside of controllers
- `ILogger<T>` structured logging with named parameters throughout service and controller layers
- `[ValidateAntiForgeryToken]` and ownership checks as defense-in-depth on every sensitive action

### Entity Framework Core
- Snapshotting prices at order time so price changes don't retroactively affect past orders
- Optimistic stock decrement — trading strict consistency for simplicity, appropriate for this scale
- `ThenInclude` for multi-level eager loading (`OrderItems.Product`)
- Code-First migrations for schema evolution

### Real-World Problem Solving
- Per-user session cart keys to fix account-switching bug
- Stock restore on cancellation as a single shared method called by both admin and user cancel paths
- Toast notifications in `_Layout.cshtml` as the single source of user feedback — removing duplicate inline alerts from views
