using System.Collections.ObjectModel;
using Avalonia;
using SeedPlacement.App.Controls;
using SeedPlacement.App.Styles;
using SeedPlacement.Core;

namespace SeedPlacement.App.ViewModels;

public sealed record DishShelvedEventArgs(DishItem Dish, int Slot);

public sealed class MainViewModel : ObservableObject
{
    private readonly Rack _rack = new();
    private int _seedCount = PlacementSettings.Default.SeedCount;
    private double _spacingMm = PlacementSettings.Default.SpacingMm;
    private DishItem? _bench;
    private InspectorViewModel? _inspector;
    private bool _generating;
    private SeedKind _seedKind;
    private bool _clearArmed;
    private CancellationTokenSource? _disarm;
    private readonly AppSettings? _settings;
    private Palette _palette;

    public MainViewModel(AppSettings? settings = null)
    {
        _settings = settings;
        _palette = Palette.Named(settings?.Palette);
        _seedKind = settings?.SeedKind ?? default;
        Slots = new ObservableCollection<SlotViewModel>(
            Enumerable.Range(0, Rack.SlotCount).Select(i => new SlotViewModel(i)));
        GenerateCommand = new RelayCommand(() => _ = GenerateAsync(), () => CanGenerate);
        ClearRackCommand = new RelayCommand(ArmOrClear, () => _rack.Occupied > 0 && !_generating);
        MoreSeedsCommand = new RelayCommand(() => SeedCount++, () => SeedCount < PlacementSettings.MaxSeeds);
        FewerSeedsCommand = new RelayCommand(() => SeedCount--, () => SeedCount > PlacementSettings.MinSeeds);
        CloseInspectorCommand = new RelayCommand(() => Inspector = null);
        SeedLooks = SeedKindInfo.All.Select(info => new SeedLookOption(info)).ToList();
        foreach (var look in SeedLooks) look.IsSelected = look.Info.Kind == _seedKind;
    }

    /// <summary>Raised after a dish is placed on the rack, so the view can animate it there.</summary>
    public event EventHandler<DishShelvedEventArgs>? DishShelved;

    public event EventHandler<int>? DishRemoved;

    public event EventHandler? RackCleared;

    public ObservableCollection<SlotViewModel> Slots { get; }

    public RelayCommand GenerateCommand { get; }

    public RelayCommand ClearRackCommand { get; }

    public RelayCommand MoreSeedsCommand { get; }

    public RelayCommand FewerSeedsCommand { get; }

    public RelayCommand CloseInspectorCommand { get; }

    public IReadOnlyList<SeedLookOption> SeedLooks { get; }

    public SeedKind SeedKind
    {
        get => _seedKind;
        set
        {
            if (!Set(ref _seedKind, value)) return;
            foreach (var look in SeedLooks) look.IsSelected = look.Info.Kind == value;
            OnPropertyChanged(nameof(SeedLookName));
            SaveSettings();
        }
    }

    public string SeedLookName => SeedKindInfo.Of(SeedKind).Name;

    public IReadOnlyList<Palette> Palettes => Palette.All;

    public Palette SelectedPalette
    {
        get => _palette;
        set
        {
            if (value is null || !Set(ref _palette, value)) return;
            if (Application.Current is { } app) value.Apply(app);
            SaveSettings();
        }
    }

    private void SaveSettings()
    {
        if (_settings is null) return;
        _settings.Palette = _palette.Name;
        _settings.SeedKind = _seedKind;
        _settings.Save();
    }

    public int MinSeeds => PlacementSettings.MinSeeds;

    public int MaxSeeds => PlacementSettings.MaxSeeds;

    public double MinSpacing => PlacementSettings.MinSpacingMm;

    public double MaxSpacing => PlacementSettings.MaxSpacingMm;

    public int SeedCount
    {
        get => _seedCount;
        set
        {
            if (!Set(ref _seedCount, Math.Clamp(value, PlacementSettings.MinSeeds, PlacementSettings.MaxSeeds))) return;
            MoreSeedsCommand.Refresh();
            FewerSeedsCommand.Refresh();
        }
    }

