using Prism.Events;
using PrzykladHandel.Messages;
using System.Configuration;

namespace PrzykladHandel.Utils
{
    public static class Tools
    {
        /// <summary>
        /// 
        /// </summary>
        public static EventAggregator EventAggregator { get; } = new EventAggregator();

        /// <summary>
        /// Baza danych, na której pracuje program
        /// </summary>
        public static object Database { get; internal set; }

        /// <summary>
        /// Login, na którym pracuje program
        /// </summary>
        public static object Login { get; internal set; }

        /// <summary>
        /// Informacja czy aplikacja jest w trybie niefiskalnym
        /// </summary>
        public static bool NoFiscalMode { get; internal set; }

        /// <summary>
        /// Nazwa drukarki fiskalnej, na której drukowane będą paragony
        /// </summary>
        public static string NazwaDrukarkiFiskalnej { get; internal set; }

        /// <summary>
        /// Sprawdzenie czy program jest uruchomiony w trybie fiskalnym
        /// </summary>
        internal static void CheckNoFiscalParameter()
        {
            if (bool.TryParse(ConfigurationManager.AppSettings["NoFiscalMode"], out bool noFiscalMode))
            {
                NoFiscalMode = noFiscalMode;
                EventAggregator.GetEvent<ShowNoFiscalWarningMessage>().Publish(NoFiscalMode);
            }
        }
    }
}
