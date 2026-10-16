using System;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Extensions.Configuration;
using System.IO;

namespace RetailApp
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.ThreadException += new ThreadExceptionEventHandler(Application_ThreadException);
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);

            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            try
            {
                var builder = new ConfigurationBuilder()
                    .SetBasePath(AppContext.BaseDirectory)
                    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

                IConfiguration configuration = builder.Build();
                string connectionString = configuration.GetConnectionString("DefaultConnection")
                                          ?? @"Server=(localdb)\MSSQLLocalDB;Database=RetailInventoryDb;Trusted_Connection=True;";

                Application.Run(new Form1(new SqlServerInventoryRepository(connectionString)));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Critical error during application startup: {ex.Message}",
                                "Fatal Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static void Application_ThreadException(object sender, ThreadExceptionEventArgs e)
        {
            MessageBox.Show($"An unexpected error occurred in the application:\n\n{e.Exception.Message}",
                            "System Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Exception ex = e.ExceptionObject as Exception;
            MessageBox.Show($"A critical background error has affected the application:\n\n{ex?.Message}",
                            "Critical Error", MessageBoxButtons.OK, MessageBoxIcon.Stop);
        }
    }
}