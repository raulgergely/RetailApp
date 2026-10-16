using System;
using System.Data;
using Microsoft.Data.SqlClient;

namespace RetailApp
{
    public class SqlServerInventoryRepository : InventoryRepository
    {
        // Folosim LocalDB, care funcționează imediat pe orice PC cu Visual Studio instalat
        //private readonly string _connectionString = "Server=(localdb)\\MSSQLLocalDB;Database=master;Trusted_Connection=True;MultipleActiveResultSets=true";
        private readonly string _masterConnectionString = "Server=(localdb)\\MSSQLLocalDB;Database=master;Trusted_Connection=True;MultipleActiveResultSets=true";
        private readonly string _dbConnectionString = "Server=(localdb)\\MSSQLLocalDB;Database=RetailAppDb;Trusted_Connection=True;MultipleActiveResultSets=true";

        public void InitiDatabase()
        {
            try
            {
                // Pasul 1: Ne asigurăm că baza de date există
                EnsureDatabaseExists();

                // Pasul 2: Ne asigurăm că tabelele și datele inițiale există
                EnsureTablesAndSeedExist();
            }
            catch (Exception ex)
            {
                throw new Exception("Error initializing SQL Server database: " + ex.Message);
            }
        }

        private void EnsureDatabaseExists()
        {
            using (SqlConnection conn = new SqlConnection(_masterConnectionString))
            {
                conn.Open();
                string createDbQuery = @"
                    IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = 'RetailAppDb')
                    BEGIN
                        CREATE DATABASE RetailAppDb;
                    END
                ";
                using (SqlCommand cmd = new SqlCommand(createDbQuery, conn))
                {
                    cmd.ExecuteNonQuery();
                }
            }
        }

        private void EnsureTablesAndSeedExist()
        {
            using (SqlConnection conn = new SqlConnection(_dbConnectionString))
            {
                conn.Open();
                string checkTableQuery = @"
                    IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Products' and xtype='U')
                    BEGIN
                        -- Creăm tabelele doar dacă nu există
                        CREATE TABLE Products (
                            Id INT IDENTITY(1,1) PRIMARY KEY,
                            Name NVARCHAR(150) NOT NULL,
                            Price DECIMAL(18,2) NOT NULL
                        );
                        CREATE TABLE Stock (
                            ProductId INT,
                            Quantity INT NOT NULL
                        );
                        CREATE TABLE Sales (
                            Id INT IDENTITY(1,1) PRIMARY KEY,
                            SaleDate DATETIME DEFAULT GETDATE(),
                            TotalAmount DECIMAL(18,2) NOT NULL
                        );
                        CREATE TABLE SaleItems (
                            Id INT IDENTITY(1,1) PRIMARY KEY,
                            SaleId INT,
                            ProductId INT,
                            Quantity INT NOT NULL,
                            UnitPrice DECIMAL(18,2) NOT NULL
                        );

                        -- Adăugăm datele de test DOAR la prima creare a bazei de date
                        INSERT INTO Products (Name, Price) VALUES ('Coffee Beans 1kg', 45.00); 
                        INSERT INTO Products (Name, Price) VALUES ('Artisanal Chocolate', 15.50); 
                        INSERT INTO Products (Name, Price) VALUES ('Organic Green Tea', 22.00); 
                        INSERT INTO Products (Name, Price) VALUES ('Natural Honey 500g', 28.00); 
                        INSERT INTO Products (Name, Price) VALUES ('Roasted Peanuts 200g', 8.50); 
                        INSERT INTO Products (Name, Price) VALUES ('Sparkling Mineral Water', 5.00);

                        INSERT INTO Stock (ProductId, Quantity) VALUES (1, 30);
                        INSERT INTO Stock (ProductId, Quantity) VALUES (2, 80);
                        INSERT INTO Stock (ProductId, Quantity) VALUES (3, 50);
                        INSERT INTO Stock (ProductId, Quantity) VALUES (4, 25);
                        INSERT INTO Stock (ProductId, Quantity) VALUES (5, 100);
                        INSERT INTO Stock (ProductId, Quantity) VALUES (6, 160);
                    END
                ";

                using (SqlCommand cmdInit = new SqlCommand(checkTableQuery, conn))
                {
                    cmdInit.ExecuteNonQuery();
                }
            }
        }

