using System;
using System.Windows.Forms;

namespace RetailApp
{
    public partial class Form1 : Form
    {
        private readonly InventoryRepository _repository;
        private bool isShowingHistory = false;

        public Form1(InventoryRepository repository)
        {
            InitializeComponent();
            _repository = repository;
            InitializeApp();
        }

        public Form1() : this(new SqliteInventoryRepository())
        {
        }

        private void InitializeApp()
        {
            try
            {
                _repository.InitiDatabase();
                LoadProductsToGrid();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Eroare", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LoadProductsToGrid()
        {
            try
            {
                isShowingHistory = false;
                dataGridView1.DataSource = _repository.GetAllProducts();
                dataGridView1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
                button1.Enabled = true;
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
                dataGridView1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
                button1.Enabled = false;
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
                if (e.Value != null && int.TryParse(e.Value.ToString(), out int stockValue))
                {
                    DataGridViewRow row = dataGridView1.Rows[e.RowIndex];

                    if (stockValue < 10)
                    {
                        row.DefaultCellStyle.BackColor = System.Drawing.Color.FromArgb(255, 230, 230); //red
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

            if (!int.TryParse(textBox1.Text, out int quantityToSell) || quantityToSell <= 0)
            {
                MessageBox.Show("Please enter a valid quantity (positive whole number)!", "Validation error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            bool success = _repository.ProcessSale(pId, quantityToSell, price, out string errorMessage);

            if (success)
            {
                MessageBox.Show($"Sale recorded successfully!\nTotal:: {price * quantityToSell} €", "Succes", MessageBoxButtons.OK, MessageBoxIcon.Information);
                textBox1.Clear();
                txtSearch.Clear();
                LoadProductsToGrid();
            }
            else
            {
                MessageBox.Show(errorMessage, "Transaction Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
            string productName = dataGridView1.SelectedRows[0].Cells["Product"].Value.ToString();

            if (!int.TryParse(textBox1.Text, out int quantityToAdd) || quantityToAdd <= 0)
            {
                MessageBox.Show("Please enter a valid quantity to restock!", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            bool success = _repository.RestockProduct(productId, quantityToAdd, out string errorMessage);

            if (success)
            {
                MessageBox.Show($"Stock successfully added for product:\n{productName} (+{quantityToAdd} units)", "Succes Restock", MessageBoxButtons.OK, MessageBoxIcon.Information);
                textBox1.Clear();
                txtSearch.Clear();
                LoadProductsToGrid();
            }
            else
            {
                MessageBox.Show(errorMessage, "Error Restock", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                LoadProductsToGrid();
            }
        }
    }
}