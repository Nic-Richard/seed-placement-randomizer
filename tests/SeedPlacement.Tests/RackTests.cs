using SeedPlacement.Core;

namespace SeedPlacement.Tests;

public class RackTests
{
    private static DishLayout AnyLayout() =>
        SeedSampler.Generate(new LayoutCode(1, PlacementSettings.Default));

    [Fact]
    public void Fills_every_slot_once_then_refuses()
    {
        var rack = new Rack();
        var slots = new HashSet<int>();

        for (var i = 0; i < Rack.SlotCount; i++) Assert.True(slots.Add(rack.Shelve(AnyLayout())));

        Assert.True(rack.IsFull);
        Assert.Throws<InvalidOperationException>(() => rack.Shelve(AnyLayout()));
    }

    [Fact]
    public void Numbers_dishes_in_order_and_restarts_after_clearing()
    {
        var rack = new Rack();
        var first = rack.Shelve(AnyLayout());
        var second = rack.Shelve(AnyLayout());

        Assert.Equal(1, rack.Slots[first]!.Number);
        Assert.Equal(2, rack.Slots[second]!.Number);

        rack.Clear();

        Assert.Equal(0, rack.Occupied);
        Assert.Equal(1, rack.NextNumber);
    }

    [Fact]
    public void Removing_frees_the_slot_without_renumbering()
    {
        var rack = new Rack();
        var slot = rack.Shelve(AnyLayout());
        rack.Shelve(AnyLayout());

        Assert.Equal(1, rack.Remove(slot)!.Number);
        Assert.Null(rack.Slots[slot]);
        Assert.Equal(3, rack.NextNumber);
    }

    [Fact]
    public void Picks_among_empty_slots_only()
    {
        var rack = new Rack(n => n - 1);
        Assert.Equal(9, rack.Shelve(AnyLayout()));
        Assert.Equal(8, rack.Shelve(AnyLayout()));
        rack.Remove(9);
        Assert.Equal(9, rack.Shelve(AnyLayout()));
    }

    [Fact]
    public void Labels_default_to_the_dish_number_and_can_be_changed()
    {
        var rack = new Rack();
        var slot = rack.Shelve(AnyLayout());

        Assert.Equal("Dish 1", rack.Slots[slot]!.DisplayLabel);
        Assert.Equal("Control A", rack.Relabel(slot, "  Control A ").DisplayLabel);
        Assert.Equal("Dish 1", rack.Relabel(slot, "   ").DisplayLabel);
        Assert.Equal(ShelvedDish.MaxLabelLength, rack.Relabel(slot, new string('x', 40)).DisplayLabel.Length);
        Assert.Throws<InvalidOperationException>(() => rack.Relabel((slot + 1) % Rack.SlotCount, "x"));
    }

    [Fact]
    public void Each_dish_keeps_its_own_seed_type()
    {
        var rack = new Rack();
        var wheat = rack.Shelve(AnyLayout(), "Wheat");
        var lettuce = rack.Shelve(AnyLayout(), "Lettuce");

        Assert.Equal("Wheat", rack.Slots[wheat]!.SeedType);
        Assert.Equal("Lettuce", rack.Slots[lettuce]!.SeedType);
        Assert.Equal("Wheat", rack.Relabel(wheat, "Control A").SeedType);
    }

    [Fact]
    public void Slot_numbers_start_top_left_and_read_across()
    {
        Assert.Equal(1, Rack.SlotNumber(0));
        Assert.Equal(5, Rack.SlotNumber(Rack.Columns - 1));
        Assert.Equal(6, Rack.SlotNumber(Rack.Columns));
        Assert.Equal(10, Rack.SlotNumber(Rack.SlotCount - 1));
    }

    [Fact]
    public void First_dish_is_equally_likely_in_any_slot()
    {
        const int trials = 20_000;
        var layout = AnyLayout();
        var counts = new int[Rack.SlotCount];
        for (var i = 0; i < trials; i++) counts[new Rack().Shelve(layout)]++;

        var expected = trials / (double)Rack.SlotCount;
        var chi = counts.Sum(c => (c - expected) * (c - expected) / expected);
        // Critical value for 9 degrees of freedom at p = 0.001.
        Assert.True(chi < 27.88, $"chi-square {chi:F2}");
    }
}
