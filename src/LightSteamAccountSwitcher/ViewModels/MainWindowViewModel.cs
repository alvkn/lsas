using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LightSteamAccountSwitcher.Core.Services;
using LightSteamAccountSwitcher.Steam;
using LightSteamAccountSwitcher.Windows;

namespace LightSteamAccountSwitcher.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    private readonly SteamService _steamService = new();

    [ObservableProperty]
    private ObservableCollection<SteamAccountViewModel> _accounts = [];

    [ObservableProperty]
    private SteamAccountViewModel? _selectedAccount;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _statusMessage = "Ready";

    [RelayCommand]
    public async Task LoadAccounts()
    {
        IsLoading = true;
        StatusMessage = "Loading accounts...";
        Accounts.Clear();

        try
        {
            var activeUser = SteamService.GetActiveAccountName();
            var accounts = await Task.Run(() => _steamService.GetSteamUsers());

            foreach (var acc in accounts)
            {
                var vm = new SteamAccountViewModel(acc);
                if (!string.IsNullOrEmpty(activeUser) && acc.AccountName == activeUser)
                {
                    vm.IsActive = true;
                }

                Accounts.Add(vm);
            }

            StatusMessage = $"Loaded {Accounts.Count} accounts.";

            _ = Task.Run(async () =>
            {
                foreach (var accVm in Accounts)
                {
                    await _steamService.EnrichAccountInfo(accVm.Model);
                    Application.Current.Dispatcher.Invoke(() => accVm.RefreshFromModel());
                }
            });
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void OpenAbout()
    {
        var aboutWindow = new AboutWindow();
        aboutWindow.Owner = Application.Current.MainWindow;
        aboutWindow.ShowDialog();
    }

    [RelayCommand]
    private void OpenSettings()
    {
        var settingsViewModel = new SettingsViewModel(_steamService);
        var settingsWindow = new SettingsWindow(settingsViewModel);
        settingsWindow.Owner = Application.Current.MainWindow;
        settingsWindow.ShowDialog();
    }

    [RelayCommand]
    private async Task SwitchAccount(SteamAccountViewModel accountVm)
    {
        await SwitchWithState(accountVm, 1);
    }

    [RelayCommand]
    private async Task SwitchOnline(SteamAccountViewModel accountVm)
    {
        await SwitchWithState(accountVm, 1);
    }

    [RelayCommand]
    private async Task SwitchInvisible(SteamAccountViewModel accountVm)
    {
        await SwitchWithState(accountVm, 7);
    }

    [RelayCommand]
    private async Task SwitchOffline(SteamAccountViewModel accountVm)
    {
        await SwitchWithState(accountVm, 0);
    }

    private async Task SwitchWithState(SteamAccountViewModel? accountVm, int state)
    {
        if (accountVm == null)
        {
            return;
        }

        IsLoading = true;
        StatusMessage = $"Switching to {accountVm.AccountName}...";

        try
        {
            await Task.Run(() => _steamService.SwitchAccount(accountVm.Model, state));
            StatusMessage = $"Switched to {accountVm.AccountName}.";
            Accounts.FirstOrDefault(acc => acc.IsActive)?.IsActive = false;
            accountVm.IsActive = true;

            if (SettingsService.Settings.AutoClose)
            {
                Application.Current.Shutdown();
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error switching: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task AddAccount()
    {
        IsLoading = true;
        StatusMessage = "Starting Steam for new login...";
        try
        {
            await Task.Run(() => SteamService.StartSteamLogin());
            StatusMessage = "Steam started. Please log in.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public void ForgetAccount(SteamAccountViewModel? accountVm)
    {
        if (accountVm == null)
        {
            return;
        }

        SteamService.ForgetAccount(accountVm.Model.SteamId64);
        Accounts.Remove(accountVm);
        StatusMessage = $"Forgot {accountVm.AccountName}";
    }

    [RelayCommand]
    private void OpenGameData(SteamAccountViewModel? accountVm)
    {
        if (accountVm == null)
        {
            return;
        }

        try
        {
            var steamPath = SteamRegistryHelper.GetSteamPath();

            if (string.IsNullOrEmpty(steamPath))
            {
                StatusMessage = "Steam path not found.";
                return;
            }

            // Normalize path (SteamPath in registry often uses forward slashes)
            steamPath = steamPath.Replace('/', Path.DirectorySeparatorChar);

            var id32 = new SteamId(accountVm.Model.SteamId64).Id32;
            var path = Path.Combine(steamPath, "userdata", id32);

            if (Directory.Exists(path))
            {
                Process.Start(new ProcessStartInfo("explorer.exe", $"\"{path}\"") { UseShellExecute = true });
            }
            else
            {
                StatusMessage = $"Userdata folder not found: {path}";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error opening folder: {ex.Message}";
        }
    }

    [RelayCommand]
    private void CreateShortcut(SteamAccountViewModel? accountVm)
    {
        if (accountVm == null)
        {
            return;
        }

        try
        {
            var exePath = Environment.ProcessPath;
            var desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            var shortcutPath = Path.Combine(desktop, $"Switch to {accountVm.AccountName}.lnk");

            var iconPath = SteamService.GetCachedIconPath(accountVm.Model.SteamId64);
            if (iconPath == null)
            {
                // Try to create icon from avatar
                var avatarPath = Path.Combine(AppDataHelper.GetCachePath("Avatars"),
                    $"{accountVm.Model.SteamId64}.jpg");
                if (File.Exists(avatarPath))
                {
                    var newIconPath = Path.ChangeExtension(avatarPath, ".ico");
                    IconHelper.CreateIconFromImage(avatarPath, newIconPath);
                    if (File.Exists(newIconPath))
                    {
                        iconPath = newIconPath;
                    }
                }
            }

            ShortcutHelper.CreateShortcut(
                shortcutPath,
                exePath!,
                $"--switch {accountVm.Model.SteamId64}",
                $"Switch to {accountVm.AccountName}",
                iconPath ?? "");

            StatusMessage = "Shortcut created on Desktop.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error creating shortcut: {ex.Message}";
        }
    }

    [RelayCommand]
    private void CopySteamId64(SteamAccountViewModel? accountVm)
    {
        if (accountVm == null)
        {
            return;
        }

        Clipboard.SetText(accountVm.Model.SteamId64);
        StatusMessage = "Copied SteamID64";
    }

    [RelayCommand]
    private void CopySteamId3(SteamAccountViewModel? accountVm)
    {
        if (accountVm == null)
        {
            return;
        }

        var conv = new SteamId(accountVm.Model.SteamId64);
        Clipboard.SetText(conv.Id3);
        StatusMessage = "Copied SteamID3";
    }

    [RelayCommand]
    private void CopySteamIdLegacy(SteamAccountViewModel? accountVm)
    {
        if (accountVm == null)
        {
            return;
        }

        var conv = new SteamId(accountVm.Model.SteamId64);
        Clipboard.SetText(conv.Id);
        StatusMessage = "Copied SteamID";
    }

    [RelayCommand]
    private void CopyProfileLink(SteamAccountViewModel? accountVm)
    {
        if (accountVm == null)
        {
            return;
        }

        Clipboard.SetText($"https://steamcommunity.com/profiles/{accountVm.Model.SteamId64}");
        StatusMessage = "Copied profile link";
    }
}