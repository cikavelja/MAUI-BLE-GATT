using MauiBleApp2.Core.ViewModels;
using MauiBleApp2.Views;
using MauiBleApp2.Core.Models;
using MauiBleApp2.ViewModels;

namespace MauiBleApp2.Views
{
    public partial class DeviceScanPage : ContentPage
    {
        private readonly MauiBleApp2.Core.ViewModels.DeviceScanViewModel _viewModel;
        private readonly IServiceProvider _serviceProvider;
        
        public DeviceScanPage(MauiBleApp2.Core.ViewModels.DeviceScanViewModel viewModel, IServiceProvider serviceProvider)
        {
            InitializeComponent();
            _viewModel = viewModel;
            _serviceProvider = serviceProvider;
            BindingContext = _viewModel;
            _viewModel.NavigateToDeviceDetailsRequested += OnNavigateToDeviceDetailsRequested;
        }

        private async void OnNavigateToDeviceDetailsRequested(object? sender, MauiBleApp2.Core.Models.BleDeviceInfo deviceInfo)
        {
            var detailsViewModel = _serviceProvider.GetRequiredService<DeviceDetailsViewModel>();
            detailsViewModel.Device = deviceInfo;
            var detailsPage = _serviceProvider.GetRequiredService<DeviceDetailsPage>();
            detailsPage.BindingContext = detailsViewModel;
            await Navigation.PushAsync(detailsPage);
        }
    }
}