        public DataTable GetAllProducts()
        {
            using (SqlConnection conn = new SqlConnection(_dbConnectionString))
            {
                conn.Open();
                string query = "SELECT p.Id, p.Name AS [Product], p.Price AS [Price (€)], s.Quantity AS [Quantity] FROM Products p INNER JOIN Stock s ON p.Id = s.ProductId";
                using (SqlCommand sqcmd = new SqlCommand(query, conn))
                {
                    using (SqlDataAdapter adapt = new SqlDataAdapter(sqcmd))
                    {
                        DataTable data = new DataTable();
                        adapt.Fill(data);
                        return data;
                    }
                }
            }
        }

        public DataTable SearchProduct(string search)
        {
            using (SqlConnection conn = new SqlConnection(_dbConnectionString))
            {
                conn.Open();
                SqlCommand sqcmd = conn.CreateCommand();

                if (int.TryParse(search, out int searchId))
                {
                    sqcmd.CommandText = @"
                        SELECT p.Id, p.Name AS [Product], p.Price AS [Price (€)], s.Quantity AS [Quantity] 
                        FROM Products p 
                        INNER JOIN Stock s ON p.Id = s.ProductId 
                        WHERE p.Id = @id OR p.Name LIKE @search";
                    sqcmd.Parameters.AddWithValue("@id", searchId);
                    sqcmd.Parameters.AddWithValue("@search", "%" + search + "%");
                }
                else
                {
                    sqcmd.CommandText = @"
                        SELECT p.Id, p.Name AS [Product], p.Price AS [Price (€)], s.Quantity AS [Quantity] 
                        FROM Products p 
                        INNER JOIN Stock s ON p.Id = s.ProductId 
                        WHERE p.Name LIKE @search";
                    sqcmd.Parameters.AddWithValue("@search", "%" + search + "%");
                }

                using (SqlDataAdapter adapt = new SqlDataAdapter(sqcmd))
                {
                    DataTable data = new DataTable();
                    adapt.Fill(data);
                    return data;
                }
            }
        }

