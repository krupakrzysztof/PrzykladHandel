using PrzykladHandel.Messages;
using PrzykladHandel.Utils;
using PrzykladHandel.Views;
using Soneta.Business.App;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace PrzykladHandel.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        public MainViewModel()
        {
            Tools.Database = BusApplication.Instance[ConfigurationManager.AppSettings["DatabaseName"], false];
            if (Tools.Database == null)
            {
                _ = MessageBox.Show($"Nie odnaleziono bazy danych o nazwie \"{ConfigurationManager.AppSettings["DatabaseName"]}\"", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                Application.Current.Shutdown(-1);
                return;
            }

            _ = EventAggregator.GetEvent<ChangeViewMessage>().Subscribe(viewType =>
            {
                if (!views.ContainsKey(viewType))
                {
                    views.Add(viewType, Activator.CreateInstance(viewType) as UserControl);
                }
                else if (views[viewType].DataContext is ViewModelBase viewModelBase)
                {
                    _ = typeof(ViewModelBase).GetMethod(nameof(Reload), BindingFlags.Instance | BindingFlags.NonPublic)?.Invoke(viewModelBase, new object[] { });
                }
                SelectedView = views[viewType];
            });
            _ = EventAggregator.GetEvent<ShowNoFiscalWarningMessage>().Subscribe(isWarning =>
            {
                NoFiscalModeWarning = isWarning ? "Tryb niefiskalny" : string.Empty;
            });

            EventAggregator.GetEvent<ChangeViewMessage>().Publish(typeof(LoginView));

            _ = Task.Run(() =>
            {
                do
                {
                    ActualTime = $"{DateTime.Now}";

                    Thread.Sleep(75);
                } while (true);
            });

            Tools.CheckNoFiscalParameter();
            CacheAlgorithms();
        }

        /// <summary>
        /// 
        /// </summary>
        private readonly Dictionary<Type, UserControl> views = new Dictionary<Type, UserControl>();

        private UserControl selectedView;
        /// <summary>
        /// 
        /// </summary>
        public UserControl SelectedView
        {
            get => selectedView;
            set => SetProperty(ref selectedView, value);
        }

        private string actualTime;
        /// <summary>
        /// 
        /// </summary>
        public string ActualTime
        {
            get => actualTime;
            set => SetProperty(ref actualTime, value);
        }

        private string noFiscalModeWarning;
        /// <summary>
        /// 
        /// </summary>
        public string NoFiscalModeWarning
        {
            get => noFiscalModeWarning;
            private set => SetProperty(ref noFiscalModeWarning, value);
        }

        protected override void LoadCommands()
        {

        }

        /// <summary>
        /// Załadowanie algorytmó Sonety przed startem aplikacji, aby uniknąć błędów kompilacji
        /// </summary>
        private void CacheAlgorithms()
        {
            try
            {
                using (Login login = (Tools.Database as Database).LoginAsScheduler())
                {
                    login.CacheAlgorithms();
                }
            }
            catch (Exception exception)
            {
                _ = MessageBox.Show(exception.Message, "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
