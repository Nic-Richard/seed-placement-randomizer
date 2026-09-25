using System.Security.Cryptography;

namespace SeedPlacement.Core;

public sealed record ShelvedDish(int Number, DishLayout Layout, string? Label = null)
{
    public const int MaxLabelLength = 24;

    public string DisplayLabel => Label ?? $"Dish {Number}";
}

/// <summary>A shelf of slots numbered from the top left, left to right, then the next row.</summary>
public sealed class Rack
{
    public const int Rows = 2;
    public const int Columns = 5;
    public const int SlotCount = Rows * Columns;

    private readonly ShelvedDish?[] _slots = new ShelvedDish?[SlotCount];
    private readonly Func<int, int> _pick;
    private int _nextNumber = 1;

    public Rack() : this(RandomNumberGenerator.GetInt32) { }

    internal Rack(Func<int, int> pick) => _pick = pick;

    public IReadOnlyList<ShelvedDish?> Slots => _slots;

    public int Occupied => _slots.Count(s => s is not null);

    public bool IsFull => Occupied == SlotCount;

    public int NextNumber => _nextNumber;

    /// <summary>
    /// Puts the dish in a uniformly random empty slot. Filling a rack this way gives every assignment of
    /// dishes to slots the same probability. Returns the zero-based slot index.
    /// </summary>
    public int Shelve(DishLayout layout)
    {
        var empty = Enumerable.Range(0, SlotCount).Where(i => _slots[i] is null).ToArray();
        if (empty.Length == 0) throw new InvalidOperationException("The rack is full.");
        var slot = empty[_pick(empty.Length)];
        _slots[slot] = new ShelvedDish(_nextNumber++, layout);
        return slot;
    }

    public ShelvedDish? Remove(int slot)
    {
        var dish = _slots[slot];
        _slots[slot] = null;
        return dish;
    }

    /// <summary>Sets a custom label; a blank label restores the default "Dish N".</summary>
    public ShelvedDish Relabel(int slot, string? label)
    {
        var dish = _slots[slot] ?? throw new InvalidOperationException($"Slot {SlotNumber(slot)} is empty.");
        var trimmed = label?.Trim();
        if (trimmed is { Length: > ShelvedDish.MaxLabelLength }) trimmed = trimmed[..ShelvedDish.MaxLabelLength];
        var updated = dish with { Label = string.IsNullOrEmpty(trimmed) ? null : trimmed };
        _slots[slot] = updated;
        return updated;
    }

    public void Clear()
    {
        Array.Clear(_slots);
        _nextNumber = 1;
    }

    /// <summary>Replaces the rack's contents with a saved run.</summary>
    public void Restore(IEnumerable<(int Slot, ShelvedDish Dish)> dishes, int nextNumber)
    {
        Clear();
        foreach (var (slot, dish) in dishes)
        {
            if (slot is < 0 or >= SlotCount) throw new ArgumentOutOfRangeException(nameof(dishes), $"Slot {slot} is outside the rack.");
            if (_slots[slot] is not null) throw new ArgumentException($"Slot {SlotNumber(slot)} is used twice.", nameof(dishes));
            _slots[slot] = dish;
        }
        var highest = _slots.Max(d => d?.Number ?? 0);
        _nextNumber = Math.Max(nextNumber, highest + 1);
    }

    public static int SlotNumber(int slot) => slot + 1;
}
