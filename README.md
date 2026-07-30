# Inventory Management System

## 📖 Project Overview
The Inventory Management System is a robust web application built with ASP.NET Core MVC. It allows users to track product catalogs, monitor current stock levels, and record stock movements (In/Out) with strict business rules to prevent data anomalies. This project demonstrates foundational concepts of modern C# web development, Dependency Injection, Entity Framework Core, and clean UI design.

## ✨ Features
- **Product Management**: Create, read, update, and soft-delete products (SKU, Name, Description, Reorder Level).
- **Stock Movements**: Record incoming (restock) and outgoing (dispatch) stock movements.
- **Real-Time Stock Calculation**: Current stock is dynamically computed as `Total IN - Total OUT`.
- **Intelligent Validations**: Prevents stock from falling below zero. Alerts when stock reaches the reorder threshold.
- **Search Capabilities**: Instantly search for products by SKU or Name.
- **Beautiful UI**: Built with a clean, responsive, and dynamic UI using Bootstrap 5, featuring form validation, interactive toggles, and unified "Add/Edit" forms.

## 🏗️ Architecture
The application follows a clean **N-Tier Architecture**:
1. **Presentation Layer (Controllers & Views)**: Responsible for routing HTTP requests, rendering HTML, and model binding. ViewModels are heavily utilized to decouple the database schema from the UI.
2. **Business Logic Layer (Services)**: The core of the application. `ProductService` and `StockMovementService` handle all business rules, calculations, and validations (e.g., checking for duplicate SKUs, ensuring stock doesn't go negative).
3. **Data Access Layer (EF Core & Models)**: Responsible for interacting with the SQL database. Uses `ApplicationDbContext` and strongly-typed domain models.

## 🛠️ Technologies
- **Backend**: C# 10+, ASP.NET Core MVC 
- **ORM / Database**: Entity Framework Core, SQL Server LocalDB
- **Frontend**: HTML5, Bootstrap 5, jQuery (for unobtrusive validation), Bootstrap Icons
- **Design Pattern**: Dependency Injection (DI), Repository/Service Pattern

## 🚀 How to Run

1. **Clone the repository**:
   ```bash
   git clone https://github.com/Haroon62/InventoryManagement.git
   cd InventoryManagement
   ```

2. **Restore dependencies**:
   ```bash
   dotnet restore
   ```

3. **Build the project**:
   ```bash
   dotnet build
   ```

4. **Run the application**:
   ```bash
   dotnet run --project InventoryManagement
   ```
   The application will be accessible at `http://localhost:7273`.

## 🗄️ Database Setup

The project uses SQL Server LocalDB, which is included with Visual Studio. The connection string is pre-configured in `appsettings.json`:
```json
"ConnectionStrings": {
  "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=InventoryDb;Trusted_Connection=True;MultipleActiveResultSets=true"
}
```

### 📜 Migration Commands
If you need to update the database schema or set it up for the first time, open the Package Manager Console or use the .NET CLI:

**To create the initial database:**
```bash
dotnet ef database update --project InventoryManagement
```

**If you make changes to the Models and need to create a new migration:**
```bash
dotnet ef migrations add <MigrationName> --project InventoryManagement
dotnet ef database update --project InventoryManagement
```

## ⚖️ Business Rules
- **SKU Uniqueness**: No two products can share the same SKU. The `ProductService` verifies uniqueness during creation and updates.
- **Stock Integrity (The Key Rule)**: Stock levels cannot be manually edited. They are strictly calculated based on the history of Stock Movements.
- **Zero-Floor Policy**: An `Out` movement is immediately rejected by the `StockMovementService` if the requested quantity exceeds the currently available stock.
- **Soft Deletion**: Products are never hard-deleted from the database (to preserve movement history). Instead, `IsActive` is set to `false`, hiding them from the UI.
