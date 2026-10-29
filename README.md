# RetailApp - Windows Forms POS & Inventory Management System

A professional desktop Point of Sale (POS) and inventory management system built with **C# (.NET)** and **SQL Server**, designed with enterprise-level architecture, robust error handling, and automated unit tests.

---

## 🚀 Key Features

* **Multi-Item Cart System:** Add multiple products to a dynamic cart, manage quantities, remove items, and calculate grand totals seamlessly.
* **Inventory Control & Real-Time Search:** Real-time stock tracking with dynamic low-stock visual alerts (highlighting products with low quantities) and instant product searching.
* **Sales History & Tracking:** Complete logging of past sales and transaction details.
* **Atomic Database Transactions:** Built-in transaction management (`SqlTransaction`) with automatic rollback support to ensure data integrity during checkouts and stock updates.
* **Automated Unit Testing:** Comprehensive unit testing suite implemented using **xUnit** to validate business logic and repository operations.

---

## 🏛️ Architecture & Engineering Highlights

This project follows clean code principles and industry best practices:
* **Repository Pattern:** Decouples data access logic from the user interface via structured repository interfaces (`IInventoryRepository`).
* **Configuration Management:** Externalized settings (such as database connection strings) managed via `appsettings.json` using `Microsoft.Extensions.Configuration`.
* **Global Exception Handling:** Centralized exception catching for both UI threads and background operations in `Program.cs`, ensuring graceful error reporting.
* **Zero-Config LocalDB Support:** Automatically detects, provisions, and seeds the database (`RetailInventoryDb`) on startup.

---

## 🛠️ Tech Stack

* **Language:** C# (.NET)
* **UI Framework:** Windows Forms (WinForms)
* **Database:** Microsoft SQL Server / LocalDB (`Microsoft.Data.SqlClient`)
* **Testing:** xUnit
* **Configuration:** Microsoft.Extensions.Configuration.Json

---

## 📂 Project Structure

```text
RetailApp/
│
├── Program.cs                  # Application entry point, configuration & global exception handling
├── Form1.cs                    # Main UI form (POS catalog, cart, and history management)
├── CartItem.cs                 # Domain model for cart items
├── InventoryRepository.cs      # Base repository contract/class
├── SqlServerInventoryRepository.cs # SQL Server implementation with atomic transactions
│
└── RetailApp.Tests/            # xUnit automated test suite
    └── InventoryControllerTests.cs
