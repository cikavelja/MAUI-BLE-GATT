using MauiBleApp2.ViewModels;

namespace MauiBleApp2.Views
{
    public partial class DeviceDetailsPage : ContentPage
    {
        public DeviceDetailsPage(DeviceDetailsViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = viewModel;
        }
    }
}