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
        bool ProcessSale(int productId, int quantityToSell, decimal price, out string errorMessage);
        bool RestockProduct(int productId, int quantityToAdd, out string errorMessage);
        DataTable GetSalesHistory();
    }
}
