using Avalonia.Media.Imaging;
using Avalonia.Platform;
using SeedPlacement.App.Controls;

namespace SeedPlacement.App.ViewModels;

public sealed class SeedLookOption(SeedKindInfo info) : ObservableObject
{
    private bool _isSelected;

    public SeedKindInfo Info { get; } = info;

    public string Name => Info.Name;

    public Bitmap Sprite { get; } = new(AssetLoader.Open(info.SpriteUri(0)));

    public bool IsSelected
    {
        get => _isSelected;
        set => Set(ref _isSelected, value);
    }
}
