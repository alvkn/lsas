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
    private string _avatarUrl;

    [ObservableProperty]
    private bool _isVacBanned;

    [ObservableProperty]
    private bool _isLimited;

    [ObservableProperty]
    private bool _isActive;

    public void RefreshFromModel()
    {
        AvatarUrl = Model.AvatarUrl;
        IsVacBanned = Model.IsVacBanned;
        IsLimited = Model.IsLimited;
        OnPropertyChanged(nameof(AccountName));
        OnPropertyChanged(nameof(PersonaName));
    }
}