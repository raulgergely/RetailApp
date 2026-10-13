using System;
using System.Data;
using System.Data.SQLite;

namespace RetailApp
{
    public class SqliteInventoryRepository : InventoryRepository
    {
        private readonly string _connectionString = "Data Source=store.db;Version=3;";

        public void InitiDatabase()
        {
            try
            {
                if (System.IO.File.Exists("store.db"))
                {
                    System.IO.File.Delete("store.db");
                }

                using (SQLiteConnection conn = new SQLiteConnection(_connectionString))
                {
                    conn.Open();
                    string sql = @"
                        CREATE TABLE Products (
                            Id INTEGER PRIMARY KEY AUTOINCREMENT,
                            Name TEXT NOT NULL,
                            Price DECIMAL NOT NULL
                        );
                        CREATE TABLE Stock (
                            ProductId INTEGER,
                            Quantity INTEGER NOT NULL
                        );
                        CREATE TABLE Sales (
                            Id INTEGER PRIMARY KEY AUTOINCREMENT,
                            SaleDate DATETIME DEFAULT CURRENT_TIMESTAMP,
                            TotalAmount DECIMAL NOT NULL
                        );
                        CREATE TABLE SaleItems (
                            Id INTEGER PRIMARY KEY AUTOINCREMENT,
                            SaleId INTEGER,
                            ProductId INTEGER,
                            Quantity INTEGER NOT NULL,
                            UnitPrice DECIMAL NOT NULL
                        );
                    ";
                    using (SQLiteCommand cmd = new SQLiteCommand(sql, conn))
                    {
                        cmd.ExecuteNonQuery();
                    }
                    //I added data for test app in ui 
                    string seed = @"
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
                    ";
                    using (SQLiteCommand cmdSeed = new SQLiteCommand(seed, conn))
                    {
                        cmdSeed.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error creating database: " + ex.Message);
            }
        }
        /*
         * Returns the list of products and their current stock levels.
         */
        public DataTable GetAllProducts()
        {
            using (SQLiteConnection conn = new SQLiteConnection(_connectionString))
            {
                conn.Open();
                string query = "SELECT p.Id, p.Name AS [Product], p.Price AS [Price (€)], s.Quantity AS [Quantity] FROM Products p INNER JOIN Stock s ON p.Id = s.ProductId";
                using (SQLiteCommand sqcmd = new SQLiteCommand(query, conn))
                {
                    using (SQLiteDataAdapter adapt = new SQLiteDataAdapter(sqcmd))
                    {
                        DataTable data = new DataTable();
                        adapt.Fill(data);
                        return data;
                    }
                }
            }
        }
        /*
         * Searches for products by ID or name matching the product.
         */
        public DataTable SearchProduct(string search)
        {
            using (SQLiteConnection conn = new SQLiteConnection(_connectionString))
            {
                conn.Open();
                SQLiteCommand sqcmd = conn.CreateCommand();

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

                using (SQLiteDataAdapter adapt = new SQLiteDataAdapter(sqcmd))
                {
                    DataTable data = new DataTable();
                    adapt.Fill(data);
                    return data;
                }
            }
        }
        /*
         * Processes a sale, updates the stock levels, and records the transaction.
         */
        public bool ProcessSale(int productId, int quantityToSell, decimal price, out string errorMessage)
        {
            errorMessage = string.Empty;

            try
            {
                using (SQLiteConnection conn = new SQLiteConnection(_connectionString))
                {
                    conn.Open();
                    using (SQLiteTransaction transaction = conn.BeginTransaction())
                    {
                        using (SQLiteCommand sqcmd = conn.CreateCommand())
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
                            sqcmd.CommandText = "INSERT INTO Sales (TotalAmount) VALUES (@total); SELECT last_insert_rowid();";
                            sqcmd.Parameters.AddWithValue("@total", price * quantityToSell);
                            long saleId = (long)sqcmd.ExecuteScalar();

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
        /*
         * Adds quantity to an existing product's stock.
         */
        public bool RestockProduct(int productId, int quantityToAdd, out string errorMessage)
        {
            errorMessage = string.Empty;

            try
            {
                using (SQLiteConnection conn = new SQLiteConnection(_connectionString))
                {
                    conn.Open();
                    string query = "UPDATE Stock SET Quantity = Quantity + @qty WHERE ProductId = @pId";
                    using (SQLiteCommand sqcmd = new SQLiteCommand(query, conn))
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
        /*
         * Returns the sales history with item details.
         */
        public DataTable GetSalesHistory()
        {
            using (SQLiteConnection conn = new SQLiteConnection(_connectionString))
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
                using (SQLiteCommand sqcmd = new SQLiteCommand(query, conn))
                {
                    using (SQLiteDataAdapter adapt = new SQLiteDataAdapter(sqcmd))
                    {
                        DataTable data = new DataTable();
                        adapt.Fill(data);
                        return data;
                    }
                }
            }
        }
    }
}