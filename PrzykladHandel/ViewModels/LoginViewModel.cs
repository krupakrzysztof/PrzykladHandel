using Prism.Commands;
using PrzykladHandel.Messages;
using PrzykladHandel.Utils;
using PrzykladHandel.Views;
using Soneta.Business;
using Soneta.Business.App;
using Soneta.Fiskal;
using Soneta.Handel;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;

namespace PrzykladHandel.ViewModels
{
    public class LoginViewModel : ViewModelBase
    {
        public LoginViewModel()
        {
            LoadLogins();
        }

        /// <summary>
        /// 
        /// </summary>
        public DelegateCommand LoginCommand { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        public DelegateCommand CancelCommand { get; private set; }

        private ObservableCollection<string> logins;
        /// <summary>
        /// Dostępne loginy w bazie danych
        /// </summary>
        public ObservableCollection<string> Logins
        {
            get => logins;
            set => SetProperty(ref logins, value);
        }

        private string selectedLogin;
        /// <summary>
        /// Wybrany login
        /// </summary>
        public string SelectedLogin
        {
            get => selectedLogin;
            set => SetProperty(ref selectedLogin, value, () => LoginCommand.RaiseCanExecuteChanged());
        }

        private string password;
        /// <summary>
        /// Wprowadzone hasło
        /// </summary>
        public string Password
        {
            get => password;
            set => SetProperty(ref password, value);
        }

        private bool isLoginSelected;
        /// <summary>
        /// Informacja czy pole z loginem jest zaznaczone
        /// </summary>
        public bool IsLoginSelected
        {
            get => isLoginSelected;
            set => SetProperty(ref isLoginSelected, value);
        }

        protected override void LoadCommands()
        {
            LoginCommand = new DelegateCommand(LoginToDb, CanLoginToDb);
            CancelCommand = new DelegateCommand(Cancel);
        }

        protected override void Reload()
        {
            IsLoginSelected = false;
            SelectedLogin = null;
            Password = null;
            LoadLogins();
        }

        /// <summary>
        /// Załadowanie dostępnych loginów
        /// </summary>
        private void LoadLogins()
        {
            Logins = (Tools.Database as Database).Operators.ToObservableCollection();
            IsLoginSelected = true;
        }

        /// <summary>
        /// Logowanie do bazy danych
        /// </summary>
        private void LoginToDb()
        {
            try
            {
                Tools.Login = (Tools.Database as Database).Login(false, SelectedLogin, Password ?? string.Empty);

                if (!Tools.NoFiscalMode)
                {
                    if (!ZnajdzDrukarke())
                    {
                        (Tools.Login as Login).Dispose();
                        return;
                    }
                    else
                    {
                        Tools.NoFiscalMode = true;
                        EventAggregator.GetEvent<ShowNoFiscalWarningMessage>().Publish(Tools.NoFiscalMode);
                    }
                }
                EventAggregator.GetEvent<ChangeViewMessage>().Publish(typeof(DokumentView));
            }
            catch (Exception exception)
            {
                _ = MessageBox.Show(exception.Message, "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Informacja czy możliwe jest zalogowanie do bazy
        /// </summary>
        /// <returns></returns>
        private bool CanLoginToDb()
        {
            return !string.IsNullOrWhiteSpace(SelectedLogin);
        }

        /// <summary>
        /// 
        /// </summary>
        private void Cancel()
        {
            Application.Current.Shutdown();
        }

        /// <summary>
        /// Sprawdzenie czy do stanowiska podłączona jest drukarka fiskalna
        /// </summary>
        /// <returns></returns>
        private bool ZnajdzDrukarke()
        {
            using (Session session = (Tools.Login as Login).CreateSession(false, false))
            {
                DrukarkaFiskalna drukarkaFiskalna = HandelModule.GetInstance(session).DrukarkiFiskalne.Rows.Cast<DrukarkaFiskalna>().FirstOrDefault(x => x.Domyslna);
                if (drukarkaFiskalna == null)
                {
                    return MessageBox.Show($"Nie znaleziono domyślnej drukarki fiskalnej.{Environment.NewLine}Czy chcesz kontynuować?", "Pytanie", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes;
                }
                try
                {
                    object obj = null;
                    using (IFiskalPrinter fiskalPrinter = new FiskalManager.Creator(session).CreateFromAttribute(DrukarkiFiskalne.PrinterDrivers[drukarkaFiskalna.Nazwa]))
                    {
                        if (fiskalPrinter != null)
                        {
                            obj = fiskalPrinter.Test();
                        }
                        if (obj != null)
                        {
                            if (obj is Exception exception)
                            {
                                if (MessageBox.Show($"Wystąpił błąd podczas łączenia z drukarką fiskalną.{Environment.NewLine}{exception.Message}{Environment.NewLine}Czy chcesz kontynuować?", "Błąd", MessageBoxButton.YesNo, MessageBoxImage.Error) == MessageBoxResult.Yes)
                                {
                                    Tools.NoFiscalMode = true;
                                }
                                return false;
                            }
                        }
                    }
                }
                catch (Exception exception)
                {
                    return MessageBox.Show($"Wystąpił błąd podczas łączenia z drukarką fiskalną.{Environment.NewLine}{exception.Message}{Environment.NewLine}Czy chcesz kontynuować?", "Błąd", MessageBoxButton.YesNo, MessageBoxImage.Error) == MessageBoxResult.Yes;
                }

                Tools.NazwaDrukarkiFiskalnej = drukarkaFiskalna.Nazwa;
            }

            return true;
        }
    }
}
