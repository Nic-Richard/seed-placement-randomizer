using SeedPlacement.Core;

namespace SeedPlacement.App.ViewModels;

/// <summary>A dish on the rack, shared by the bench, its slot and the inspector so a label edit shows everywhere.</summary>
public sealed class DishItem : ObservableObject
{
    private readonly Rack _rack;
    private ShelvedDish _dish;

    public DishItem(Rack rack, int slot)
    {
        _rack = rack;
        Slot = slot;
        _dish = rack.Slots[slot] ?? throw new ArgumentException("Slot is empty.", nameof(slot));
    }

    public int Slot { get; }

    public int Number => _dish.Number;

    public DishLayout Layout => _dish.Layout;

    public int MaxLabelLength => ShelvedDish.MaxLabelLength;

    /// <summary>The label written on the dish's tape. Clearing it restores "Dish N".</summary>
    public string Label
    {
        get => _dish.DisplayLabel;
        set
        {
            var updated = _rack.Relabel(Slot, value);
            if (updated == _dish) return;
            _dish = updated;
            OnPropertyChanged();
            OnPropertyChanged(nameof(Caption));
        }
    }

    public string SlotText => $"Slot {Rack.SlotNumber(Slot)}";

    public string Caption => $"{Label} goes in slot {Rack.SlotNumber(Slot)}";

    public string Code => Layout.Code.ToString();

    public string Summary => Describe(Layout);

    public string Detail => $"{Summary}. Layout code {Code}";

    public static string Describe(DishLayout layout)
    {
        var n = layout.Seeds.Count;
        return n == 1 ? "1 seed" : $"{n} seeds, at least {layout.Settings.SpacingMm:0.#} mm apart";
    }
}
