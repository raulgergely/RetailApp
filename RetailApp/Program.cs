using System;
using System.Windows.Forms;
using Microsoft.Extensions.Configuration;
using System.IO;

namespace RetailApp
{
    static class Program
    {
        public static IConfiguration Configuration { get; private set; }

        [STAThread]
            static void Main()
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

            Configuration = builder.Build();

            string connectionString = Configuration.GetConnectionString("DefaultConnection");

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            Application.Run(new Form1(new SqlServerInventoryRepository(connectionString)));
        }
    }
}