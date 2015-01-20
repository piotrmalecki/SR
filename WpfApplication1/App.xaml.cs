using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace WpfApplication1
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        
        void App_Startup(object sender, StartupEventArgs e)
        {
            MainWindow mainWindow = new MainWindow();
            

            //mainWindow2.name = "Ada";
            mainWindow.name = "Jozef";
            //mainWindow2.port = 4511;
            mainWindow.port = 4511;
            // Application is running
            // Process command line args
            for (int i = 0; i != e.Args.Length; ++i)
            {
                if(e.Args[0] != null)
                mainWindow.port = Convert.ToInt32(e.Args[0]);
                if (e.Args[1] != null)
                { mainWindow.name = e.Args[1];
                mainWindow.Title = mainWindow.name;
                }
                
            }

            // Create main application window, starting minimized if specified
            mainWindow.Show();
            //mainWindow2.Show();
        }
    }
}
