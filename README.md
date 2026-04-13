# 🌮 TacoTales - Restaurant Management System

A comprehensive ASP.NET Core MVC web application for managing a restaurant's complete operations including menu management, ingredient tracking, order processing, and customer interactions.

![License](https://img.shields.io/badge/license-MIT-blue.svg)
![.NET](https://img.shields.io/badge/.NET-9.0-purple.svg)
![Status](https://img.shields.io/badge/status-active-success.svg)

## 📋 Table of Contents
- [Features](#features)
- [Tech Stack](#tech-stack)
- [Screenshots](#screenshots)
- [Architecture](#architecture)
- [Database Schema](#database-schema)
- [Getting Started](#getting-started)
- [API Endpoints](#api-endpoints)
- [What I Learned](#what-i-learned)
- [Future Enhancements](#future-enhancements)
- [Contact](#contact)

## ✨ Features

### Customer Features
- 🛒 **Shopping Cart System**: Session-based cart with add/remove/update functionality
- 🍽️ **Menu Browsing**: View products organized by categories with images and descriptions
- 📦 **Order Placement**: Complete checkout process with order confirmation
- 📜 **Order History**: View past orders with detailed line items
- 🔐 **User Authentication**: Secure registration and login with ASP.NET Identity

### Management Features
- 📋 **Menu Management**: Full CRUD operations for menu items with image uploads
- 🥗 **Ingredient Tracking**: Manage ingredients and their stock levels
- 🔗 **Product-Ingredient Relations**: Many-to-many relationships for recipe management
- 🏷️ **Category Management**: Organize menu items (Appetizer, Entree, Side, Dessert, Beverage)
- 📊 **Inventory Control**: Track product stock and availability
- 🖼️ **Image Management**: Upload and store product images with fallback placeholders

### Technical Features
- ⚡ **Generic Repository Pattern**: Reusable data access layer with `Repository<T>`
- 🔄 **Session Management**: Custom session extensions for type-safe cart storage
- 📱 **Responsive Design**: Bootstrap 5 for mobile-friendly interface
- 🔍 **Advanced Filtering**: Dynamic query options with Include functionality
- 🎯 **Async/Await**: Fully asynchronous data operations for better performance

## 🛠️ Tech Stack

### Backend
- **Framework**: ASP.NET Core 9.0 MVC
- **Language**: C# 12
- **ORM**: Entity Framework Core 9.0
- **Database**: SQL Server 2022
- **Authentication**: ASP.NET Core Identity
- **Session Storage**: In-memory distributed cache

### Frontend
- **View Engine**: Razor Pages
- **CSS Framework**: Bootstrap 5
- **JavaScript**: jQuery
- **HTML/CSS**: HTML5, CSS3

### Architecture & Patterns
- **Design Pattern**: Generic Repository Pattern
- **Architecture**: MVC with Layered Architecture
- **Dependency Injection**: Built-in ASP.NET Core DI
- **Code Quality**: SOLID Principles, Clean Code

### Development Tools
- **IDE**: Visual Studio 2022
- **Version Control**: Git & GitHub
- **Database Tools**: SQL Server Management Studio

## 📸 Screenshots

<img width="1920" height="1011" alt="Image" src="https://github.com/user-attachments/assets/617f57ef-4a28-4127-8441-a2c4bef5cfc4" />
<img width="1920" height="997" alt="Image" src="https://github.com/user-attachments/assets/6cac0530-1dee-4e9c-9c57-00e009a040db" />
<img width="1920" height="1004" alt="Image" src="https://github.com/user-attachments/assets/2fd35171-0a95-411c-b4f3-fd869d18c4de" />
<img width="1920" height="1021" alt="Image" src="https://github.com/user-attachments/assets/d76ed906-72a7-43f9-a81f-325b1bde756d" />

## 🏗️ Architecture

### Project Structure
```
TacoTales/
├── Controllers/              # MVC Controllers
│   ├── HomeController.cs     # Landing page and menu display
│   ├── ProductController.cs  # Product CRUD operations
│   ├── IngredientController.cs # Ingredient management
│   └── OrderController.cs    # Order processing and cart
│
├── Data/
│   └── ApplicationDbContext.cs # EF Core DbContext with configurations
│
├── Models/                   # Domain Models
│   ├── Product.cs           # Menu items
│   ├── Order.cs             # Customer orders
│   ├── OrderItem.cs         # Order line items
│   ├── Ingredient.cs        # Recipe ingredients
│   ├── Category.cs          # Menu categories
│   ├── ProductIngredient.cs # Many-to-many junction
│   └── Repository.cs        # Generic repository implementation
│
├── ViewModels/              # Data transfer objects
│   ├── ProductViewModel.cs
│   └── OrderViewModel.cs
│
├── Views/                   # Razor Views
│   ├── Home/
│   ├── Product/
│   ├── Order/
│   ├── Ingredient/
│   └── Shared/
│       ├── _Layout.cshtml   # Main layout
│       └── Components/      # View components
│
└── wwwroot/                 # Static files
    ├── css/                 # Stylesheets
    ├── js/                  # JavaScript files
    └── images/              # Product images
```

### Design Patterns Implemented

**1. Generic Repository Pattern**
```csharp
public class Repository<T> : IRepository<T> where T : class
{
    private readonly ApplicationDbContext _context;
    private readonly DbSet<T> _dbSet;
    
    public async Task<List<T>> GetAllAsync(QueryOptions<T> options = null)
    {
        IQueryable<T> query = _dbSet;
        
        // Apply includes for related data
        if (options?.Includes != null)
            foreach (var include in options.Includes)
                query = query.Include(include);
        
        // Apply filters
        if (options?.Where != null)
            query = query.Where(options.Where);
            
        return await query.ToListAsync();
    }
}
```

**2. Session Extension Methods**
```csharp
public static class SessionExtensions
{
    public static void SetObject<T>(this ISession session, string key, T value)
    {
        session.SetString(key, JsonSerializer.Serialize(value));
    }
    
    public static T GetObject<T>(this ISession session, string key)
    {
        var value = session.GetString(key);
        return value == null ? default : JsonSerializer.Deserialize<T>(value);
    }
}
```

## 🗄️ Database Schema

### Entity Relationship Diagram

```
┌──────────┐         ┌─────────────┐         ┌────────────┐
│ Category │────────▶│   Product   │◀────────│ Ingredient │
└──────────┘         └─────────────┘         └────────────┘
                           │                         ▲
                           │                         │
                           ▼                         │
                     ┌─────────────┐                 │
                     │    Order    │                 │
                     └─────────────┘                 │
                           │                         │
                           │                         │
                           ▼                         │
                     ┌─────────────┐                 │
                     │  OrderItem  │                 │
                     └─────────────┘                 │
                                                     │
                     ┌──────────────────────────────┘
                     │ ProductIngredient (Junction)
                     └─────────────────────────────
```

### Key Entities

**Product**
- Id, Name, Description, Price, Stock, ImagePath, CategoryId
- One-to-Many with Category
- Many-to-Many with Ingredient (via ProductIngredient)

**Order**
- Id, OrderDate, TotalAmount, UserId, Status
- One-to-Many with OrderItem
- Belongs to User (ASP.NET Identity)

**OrderItem**
- Id, OrderId, ProductId, Quantity, Price
- Many-to-One with Order and Product

**Ingredient**
- Id, Name, StockQuantity, Unit
- Many-to-Many with Product

**Category**
- Id, Name (Appetizer, Entree, Side Dish, Dessert, Beverage)
- One-to-Many with Product

### Sample Data Seeding

The application includes comprehensive seed data:
- **5 Categories**: Appetizer, Entree, Side Dish, Dessert, Beverage
- **6 Ingredients**: Beef, Chicken, Fish, Tortilla, Lettuce, Tomato
- **3 Products**: Beef Taco, Chicken Taco, Fish Taco (with images and relationships)

## 🚀 Getting Started

### Prerequisites
- .NET 9.0 SDK or later
- SQL Server 2019+ (or SQL Server Express/LocalDB)
- Visual Studio 2022 / VS Code / Rider
- Node.js (optional, for frontend tooling)

### Installation

1. **Clone the repository**
```bash
git clone https://github.com/bassant-salem/TacoTales.git
cd TacoTales
```

2. **Update Database Connection**
Edit `appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=TacoTalesDB;Trusted_Connection=True;MultipleActiveResultSets=true"
  }
}
```

3. **Apply Migrations and Seed Data**
```bash
# Create and update database
dotnet ef database update

# Or if migrations don't exist yet
dotnet ef migrations add InitialCreate
dotnet ef database update
```

4. **Run the Application**
```bash
dotnet run
```

5. **Access the Application**
- **URL**: `https://localhost:5001` or `http://localhost:5000`


### Quick Test

1. Browse the menu as a guest
2. Register a new account
3. Add items to cart
4. Complete an order
5. View order history
6. Login as admin to manage products/ingredients

## 📡 API Endpoints / Routes

### Public Routes

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/` | Homepage with featured products |
| GET | `/Product` | Browse all menu items |
| GET | `/Product/Details/{id}` | View product details |

### Order Management (Authenticated)

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/Order/Create` | Start order (product selection) |
| POST | `/Order/AddItem` | Add product to cart |
| GET | `/Order/Cart` | View shopping cart |
| POST | `/Order/UpdateCart` | Update item quantities |
| POST | `/Order/RemoveItem` | Remove item from cart |
| POST | `/Order/PlaceOrder` | Submit order |
| GET | `/Order/ViewOrders` | View order history |
| GET | `/Order/OrderDetails/{id}` | View specific order |

### Admin Routes (Authorized)

**Product Management**
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/Product/AddEdit/{id?}` | Add/Edit product form |
| POST | `/Product/AddEdit` | Submit product changes |
| POST | `/Product/Delete/{id}` | Delete product |

**Ingredient Management**
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/Ingredient` | List all ingredients |
| GET | `/Ingredient/Create` | Create ingredient form |
| POST | `/Ingredient/Create` | Submit new ingredient |
| GET | `/Ingredient/Edit/{id}` | Edit ingredient form |
| POST | `/Ingredient/Edit` | Update ingredient |
| GET | `/Ingredient/Delete/{id}` | Delete confirmation |
| POST | `/Ingredient/Delete` | Delete ingredient |

## 📚 What I Learned

### Technical Accomplishments

**Entity Framework Core Mastery**
- Implemented Code-First approach with fluent API configurations
- Created complex many-to-many relationships without explicit junction entities
- Optimized queries using Include() for eager loading
- Applied migrations for database versioning

**Advanced .NET Concepts**
- Built generic repository with `QueryOptions<T>` for flexible querying
- Implemented custom session extensions for type-safe object storage
- Used dependency injection throughout the application
- Applied async/await patterns for all data operations

**Full-Stack Development**
- Integrated file upload functionality with validation
- Implemented session-based shopping cart with JSON serialization
- Created responsive UI components with Bootstrap 5
- Built CRUD operations following RESTful conventions

**Software Engineering Practices**
- Applied Repository pattern for separation of concerns
- Followed SOLID principles (especially Single Responsibility)
- Implemented proper error handling and validation
- Used meaningful naming conventions and code organization

### Problem-Solving Highlights

1. **Shopping Cart Persistence**: Created custom session extensions to serialize/deserialize cart objects, maintaining state across requests without database overhead

2. **Image Management**: Implemented file upload with unique filename generation and fallback to placeholder images for better user experience

3. **Complex Relationships**: Designed and implemented many-to-many relationship between Products and Ingredients using EF Core conventions

4. **Query Optimization**: Created `QueryOptions<T>` class allowing dynamic includes and filters, preventing N+1 query problems

5. **Seed Data Strategy**: Implemented comprehensive seed data for realistic testing and demonstration

## 🔮 Future Enhancements

### High Priority
- [ ] **Payment Integration**: Stripe/PayPal for online payments
- [ ] **Order Status Tracking**: Real-time order updates (Pending → Preparing → Ready → Delivered)
- [ ] **Admin Dashboard**: Analytics, sales reports, popular items
- [ ] **Email Notifications**: Order confirmations and updates
- [ ] **Reviews & Ratings**: Customer feedback system

### Medium Priority
- [ ] **Advanced Search**: Filter by price, category, ingredients, dietary restrictions
- [ ] **Favorites/Wishlist**: Save favorite menu items
- [ ] **Promo Codes/Discounts**: Coupon system for marketing
- [ ] **Multi-Restaurant**: Support for multiple locations
- [ ] **Delivery Integration**: Integration with delivery services
- [ ] **Table Reservations**: Booking system for dine-in

### Technical Improvements
- [ ] **API Version**: RESTful API for mobile app integration
- [ ] **Unit Tests**: xUnit tests for business logic and repositories
- [ ] **Caching**: Redis for improved performance
- [ ] **Logging**: Serilog for structured logging
- [ ] **Docker**: Containerization for easy deployment
- [ ] **CI/CD**: Automated deployment pipeline

### Advanced Features
- [ ] **Inventory Alerts**: Low stock notifications
- [ ] **Loyalty Program**: Points system for repeat customers
- [ ] **Nutritional Info**: Calorie and allergy information
- [ ] **Kitchen Display**: Real-time order screen for kitchen staff
- [ ] **Analytics**: Sales trends, customer insights with Chart.js
- [ ] **Multi-Language**: i18n support for international users



## 🔐 Security Features

- **ASP.NET Identity**: Secure password hashing and user management
- **Anti-Forgery Tokens**: CSRF protection on all POST requests
- **Authorization Filters**: Role-based access control for admin features
- **Input Validation**: Data annotations and server-side validation
- **SQL Injection Prevention**: Parameterized queries via EF Core
- **HTTPS Enforcement**: Secure communication in production

## 🤝 Contributing

This is a personal learning project, but suggestions are welcome!

1. Fork the repository
2. Create feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request




## 🙏 Acknowledgments

- Microsoft for ASP.NET Core documentation
- Bootstrap team for responsive framework
- Entity Framework Core community
- Stack Overflow for problem-solving support



⭐ **If you find this project interesting, please consider giving it a star!**

**Status**: ✅ Fully Functional | 🚀 Production-Ready | 📚 Learning Project

*Built with ❤️ and 🌮 using ASP.NET Core*
