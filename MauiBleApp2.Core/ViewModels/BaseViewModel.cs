using CommunityToolkit.Mvvm.ComponentModel;

namespace MauiBleApp2.Core.ViewModels
{
    /// <summary>
    /// Base view model class that implements INotifyPropertyChanged
    /// </summary>
    public abstract class BaseViewModel : ObservableObject
    {
        private bool _isBusy;
        private string _title = string.Empty;
        
        /// <summary>
        /// Gets or sets whether the view model is currently busy
        /// </summary>
        public bool IsBusy
        {
            get => _isBusy;
            set => SetProperty(ref _isBusy, value);
        }

        /// <summary>
        /// Gets or sets the title of the view model
        /// </summary>
        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }
    }
}