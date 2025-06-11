using Microsoft.Extensions.DependencyInjection;
using MauiBleApp2.ViewModels;
using MauiBleApp2.Views;
using MauiBleApp2.Services.Bluetooth;

namespace MauiBleApp2
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

            // Start with MainPage wrapped in NavigationPage
            MainPage = new NavigationPage(new MainPage());
        }
    }
}
