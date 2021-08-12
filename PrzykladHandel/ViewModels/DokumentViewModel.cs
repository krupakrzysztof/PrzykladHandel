using Prism.Commands;
using PrzykladHandel.Messages;
using PrzykladHandel.Views;
using Soneta.Business;
using Soneta.Business.App;
using Soneta.Fiskal.API;
using Soneta.Handel;
using Soneta.Towary;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using Tools = PrzykladHandel.Utils.Tools;

namespace PrzykladHandel.ViewModels
{
    public class DokumentViewModel : ViewModelBase
    {
        public DokumentViewModel()
        {
            Reload();
        }

        /// <summary>
        /// 
        /// </summary>
        private Session session;

        /// <summary>
        /// 
        /// </summary>
        private HandelModule handelModule;

        /// <summary>
        /// 
        /// </summary>
        private DokumentHandlowy dokument;

        /// <summary>
        /// 
        /// </summary>
        public DelegateCommand LogoutCommand { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        public DelegateCommand DodajPozycjeCommand { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        public DelegateCommand ZatwierdzCommand { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        public DelegateCommand AnulujDokumentCommand { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        public DelegateCommand StornoCommand { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        public string DokumentText => dokument != null ?
            $"Dokument {dokument.Numer.NumerPelny} na wartość {dokument.Suma.BruttoCy}"
            : string.Empty;

        private string kodTowaru;
        /// <summary>
        /// Wprowadzony kod towaru
        /// </summary>
        public string KodTowaru
        {
            get => kodTowaru;
            set
            {
                if (value.EndsWith("\t") || value.EndsWith(Environment.NewLine))
                {
                    kodTowaru = value.Replace("\t", string.Empty).Replace(Environment.NewLine, string.Empty);
                    RaisePropertyChanged(nameof(KodTowaru));
                    if (!string.IsNullOrWhiteSpace(kodTowaru))
                    {
                        DodajPozycje();
                    }
                }
                else
                {
                    _ = SetProperty(ref kodTowaru, value);
                }
            }
        }

        private ObservableCollection<PozycjaDokHandlowego> pozycje;
        /// <summary>
        /// Lista dodanych pozycji do dokumentu
        /// </summary>
        public ObservableCollection<PozycjaDokHandlowego> Pozycje
        {
            get => pozycje;
            private set => SetProperty(ref pozycje, value);
        }

        private PozycjaDokHandlowego selectedPozycja;
        /// <summary>
        /// 
        /// </summary>
        public PozycjaDokHandlowego SelectedPozycja
        {
            get => selectedPozycja;
            set => SetProperty(ref selectedPozycja, value);
        }

        private bool isKodTowarFocused;
        /// <summary>
        /// Informacja czy pole z kodem towaru jest aktywne
        /// </summary>
        public bool IsKodTowarFocused
        {
            get => isKodTowarFocused;
            set => SetProperty(ref isKodTowarFocused, value);
        }

        private string nipNabywcy;
        /// <summary>
        /// 
        /// </summary>
        public string NipNabywcy
        {
            get => nipNabywcy;
            set
            {
                if (value.Contains(Environment.NewLine))
                {
                    IsKodTowarFocused = false;
                    IsKodTowarFocused = true;
                }
                _ = SetProperty(ref nipNabywcy, value.Replace(Environment.NewLine, string.Empty));
            }
        }

        protected override void LoadCommands()
        {
            LogoutCommand = new DelegateCommand(Logout, CanLogout);
            DodajPozycjeCommand = new DelegateCommand(DodajPozycje, CanDodajPozycje).ObservesProperty(() => KodTowaru);
            ZatwierdzCommand = new DelegateCommand(ZatwierdzDokument, CanZatwierdzDokument);
            AnulujDokumentCommand = new DelegateCommand(AnulujDokument, CanAnulujDokument);
            StornoCommand = new DelegateCommand(DoStrono, CanDoStrono).ObservesProperty(() => SelectedPozycja);
        }

        protected override void Reload()
        {
            IsKodTowarFocused = false;
            if (Tools.Login is Login login)
            {
                session = login.CreateSession(false, false);
                handelModule = HandelModule.GetInstance(session);

                session.ExecuteInTransaction(() =>
                {
                    dokument = new DokumentHandlowy()
                    {
                        Definicja = handelModule.DefDokHandlowych.Paragon
                    };
                    handelModule.DokHandlowe.AddRow(dokument);
                    dokument.Magazyn = handelModule.Magazyny.Magazyny.Firma;
                    dokument.Kontrahent = handelModule.CRM.Kontrahenci.Incydentalny;
                });

                Pozycje = new ObservableCollection<PozycjaDokHandlowego>();
                SelectedPozycja = null;
                RaisePropertyChanged(nameof(DokumentText));
                ZatwierdzCommand.RaiseCanExecuteChanged();
            }
            NipNabywcy = string.Empty;
            IsKodTowarFocused = true;
        }

        /// <summary>
        /// Wylogowanie z programu
        /// </summary>
        private void Logout()
        {
            session?.ExecuteInTransaction(() =>
            {
                dokument?.Delete();
            });
            session?.Dispose();
            (Tools.Login as Login)?.Dispose();
            EventAggregator.GetEvent<ChangeViewMessage>().Publish(typeof(LoginView));
            Tools.CheckNoFiscalParameter();
        }

        /// <summary>
        /// Informacja czy możliwe jest wylogowanie
        /// </summary>
        /// <returns></returns>
        private bool CanLogout()
        {
            return Tools.Login != null && dokument != null
                && dokument.Pozycje.Count == 0;
        }

        /// <summary>
        /// Dodanie pozycji do dokumentu
        /// </summary>
        private void DodajPozycje()
        {
            Key<Towar> towary = handelModule.Towary.Towary.WgKodu[new FieldCondition.Like("Kod", $"{KodTowaru}*") | new FieldCondition.Equal("EAN", KodTowaru)];
            if (towary.Count > 1)
            {
                _ = MessageBox.Show($"Odnaleziono {towary.Count} o podanym kodzie. Wprowadź więcej znaków kodu przez ponownym wyszukaniem.", "Informacja", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            else if (towary.Count == 0)
            {
                _ = MessageBox.Show($"Nie odnaleziono towaru o kodzie {KodTowaru}", "Informacja", MessageBoxButton.OK, MessageBoxImage.Information);
                KodTowaru = string.Empty;
                return;
            }

            try
            {
                session.ExecuteInTransaction(() =>
                {
                    PozycjaDokHandlowego pozycja = dokument.Pozycje.Cast<PozycjaDokHandlowego>().FirstOrDefault(x => x.Towar.Guid == towary.First().Guid);
                    if (pozycja == null)
                    {
                        pozycja = new PozycjaDokHandlowego(dokument);
                        handelModule.PozycjeDokHan.AddRow(pozycja);
                        pozycja.Towar = towary.First();
                        pozycja.Ilosc = Quantity.Zero;
                    }
                    pozycja.Ilosc = new Quantity(pozycja.Ilosc.Value + 1, pozycja.Towar.Jednostka.Kod);

                    session.Events.Invoke();
                });

                RaisePropertyChanged(nameof(DokumentText));
                Pozycje = dokument.Pozycje.ToObservableCollection();
                ZatwierdzCommand.RaiseCanExecuteChanged();
                KodTowaru = string.Empty;
                LogoutCommand.RaiseCanExecuteChanged();
                AnulujDokumentCommand.RaiseCanExecuteChanged();
            }
            catch (Exception exception)
            {
                _ = MessageBox.Show(exception.Message, "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Informacja czy możliwe jest dodanie pozycji do dokumentu
        /// </summary>
        /// <returns></returns>
        private bool CanDodajPozycje()
        {
            return !string.IsNullOrWhiteSpace(KodTowaru) && dokument != null;
        }

        /// <summary>
        /// Zatwierdzenie dokumentu
        /// </summary>
        private void ZatwierdzDokument()
        {
            session.ExecuteInTransaction(() =>
            {
                if (!string.IsNullOrWhiteSpace(NipNabywcy))
                {
                    dokument.DaneKontrahenta.NIP = NipNabywcy;
                }
                dokument.Stan = StanDokumentuHandlowego.Zatwierdzony;
            });
            session.Save();
            if (!Tools.NoFiscalMode)
            {
                FiskalizujDokument();
            }
            AnulujDokumentCommand.RaiseCanExecuteChanged();
            LogoutCommand.RaiseCanExecuteChanged();
            Reload();
        }

        /// <summary>
        /// Fiskalizacja aktualnego dokumentu
        /// </summary>
        private void FiskalizujDokument()
        {
            try
            {
                session = (Tools.Login as Login).CreateSession(false, false);
                FiscalPrinterAPI apiDrukarki = new FiscalPrinterAPI(session);
                session.ExecuteInTransaction(() =>
                {
                    apiDrukarki.Fiskalizuj(session[dokument] as DokumentHandlowy, Tools.NazwaDrukarkiFiskalnej);
                });
                session.Save();
            }
            catch (Exception exception)
            {
                _ = MessageBox.Show($"Wystąpił błąd podczas fiskalizacji.{Environment.NewLine}{exception.Message}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Informacja czy możliwe jest zatwierdzenie dokumentu
        /// </summary>
        /// <returns></returns>
        private bool CanZatwierdzDokument()
        {
            return dokument != null && Pozycje != null
                && Pozycje.Count > 0;
        }

        /// <summary>
        /// Anulowanie wprowadzanego dokumentu
        /// </summary>
        private void AnulujDokument()
        {
            session.Dispose();
            Reload();
            LogoutCommand.RaiseCanExecuteChanged();
            AnulujDokumentCommand.RaiseCanExecuteChanged();
        }

        /// <summary>
        /// Informacja czy możliwe jest anulowanie dokumentu
        /// </summary>
        /// <returns></returns>
        private bool CanAnulujDokument()
        {
            return dokument != null && dokument.Pozycje.Count > 0;
        }

        /// <summary>
        /// Wystornowanie zaznaczonej pozycji
        /// </summary>
        private void DoStrono()
        {
            session.ExecuteInTransaction(() =>
            {
                SelectedPozycja.Delete();
            });
            IsKodTowarFocused = false;
            Pozycje = dokument.Pozycje.ToObservableCollection();
            ZatwierdzCommand.RaiseCanExecuteChanged();
            LogoutCommand.RaiseCanExecuteChanged();
            AnulujDokumentCommand.RaiseCanExecuteChanged();
            RaisePropertyChanged(nameof(DokumentText));
            IsKodTowarFocused = true;
        }

        /// <summary>
        /// Informacja czy możliwe jest wykonanie storna pozycji
        /// </summary>
        /// <returns></returns>
        private bool CanDoStrono()
        {
            return SelectedPozycja != null;
        }
    }
}
