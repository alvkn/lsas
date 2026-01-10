using System.IO;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LightSteamAccountSwitcher.Core.Services;
using LightSteamAccountSwitcher.Steam;

namespace LightSteamAccountSwitcher.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly SteamService _steamService;

    [ObservableProperty]
    private bool _autoClose;

    public SettingsViewModel(SteamService steamService)
    {
        _steamService = steamService;
        SettingsService.Load();

        AutoClose = SettingsService.Settings.AutoClose;
    }

    public SettingsViewModel() : this(new SteamService()) { }

    [RelayCommand]
    private void Save()
    {
        SettingsService.Settings.AutoClose = AutoClose;
        SettingsService.Save();
    }

    [RelayCommand]
    private void ClearCache()
    {
        if (MessageBox.Show("Are you sure you want to clear all cached images and data?",
                "Clear Cache",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning) ==
            MessageBoxResult.Yes)
        {
            _steamService.ClearCache();
            MessageBox.Show("Cache cleared.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    [RelayCommand]
    private void WipeData()
    {
        var result = MessageBox.Show(
            "Are you sure you want to delete ALL application data and exit? This cannot be undone.",
            "Wipe Data",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result != MessageBoxResult.Yes)
        {
            return;
        }

        try
        {
            var appData = AppDataService.GetAppDataPath();
            if (Directory.Exists(appData))
            {
                Directory.Delete(appData, true);
            }

            Application.Current.Shutdown();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error wiping data: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}