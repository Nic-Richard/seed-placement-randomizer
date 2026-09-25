using SeedPlacement.Core;

namespace SeedPlacement.App.ViewModels;

public sealed class SlotViewModel(int index) : ObservableObject
{
    private DishItem? _dish;

    public int Index { get; } = index;

    public string Label { get; } = Rack.SlotNumber(index).ToString();

    public DishItem? Dish
    {
        get => _dish;
        set
        {
            if (!Set(ref _dish, value)) return;
            OnPropertyChanged(nameof(Layout));
            OnPropertyChanged(nameof(HasDish));
        }
    }

    public DishLayout? Layout => Dish?.Layout;

    public bool HasDish => Dish is not null;
}
