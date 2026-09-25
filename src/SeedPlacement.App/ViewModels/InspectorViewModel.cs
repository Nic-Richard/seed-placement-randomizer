using SeedPlacement.Core;

namespace SeedPlacement.App.ViewModels;

public sealed record SeedRow(int Index, string Number, string X, string Y);

public sealed class InspectorViewModel
{
    public InspectorViewModel(DishItem dish, Action remove)
    {
        Dish = dish;
        RemoveCommand = new RelayCommand(remove);
        Rows = dish.Layout.Seeds
            .Select((p, i) => new SeedRow(i, (i + 1).ToString(), Mm(p.X), Mm(p.Y)))
            .ToList();
    }

    public DishItem Dish { get; }

    public int Slot => Dish.Slot;

    public DishLayout Layout => Dish.Layout;

    public string MethodText => Layout.Method switch
    {
        SamplingMethod.Exact => "Exact rejection sampling",
        _ => "Hard-disk Markov chain",
    };

    public IReadOnlyList<SeedRow> Rows { get; }

    public RelayCommand RemoveCommand { get; }

    private static string Mm(double value) =>
        (value < 0 ? "−" : "+") + Math.Abs(value).ToString("0.0");
}