    public double SpacingMm
    {
        get => _spacingMm;
        set
        {
            var snapped = Math.Round(value / PlacementSettings.SpacingStepMm) * PlacementSettings.SpacingStepMm;
            if (Set(ref _spacingMm, Math.Clamp(snapped, PlacementSettings.MinSpacingMm, PlacementSettings.MaxSpacingMm)))
            {
                OnPropertyChanged(nameof(SpacingText));
            }
        }
    }

    public string SpacingText => $"{SpacingMm:0.#} mm";

    public DishItem? Bench
    {
        get => _bench;
        private set => Set(ref _bench, value);
    }

    public InspectorViewModel? Inspector
    {
        get => _inspector;
        private set
        {
            if (Set(ref _inspector, value)) OnPropertyChanged(nameof(IsInspecting));
        }
    }

    public bool IsInspecting => Inspector is not null;

    public bool IsRackFull => _rack.IsFull;

    public bool CanGenerate => !_rack.IsFull && !_generating;

    public int Occupied => _rack.Occupied;

    public string RackStatus => _rack.Occupied switch
    {
        0 => "Rack empty",
        Rack.SlotCount => "Rack full",
        var n => $"{n} of {Rack.SlotCount} slots filled",
    };

    public string GenerateHint => _rack.IsFull ? "Remove a dish or clear the rack to continue" : "Space";

    public bool IsClearArmed
    {
        get => _clearArmed;
        private set
        {
            if (Set(ref _clearArmed, value)) OnPropertyChanged(nameof(ClearLabel));
        }
    }

    public string ClearLabel => IsClearArmed
        ? $"Clear {_rack.Occupied} {(_rack.Occupied == 1 ? "dish" : "dishes")}?"
        : "Clear rack";

    public async Task GenerateAsync()
    {
        if (!CanGenerate) return;
        _generating = true;
        RefreshRack();
        try
        {
            var settings = new PlacementSettings(SeedCount, SpacingMm);
            var layout = await Task.Run(() => SeedSampler.Generate(settings));
            var slot = _rack.Shelve(layout);
            var dish = new DishItem(_rack, slot);
            Slots[slot].Dish = dish;
            Bench = dish;
            DishShelved?.Invoke(this, new DishShelvedEventArgs(dish, slot));
        }
        finally
        {
            _generating = false;
            RefreshRack();
        }
    }

    public void Inspect(SlotViewModel slot)
    {
        if (slot.Dish is { } dish) Inspector = new InspectorViewModel(dish, RemoveInspected);
    }

    public void RemoveSlot(int slot)
    {
        if (_rack.Remove(slot) is null) return;
        Slots[slot].Dish = null;
        if (Bench?.Slot == slot) Bench = null;
        if (Inspector?.Slot == slot) Inspector = null;
        DishRemoved?.Invoke(this, slot);
        RefreshRack();
    }

    private void RemoveInspected()
    {
        if (Inspector is { } inspector) RemoveSlot(inspector.Slot);
    }

    private async void ArmOrClear()
    {
        if (IsClearArmed)
        {
            _disarm?.Cancel();
            IsClearArmed = false;
            ClearRack();
            return;
        }

        IsClearArmed = true;
        _disarm = new CancellationTokenSource();
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(3), _disarm.Token);
            IsClearArmed = false;
        }
        catch (OperationCanceledException)
        {
        }
    }

    private void ClearRack()
    {
        _rack.Clear();
        foreach (var slot in Slots) slot.Dish = null;
        Bench = null;
        Inspector = null;
        RackCleared?.Invoke(this, EventArgs.Empty);
        RefreshRack();
    }

    private void RefreshRack()
    {
        OnPropertyChanged(nameof(IsRackFull));
        OnPropertyChanged(nameof(CanGenerate));
        OnPropertyChanged(nameof(Occupied));
        OnPropertyChanged(nameof(RackStatus));
        OnPropertyChanged(nameof(GenerateHint));
        OnPropertyChanged(nameof(ClearLabel));
        GenerateCommand.Refresh();
        ClearRackCommand.Refresh();
    }
}
