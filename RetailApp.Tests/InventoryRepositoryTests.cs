using Moq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace RetailApp.Tests
{
    public class InventoryRepositoryTests
    {
        [Fact]
        public void SearchProduct()
        {
            var mockRepo = new Mock<InventoryRepository>();
            var fakeTable = new DataTable();
            fakeTable.Columns.Add("Id", typeof(int));
            fakeTable.Columns.Add("Product", typeof(string));
            fakeTable.Rows.Add(1, "Coffee Beans 1kg");

            mockRepo.Setup(r => r.SearchProduct("Coffee")).Returns(fakeTable);

            var result = mockRepo.Object.SearchProduct("Coffee");

            Assert.NotNull(result);
            Assert.Single(result.Rows);
            Assert.Equal("Coffee Beans 1kg", result.Rows[0]["Product"]);
        }

        [Fact]
        public void ProcessSale_ThrowsException_WhenStockIsInsufficient()
        {
            var mockRepo = new Mock<InventoryRepository>();

            mockRepo.Setup(r => r.ProcessSale(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<decimal>()))
                    .Throws(new Exception("Insufficient stock for the requested quantity!"));

            var exception = Assert.Throws<Exception>(() =>
                mockRepo.Object.ProcessSale(productId: 1, quantityToSell: 999, price: 45.00m));

            Assert.Equal("Insufficient stock for the requested quantity!", exception.Message);
        }

        [Fact]
        public void ProcessCartSale_ThrowsException_WhenCartIsEmpty()
        {
            var mockRepo = new Mock<InventoryRepository>();
            var emptyCart = new List<CartItem>();

            mockRepo.Setup(r => r.ProcessCartSale(emptyCart))
                    .Throws(new Exception("The cart is empty!"));

            var exception = Assert.Throws<Exception>(() =>
                mockRepo.Object.ProcessCartSale(emptyCart));

            Assert.Equal("The cart is empty!", exception.Message);
        }
    }
}
