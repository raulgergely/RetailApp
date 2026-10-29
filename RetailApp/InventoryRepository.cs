using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RetailApp
{
    public interface InventoryRepository
    {
        void InitiDatabase();
        DataTable GetAllProducts();
        DataTable SearchProduct(string searchItem);
        void ProcessSale(int productId, int quantityToSell, decimal price);
        void RestockProduct(int productId, int quantityToAdd);
        DataTable GetSalesHistory();
        void ProcessCartSale(List<CartItem> cartItems);
    }
}
