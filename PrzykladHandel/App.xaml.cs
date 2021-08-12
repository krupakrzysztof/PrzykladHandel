using PrzykladHandel.Utils;
using Soneta.Business.App;
using Soneta.Start;
using System;
using System.Configuration;
using System.IO;
using System.Reflection;
using System.Windows;

namespace PrzykladHandel
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            string sonetaBusinessPath = Path.Combine(ConfigurationManager.AppSettings["EnovaInstallPath"], "Soneta.Business.dll");
            if (!File.Exists(sonetaBusinessPath))
            {
                _ = MessageBox.Show($"Nie odnaleziono pliku \"{sonetaBusinessPath}\"", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                Environment.Exit(-1);
            }

            _ = Assembly.LoadFrom(sonetaBusinessPath);
            Loader loader = new Loader()
            {
                WithExtensions = true
            };
            loader.Load();

            base.OnStartup(e);
        }

        protected override void OnExit(ExitEventArgs e)
        {
            if (Tools.Login is Login login)
            {
                login.Dispose();
            }
            base.OnExit(e);
        }
    }
}
