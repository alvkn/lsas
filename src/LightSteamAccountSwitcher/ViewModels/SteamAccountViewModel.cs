using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using LightSteamAccountSwitcher.Core.Models;

namespace LightSteamAccountSwitcher.ViewModels;

public partial class SteamAccountViewModel : ObservableObject
{
    public SteamAccount Model { get; }

    public SteamAccountViewModel(SteamAccount model)
    {
        Model = model;
        RefreshFromModel();
    }

    public string AccountName => Model.AccountName;

    public string PersonaName => Model.PersonaName;

    public bool WantsOfflineMode => Model.WantsOfflineMode;

    [ObservableProperty]
    private ImageSource? _avatarSource;

    [ObservableProperty]
    private bool _isVacBanned;

    [ObservableProperty]
    private bool _isLimited;

    [ObservableProperty]
    private bool _isActive;

    public void RefreshFromModel()
    {
        AvatarSource = LoadImage(Model.AvatarUrl);
        IsVacBanned = Model.IsVacBanned;
        IsLimited = Model.IsLimited;
        OnPropertyChanged(nameof(AccountName));
        OnPropertyChanged(nameof(PersonaName));
    }

    private static ImageSource? LoadImage(string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return null;
        }

        try
        {
            if (path.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                return new BitmapImage(new Uri(path));
            }

            if (File.Exists(path))
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.UriSource = new Uri(path);
                bitmap.EndInit();
                bitmap.Freeze();
                return bitmap;
            }
        }
        catch
        {
            // Ignore errors
        }

        return null;
    }
}