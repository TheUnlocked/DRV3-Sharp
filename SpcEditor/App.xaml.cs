using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace SpcEditor
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            Window window;
            if (e.Args.Length > 0 && File.Exists(e.Args[0]))
            {
                window = new MainWindow(e.Args[0]);
            }
            else
            {
                window = new MainWindow();
            }
            window.Show();
        }
    }
}
