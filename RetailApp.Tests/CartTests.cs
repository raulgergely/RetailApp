using System.Collections.Generic;
using Xunit;
using RetailApp;

namespace RetailApp.Tests
{
    public class CartTests
    {
        [Fact]
        public void CalculatesTotalPrice()
        {
            var item = new CartItem
            {
                ProductId = 1,
                ProductName = "Coffee",
                UnitPrice = 10.0m,
                Quantity = 3
            };

            decimal total = item.TotalPrice;

            Assert.Equal(30.0m, total);
        }

        [Fact]
        public void CanAddMultipleItems()
        {
            var cart = new List<CartItem>();

            cart.Add(new CartItem { ProductId = 1, ProductName = "Coffee", UnitPrice = 10m, Quantity = 2 });
            cart.Add(new CartItem { ProductId = 2, ProductName = "Tea", UnitPrice = 5m, Quantity = 4 });

            Assert.Equal(2, cart.Count);
            Assert.Equal(20m, cart[0].TotalPrice);
            Assert.Equal(20m, cart[1].TotalPrice);
        }

        [Fact]
        public void CanRemoveItem()
        {
            var itemToRemove = new CartItem { ProductId = 1, ProductName = "Coffee", UnitPrice = 10m, Quantity = 2 };
            var cart = new List<CartItem> { itemToRemove };

            cart.Remove(itemToRemove);

            Assert.Empty(cart);
        }

        [Fact]
        public void CalculateCartTotal()
        {
            var cart = new List<CartItem>
            {
                new CartItem { UnitPrice = 10.0m, Quantity = 2 },
                new CartItem { UnitPrice = 15.0m, Quantity = 3 }
            };

            decimal grandTotal = 0;
            foreach (var item in cart)
            {
                grandTotal += item.TotalPrice;
            }

            Assert.Equal(65.0m, grandTotal);
        }
    }
}