using System.Windows;
using LightSteamAccountSwitcher.ViewModels;

namespace LightSteamAccountSwitcher;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        var viewModel = new MainWindowViewModel();
        DataContext = viewModel;
        Loaded += async (s, e) => await viewModel.LoadAccounts();
    }
}