        public bool ProcessSale(int productId, int quantityToSell, decimal price, out string errorMessage)
        {
            errorMessage = string.Empty;

            try
            {
                using (SqlConnection conn = new SqlConnection(_dbConnectionString))
                {
                    conn.Open();
                    using (SqlTransaction transaction = conn.BeginTransaction())
                    {
                        using (SqlCommand sqcmd = conn.CreateCommand())
                        {
                            sqcmd.Transaction = transaction;

                            sqcmd.CommandText = "UPDATE Stock SET Quantity = Quantity - @qty WHERE ProductId = @pId AND Quantity >= @qty";
                            sqcmd.Parameters.AddWithValue("@qty", quantityToSell);
                            sqcmd.Parameters.AddWithValue("@pId", productId);

                            int rowsAffected = sqcmd.ExecuteNonQuery();

                            if (rowsAffected == 0)
                            {
                                errorMessage = "Insufficient stock for the requested quantity!";
                                transaction.Rollback();
                                return false;
                            }

                            sqcmd.Parameters.Clear();
                            // În SQL Server, ultimul ID generat se preia cu SCOPE_IDENTITY()
                            sqcmd.CommandText = "INSERT INTO Sales (TotalAmount) VALUES (@total); SELECT SCOPE_IDENTITY();";
                            sqcmd.Parameters.AddWithValue("@total", price * quantityToSell);
                            long saleId = Convert.ToInt64(sqcmd.ExecuteScalar());

                            sqcmd.Parameters.Clear();
                            sqcmd.CommandText = "INSERT INTO SaleItems (SaleId, ProductId, Quantity, UnitPrice) VALUES (@sId, @pId, @qty, @price)";
                            sqcmd.Parameters.AddWithValue("@sId", saleId);
                            sqcmd.Parameters.AddWithValue("@pId", productId);
                            sqcmd.Parameters.AddWithValue("@qty", quantityToSell);
                            sqcmd.Parameters.AddWithValue("@price", price);
                            sqcmd.ExecuteNonQuery();

                            transaction.Commit();
                            return true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                errorMessage = "Critical transaction error: " + ex.Message;
                return false;
            }
        }

        public bool RestockProduct(int productId, int quantityToAdd, out string errorMessage)
        {
            errorMessage = string.Empty;

            try
            {
                using (SqlConnection conn = new SqlConnection(_dbConnectionString))
                {
                    conn.Open();
                    string query = "UPDATE Stock SET Quantity = Quantity + @qty WHERE ProductId = @pId";
                    using (SqlCommand sqcmd = new SqlCommand(query, conn))
                    {
                        sqcmd.Parameters.AddWithValue("@qty", quantityToAdd);
                        sqcmd.Parameters.AddWithValue("@pId", productId);

                        int rowsAffected = sqcmd.ExecuteNonQuery();
                        if (rowsAffected > 0)
                        {
                            return true;
                        }
                        else
                        {
                            errorMessage = "The selected product was not found in the inventory.";
                            return false;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                errorMessage = "Restock error: " + ex.Message;
                return false;
            }
        }

        public DataTable GetSalesHistory()
        {
            using (SqlConnection conn = new SqlConnection(_dbConnectionString))
            {
                conn.Open();
                string query = @"
                    SELECT s.Id AS [ID Sell], 
                           s.SaleDate AS [Date], 
                           p.Name AS [Product], 
                           si.Quantity AS [Quantity], 
                           si.UnitPrice AS [Price Unit (€)], 
                           s.TotalAmount AS [Total (€)]
                    FROM Sales s
                    INNER JOIN SaleItems si ON s.Id = si.SaleId
                    INNER JOIN Products p ON si.ProductId = p.Id
                    ORDER BY s.SaleDate DESC;
                ";
                using (SqlCommand sqcmd = new SqlCommand(query, conn))
                {
                    using (SqlDataAdapter adapt = new SqlDataAdapter(sqcmd))
                    {
                        DataTable data = new DataTable();
                        adapt.Fill(data);
                        return data;
                    }
                }
            }
        }
        public bool ProcessCartSale(List<CartItem> cartItems, out string errorMessage)
        {
            errorMessage = string.Empty;

            if (cartItems == null || cartItems.Count == 0)
            {
                errorMessage = "The cart is empty!";
                return false;
            }

            try
            {
                using (SqlConnection conn = new SqlConnection(_dbConnectionString))
                {
                    conn.Open();
                    using (SqlTransaction transaction = conn.BeginTransaction())
                    {
                        using (SqlCommand sqcmd = conn.CreateCommand())
                        {
                            sqcmd.Transaction = transaction;

                            // 1. Verificăm și scădem stocul pentru fiecare produs din coș
                            foreach (var item in cartItems)
                            {
                                sqcmd.CommandText = "UPDATE Stock SET Quantity = Quantity - @qty WHERE ProductId = @pId AND Quantity >= @qty";
                                sqcmd.Parameters.Clear();
                                sqcmd.Parameters.AddWithValue("@qty", item.Quantity);
                                sqcmd.Parameters.AddWithValue("@pId", item.ProductId);

                                int rowsAffected = sqcmd.ExecuteNonQuery();
                                if (rowsAffected == 0)
                                {
                                    errorMessage = $"Insufficient stock for product: {item.ProductName}";
                                    transaction.Rollback();
                                    return false;
                                }
                            }

                            // 2. Calculăm totalul general al bonului
                            decimal grandTotal = 0;
                            foreach (var item in cartItems)
                            {
                                grandTotal += item.TotalPrice;
                            }

                            // 3. Creăm antetul vânzării în tabela Sales
                            sqcmd.Parameters.Clear();
                            sqcmd.CommandText = "INSERT INTO Sales (TotalAmount) VALUES (@total); SELECT SCOPE_IDENTITY();";
                            sqcmd.Parameters.AddWithValue("@total", grandTotal);
                            long saleId = Convert.ToInt64(sqcmd.ExecuteScalar());

                            // 4. Salvăm detaliile bonului în SaleItems
                            foreach (var item in cartItems)
                            {
                                sqcmd.Parameters.Clear();
                                sqcmd.CommandText = "INSERT INTO SaleItems (SaleId, ProductId, Quantity, UnitPrice) VALUES (@sId, @pId, @qty, @price)";
                                sqcmd.Parameters.AddWithValue("@sId", saleId);
                                sqcmd.Parameters.AddWithValue("@pId", item.ProductId);
                                sqcmd.Parameters.AddWithValue("@qty", item.Quantity);
                                sqcmd.Parameters.AddWithValue("@price", item.UnitPrice);
                                sqcmd.ExecuteNonQuery();
                            }

                            transaction.Commit();
                            return true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                errorMessage = "Cart transaction error: " + ex.Message;
                return false;
            }
        }
    }
}