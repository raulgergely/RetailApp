using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace RetailApp
{
    public partial class Form1 : Form
    {
        private readonly InventoryRepository _repository;
        private bool isShowingHistory = false;
        private List<CartItem> currentCart = new List<CartItem>();

        public Form1(InventoryRepository repository)
        {
            InitializeComponent();
            _repository = repository;
            InitializeApp();
        }

        private void InitializeApp()
        {
            try
            {
                _repository.InitiDatabase();
                LoadingProducts();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LoadingProducts()
        {
            try
            {
                isShowingHistory = false;
                dataGridView1.DataSource = _repository.GetAllProducts();
                dataGridView1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
                
                btnRestock.Enabled = true;
                textBox1.Enabled = true;
                txtSearch.Enabled = true;
                btnHistory.Text = "Sale History";
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading data: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LoadHistoryToGrid()
        {
            try
            {
                isShowingHistory = true;
                dataGridView1.DataSource = _repository.GetSalesHistory();
                dataGridView1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
                
                //setting the width for each column in the history view
                if (dataGridView1.Columns["ID Sell"] != null) dataGridView1.Columns["ID Sell"].Width = 40;
                if (dataGridView1.Columns["Date"] != null) dataGridView1.Columns["Date"].Width = 150;
                if (dataGridView1.Columns["Product"] != null) dataGridView1.Columns["Product"].Width = 200;
                if (dataGridView1.Columns["Quantity"] != null) dataGridView1.Columns["Quantity"].Width = 60;
                if (dataGridView1.Columns["Price Unit (€)"] != null) dataGridView1.Columns["Price Unit (€)"].Width = 80;
                if (dataGridView1.Columns["Total (€)"] != null) dataGridView1.Columns["Total (€)"].Width = 101;
                
                btnRestock.Enabled = false;
                textBox1.Enabled = false;
                txtSearch.Enabled = false;
                btnHistory.Text = "Back to Stock";
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading history: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void txtSearch_TextChanged(object sender, EventArgs e)
        {
            if (!isShowingHistory)
            {
                try
                {
                    dataGridView1.DataSource = _repository.SearchProduct(txtSearch.Text);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error searching: " + ex.Message);
                }
            }
        }

        private void dataGridView1_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (!isShowingHistory && dataGridView1.Columns[e.ColumnIndex].Name == "Quantity")
            {
                if (e.Value is int)
                {
                    int stockValue = (int)e.Value;
                    DataGridViewRow row = dataGridView1.Rows[e.RowIndex];

                    if (stockValue < 10)
                    {
                        row.DefaultCellStyle.BackColor = System.Drawing.Color.FromArgb(255, 230, 230); // Red
                        row.DefaultCellStyle.ForeColor = System.Drawing.Color.FromArgb(180, 0, 0);
                    }
                    else
                    {
                        row.DefaultCellStyle.BackColor = System.Drawing.Color.White;
                        row.DefaultCellStyle.ForeColor = System.Drawing.Color.Black;
                    }
                }
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (dataGridView1.SelectedRows.Count == 0)
            {
                MessageBox.Show("Please select a product from the table!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int pId = Convert.ToInt32(dataGridView1.SelectedRows[0].Cells["Id"].Value);
            decimal price = Convert.ToDecimal(dataGridView1.SelectedRows[0].Cells["Price (€)"].Value);

            try
            {
                int quantityToSell = Convert.ToInt32(textBox1.Text);

                if (quantityToSell <= 0)
                {
                    MessageBox.Show("Please enter a positive whole number!", "Validation error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                _repository.ProcessSale(pId, quantityToSell, price);

                decimal total = price * quantityToSell;
                MessageBox.Show("Sale recorded successfully!\nTotal: " + total + " €", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);

                textBox1.Clear();
                txtSearch.Clear();
                LoadingProducts();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Invalid quantity or transaction error: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnRestock_Click(object sender, EventArgs e)
        {
            if (dataGridView1.SelectedRows.Count == 0)
            {
                MessageBox.Show("Please select a product from the table to restock!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int productId = Convert.ToInt32(dataGridView1.SelectedRows[0].Cells["Id"].Value);

            string productName = string.Empty;
            var cellValue = dataGridView1.SelectedRows[0].Cells["Product"].Value;
            if (cellValue != null)
            {
                productName = cellValue.ToString();
            }

            try
            {
                int quantityToAdd = Convert.ToInt32(textBox1.Text);

                if (quantityToAdd <= 0)
                {
                    MessageBox.Show("Please enter a positive whole number!", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                _repository.RestockProduct(productId, quantityToAdd);

                MessageBox.Show("Stock successfully added for product:\n" + productName + " (+" + quantityToAdd + " units)", "Success Restock", MessageBoxButtons.OK, MessageBoxIcon.Information);

                textBox1.Clear();
                txtSearch.Clear();
                LoadingProducts(); 
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error Restock", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnHistory_Click(object sender, EventArgs e)
        {
            if (!isShowingHistory)
            {
                LoadHistoryToGrid();
            }
            else
            {
                txtSearch.Clear();
                LoadingProducts();
            }
        }


        private void btnAddToCart_Click(object sender, EventArgs e)
        {
            if (dataGridView1.SelectedRows.Count == 0)
            {
                MessageBox.Show("Please select a product from the catalog!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var selectedRow = dataGridView1.SelectedRows[0];

            var idCell = selectedRow.Cells["Id"]?.Value;
            var nameCell = selectedRow.Cells["Product"]?.Value;
            var priceCell = selectedRow.Cells["Price (€)"]?.Value;

            if (idCell == null || nameCell == null || priceCell == null)
            {
                MessageBox.Show("The selected row contains empty or invalid data.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            int pId = Convert.ToInt32(idCell);
            string pName = nameCell.ToString() ?? string.Empty;
            decimal price = Convert.ToDecimal(priceCell);

            try
            {
                int qty = Convert.ToInt32(textBox1.Text);

                if (qty <= 0)
                {
                    MessageBox.Show("Please enter a positive whole number!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                CartItem existingItem = null;
                foreach (CartItem item in currentCart)
                {
                    if (item.ProductId == pId)
                    {
                        existingItem = item;
                        break;
                    }
                }

                if (existingItem != null)
                {
                    existingItem.Quantity += qty;
                }
                else
                {
                    CartItem newItem = new CartItem();
                    newItem.ProductId = pId;
                    newItem.ProductName = pName;
                    newItem.UnitPrice = price;
                    newItem.Quantity = qty;

                    currentCart.Add(newItem);
                }

                RefreshCartGrid();
                textBox1.Clear();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Invalid quantity format: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void RefreshCartGrid()
        {
            dataGridViewCart.DataSource = null;//reset list to refresh the grid
            dataGridViewCart.DataSource = currentCart;
            dataGridViewCart.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            if (dataGridViewCart.Columns["ProductId"] != null) {
                dataGridViewCart.Columns["ProductId"].Visible = false;
            }
        }

        private void btnCheckout_Click(object sender, EventArgs e)
        {
            if (currentCart == null || currentCart.Count == 0)
            {
                MessageBox.Show("The cart is empty!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                _repository.ProcessCartSale(currentCart);

                decimal totalSum = 0;
                foreach (CartItem item in currentCart)
                {
                    totalSum += item.TotalPrice;
                }

                MessageBox.Show("Sale successfully completed!\nGrand Total: " + totalSum + " €", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);

                currentCart.Clear();
                RefreshCartGrid();
                LoadingProducts();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Checkout Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private void btnRemove_Click(object sender, EventArgs e)
        {
            if (currentCart == null || currentCart.Count == 0)
            {
                MessageBox.Show("The cart is empty!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (dataGridViewCart.SelectedRows.Count == 0)
            {
                MessageBox.Show("Please select a product from the table!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            var selectedRow = dataGridViewCart.SelectedRows[0];
            if (selectedRow.DataBoundItem is CartItem)
            {
                CartItem itemToRemove = (CartItem)selectedRow.DataBoundItem;
                currentCart.Remove(itemToRemove);
                RefreshCartGrid();
            }
        }
    }
}