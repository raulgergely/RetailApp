using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;

namespace RetailApp
{
    public class SqlServerInventoryRepository : InventoryRepository
    {
        private readonly string _connectionString;

        public SqlServerInventoryRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public SqlServerInventoryRepository()
            : this("Server=(localdb)\\MSSQLLocalDB;Database=RetailInventoryDb;Trusted_Connection=True;")
        {
        }

        public void InitiDatabase()
        {
            try
            {
                EnsureDatabaseExists();
                EnsureTablesAndSeedExist();
            }
            catch (Exception ex)
            {
                throw new Exception("Error initializing SQL Server database: " + ex.Message);
            }
        }

        private void EnsureDatabaseExists()
        {
            var builder = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(_connectionString);
            string targetDatabase = builder.InitialCatalog;
            builder.InitialCatalog = "master";

            using (SqlConnection conn = new SqlConnection(builder.ConnectionString))
            {
                conn.Open();

                string createDbQuery = "IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = '" + targetDatabase + "') " +
                                       "    CREATE DATABASE [" + targetDatabase + "]; ";

                using (SqlCommand cmd = new SqlCommand(createDbQuery, conn))
                {
                    cmd.ExecuteNonQuery();
                }
            }
        }

        private void EnsureTablesAndSeedExist()
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                string checkTableQuery = @"
                    IF NOT EXISTS (SELECT * FROM sys.tables WHERE name='Products')
                    BEGIN
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
            using (SqlConnection conn = new SqlConnection(_connectionString))
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
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                bool isNumeric = int.TryParse(search, out int productId);

                string query = isNumeric
                    ? @"SELECT p.Id, p.Name AS [Product], p.Price AS [Price (€)], s.Quantity AS [Quantity] 
                        FROM Products p 
                        INNER JOIN Stock s ON p.Id = s.ProductId 
                        WHERE p.Id = @productId OR p.Name LIKE @search"
                    : @"SELECT p.Id, p.Name AS [Product], p.Price AS [Price (€)], s.Quantity AS [Quantity] 
                        FROM Products p 
                        INNER JOIN Stock s ON p.Id = s.ProductId 
                        WHERE p.Name LIKE @search";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    if (isNumeric)
                    {
                        cmd.Parameters.AddWithValue("@productId", productId);
                    }

                    cmd.Parameters.AddWithValue("@search", "%" + search + "%");

                    using (SqlDataAdapter adapt = new SqlDataAdapter(cmd))
                    {
                        DataTable data = new DataTable();
                        adapt.Fill(data);
                        return data;
                    }
                } 
            }
        }

        public void ProcessSale(int productId, int quantityToSell, decimal price)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
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
                            transaction.Rollback();
                            throw new Exception("Insufficient stock for the requested quantity!");
                        }

                        sqcmd.Parameters.Clear();
                        sqcmd.CommandText = "INSERT INTO Sales (TotalAmount) VALUES (@total); SELECT SCOPE_IDENTITY();";//get id of the inserted sale
                        sqcmd.Parameters.AddWithValue("@total", price * quantityToSell);
                        long saleId = Convert.ToInt64(sqcmd.ExecuteScalar());//use id of the inserted sale to insert into SaleItems

                        sqcmd.Parameters.Clear();
                        sqcmd.CommandText = "INSERT INTO SaleItems (SaleId, ProductId, Quantity, UnitPrice) VALUES (@sId, @pId, @qty, @price)";
                        sqcmd.Parameters.AddWithValue("@sId", saleId);
                        sqcmd.Parameters.AddWithValue("@pId", productId);
                        sqcmd.Parameters.AddWithValue("@qty", quantityToSell);
                        sqcmd.Parameters.AddWithValue("@price", price);
                        sqcmd.ExecuteNonQuery();

                        transaction.Commit();
                    }
                }
            }
        }
        public void RestockProduct(int productId, int quantityToAdd)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                string query = "UPDATE Stock SET Quantity = Quantity + @qty WHERE ProductId = @pId";
                using (SqlCommand sqcmd = new SqlCommand(query, conn))
                {
                    sqcmd.Parameters.AddWithValue("@qty", quantityToAdd);
                    sqcmd.Parameters.AddWithValue("@pId", productId);

                    int rowsAffected = sqcmd.ExecuteNonQuery();
                    if (rowsAffected == 0)
                    {
                        throw new Exception("Product not found or failed to restock!");
                    }
                }
            }
        }

        public DataTable GetSalesHistory()
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
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

        public void ProcessCartSale(List<CartItem> cartItems)
        {
            if (cartItems == null || cartItems.Count == 0)
            {
                throw new Exception("The cart is empty!");
            }

            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                using (SqlTransaction transaction = conn.BeginTransaction())
                {
                    using (SqlCommand sqcmd = conn.CreateCommand())
                    {
                        sqcmd.Transaction = transaction;

                        try
                        {
                            foreach (CartItem item in cartItems)
                            {
                                sqcmd.CommandText = "UPDATE Stock SET Quantity = Quantity - @qty WHERE ProductId = @pId AND Quantity >= @qty";
                                sqcmd.Parameters.Clear();
                                sqcmd.Parameters.AddWithValue("@qty", item.Quantity);
                                sqcmd.Parameters.AddWithValue("@pId", item.ProductId);

                                int rowsAffected = sqcmd.ExecuteNonQuery();
                                if (rowsAffected == 0)
                                {
                                    throw new Exception("Insufficient stock for product: " + item.ProductName);
                                }
                            }

                            decimal grandTotal = 0;
                            foreach (CartItem item in cartItems)
                            {
                                grandTotal += item.TotalPrice;
                            }

                            sqcmd.Parameters.Clear();
                            sqcmd.CommandText = "INSERT INTO Sales (TotalAmount) VALUES (@total); SELECT SCOPE_IDENTITY();";
                            sqcmd.Parameters.AddWithValue("@total", grandTotal);
                            long saleId = Convert.ToInt64(sqcmd.ExecuteScalar());

                            foreach (CartItem item in cartItems)
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
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            throw new Exception("Cart transaction error: " + ex.Message);
                        }
                    }
                }
            }
        }
    }
}