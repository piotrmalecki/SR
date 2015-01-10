using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace ServerSocketWpfApp
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        void App_Startup(object sender, StartupEventArgs e)
        {
            MainWindow mainWindow = new MainWindow();
            // Application is running
            // Process command line args
            var configvalue1 = new List<string>(ConfigurationManager.AppSettings["clientPorts"].Split(new char[] { ';' }));
            foreach (var item in configvalue1)
            {
                mainWindow.portstClient.Add(Convert.ToInt32(item));
            }
            // Create main application window, starting minimized if specified
            mainWindow.Show();
        }
    }
}
