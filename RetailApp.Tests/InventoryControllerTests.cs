using System;
using System.Data;
using Xunit;
using RetailApp;

namespace RetailApp.Tests
{
    public class InventoryControllerTests
    {
        [Fact]
        public void GetProducts_ShouldReturnProductsList()
        {
            var repository = new SqlServerInventoryRepository();
            DataTable products = repository.GetAllProducts();

            Assert.NotNull(products);
            Assert.True(products.Rows.Count > 0, "The database does not contain any products.");
        }

        [Fact]
        public void ProcessSale_ShouldReduceStockAndSucceed()
        {
            var repository = new SqlServerInventoryRepository();

            DataTable products = repository.GetAllProducts();
            Assert.True(products.Rows.Count > 0, "At least one product is required in the DB for this test.");

            int productId = Convert.ToInt32(products.Rows[0]["Id"]);
            decimal price = Convert.ToDecimal(products.Rows[0]["Price (€)"]);
            int initialStock = Convert.ToInt32(products.Rows[0]["Quantity"]);

            if (initialStock < 5)
            {
                repository.RestockProduct(productId, 10, out _);
                initialStock = Convert.ToInt32(repository.GetAllProducts().Rows[0]["Quantity"]);
            }

            int quantityToSell = 2;

            bool success = repository.ProcessSale(productId, quantityToSell, price, out string errorMessage);

            Assert.True(success, $"Sale failed: {errorMessage}");

            DataTable updatedProducts = repository.GetAllProducts();
            int newStock = 0;
            foreach (DataRow row in updatedProducts.Rows)
            {
                if (Convert.ToInt32(row["Id"]) == productId)
                {
                    newStock = Convert.ToInt32(row["Quantity"]);
                    break;
                }
            }

            Assert.Equal(initialStock - quantityToSell, newStock);
        }

        [Fact]
        public void ProcessSale_ShouldFailWhenStockIsInsufficient()
        {
            var repository = new SqlServerInventoryRepository();
            DataTable products = repository.GetAllProducts();
            Assert.True(products.Rows.Count > 0);

            int productId = Convert.ToInt32(products.Rows[0]["Id"]);
            decimal price = Convert.ToDecimal(products.Rows[0]["Price (€)"]);
            int excessiveQuantity = 99999;

            bool success = repository.ProcessSale(productId, excessiveQuantity, price, out string errorMessage);

            Assert.False(success, "The sale should have failed due to insufficient stock.");
            Assert.False(string.IsNullOrEmpty(errorMessage), "An error message should have been returned.");
        }

        [Fact]
        public void RestockProduct_ShouldIncreaseStockSuccessfully()
        {
            var repository = new SqlServerInventoryRepository();
            DataTable products = repository.GetAllProducts();
            Assert.True(products.Rows.Count > 0);

            int productId = Convert.ToInt32(products.Rows[0]["Id"]);
            int initialStock = Convert.ToInt32(products.Rows[0]["Quantity"]);
            int quantityToAdd = 5;

            bool success = repository.RestockProduct(productId, quantityToAdd, out string errorMessage);

            Assert.True(success, $"Restock failed: {errorMessage}");

            DataTable updatedProducts = repository.GetAllProducts();
            int newStock = 0;
            foreach (DataRow row in updatedProducts.Rows)
            {
                if (Convert.ToInt32(row["Id"]) == productId)
                {
                    newStock = Convert.ToInt32(row["Quantity"]);
                    break;
                }
            }

            Assert.Equal(initialStock + quantityToAdd, newStock);
        }

        [Fact]
        public void SearchProduct_ShouldReturnMatchingResults()
        {
            var repository = new SqlServerInventoryRepository();

            string searchTerm = "Coffee";

            DataTable searchResult = repository.SearchProduct(searchTerm);

            Assert.NotNull(searchResult);
            Assert.True(searchResult.Rows.Count > 0, "Search should have found matching products.");

            string productName = searchResult.Rows[0]["Product"].ToString();
            Assert.Contains(searchTerm, productName, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void GetSalesHistory_ShouldContainRecordedSales()
        {
            var repository = new SqlServerInventoryRepository();
            DataTable products = repository.GetAllProducts();
            Assert.True(products.Rows.Count > 0);

            int productId = Convert.ToInt32(products.Rows[0]["Id"]);
            decimal price = Convert.ToDecimal(products.Rows[0]["Price (€)"]);

            repository.RestockProduct(productId, 5, out _);
            bool saleSuccess = repository.ProcessSale(productId, 1, price, out _);
            Assert.True(saleSuccess, "Prerequisite sale failed.");

            DataTable history = repository.GetSalesHistory();

            Assert.NotNull(history);
            Assert.True(history.Rows.Count > 0, "Sales history should contain at least one record.");
        }
        [Fact]
        public void ProcessCartSale_ShouldProcessMultipleItemsAndReduceStock()
        {
            var repository = new SqlServerInventoryRepository();
            DataTable products = repository.GetAllProducts();
            Assert.True(products.Rows.Count >= 2, "At least two products are required in the DB for this test.");

            int pId1 = Convert.ToInt32(products.Rows[0]["Id"]);
            decimal price1 = Convert.ToDecimal(products.Rows[0]["Price (€)"]);
            string name1 = products.Rows[0]["Product"].ToString() ?? "Prod 1";

            int pId2 = Convert.ToInt32(products.Rows[1]["Id"]);
            decimal price2 = Convert.ToDecimal(products.Rows[1]["Price (€)"]);
            string name2 = products.Rows[1]["Product"].ToString() ?? "Prod 2";

            repository.RestockProduct(pId1, 5, out _);
            repository.RestockProduct(pId2, 5, out _);

            var cartItems = new List<CartItem>
            {
                new CartItem { ProductId = pId1, ProductName = name1, UnitPrice = price1, Quantity = 2 },
                new CartItem { ProductId = pId2, ProductName = name2, UnitPrice = price2, Quantity = 1 }
            };

            bool success = repository.ProcessCartSale(cartItems, out string errorMessage);

            Assert.True(success, $"Cart sale failed: {errorMessage}");

            DataTable history = repository.GetSalesHistory();
            Assert.NotNull(history);
            Assert.True(history.Rows.Count > 0, "Sales history should contain the recorded cart sale.");
        }
    }
}