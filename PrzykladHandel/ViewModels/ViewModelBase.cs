using Prism.Events;
using Prism.Mvvm;
using PrzykladHandel.Utils;

namespace PrzykladHandel.ViewModels
{
    public abstract class ViewModelBase : BindableBase
    {
        public ViewModelBase()
        {
            LoadCommands();
        }

        /// <summary>
        /// 
        /// </summary>
        protected EventAggregator EventAggregator => Tools.EventAggregator;

        /// <summary>
        /// Powiązanie metod z komendami
        /// </summary>
        protected abstract void LoadCommands();

        /// <summary>
        /// Przeładowanie danych widoku
        /// </summary>
        protected virtual void Reload()
        {

        }
    }
}
