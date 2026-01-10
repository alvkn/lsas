using System.Windows;
using LightSteamAccountSwitcher.Core;
using LightSteamAccountSwitcher.Steam;

namespace LightSteamAccountSwitcher;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        SettingsHelper.Load();

        // Argument parsing
        for (var i = 0; i < e.Args.Length; i++)
        {
            if (e.Args[i] == "--switch" && i + 1 < e.Args.Length)
            {
                var steamId = e.Args[i + 1];
                var service = new SteamService();

                var state = -1;
                // Scan for state arg
                for (var j = 0; j < e.Args.Length; j++)
                {
                    if (e.Args[j] == "--state" && j + 1 < e.Args.Length)
                    {
                        int.TryParse(e.Args[j + 1], out state);
                    }
                }

                var accounts = service.GetSteamUsers();
                var account = accounts.FirstOrDefault(x => x.SteamId64 == steamId);

                if (account != null)
                {
                    service.SwitchAccount(account, state);
                }

                Shutdown();
                return;
            }
        }

        // Normal startup
        var mainWindow = new MainWindow();
        mainWindow.Show();
    }
}