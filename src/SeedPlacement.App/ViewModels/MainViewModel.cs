using System.Collections.ObjectModel;
using Avalonia;
using SeedPlacement.App.Controls;
using SeedPlacement.App.Styles;
using SeedPlacement.Core;

namespace SeedPlacement.App.ViewModels;

public sealed record DishShelvedEventArgs(DishItem Dish, int Slot);

public sealed record DishRemovedEventArgs(DishItem Dish, int Slot);

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
    private readonly RunStore? _store;
    private Palette _palette;
    private string _codeEntry = "";
    private string? _codeError;
    private bool _isCodeEntryOpen;

    public MainViewModel(AppSettings? settings = null, RunStore? store = null)
    {
        _store = store;
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
        PlaceFromCodeCommand = new RelayCommand(() => _ = PlaceFromCodeAsync(), () => CanGenerate);
        ToggleCodeEntryCommand = new RelayCommand(() => IsCodeEntryOpen = !IsCodeEntryOpen);
        ExportCommand = new RelayCommand(() => ExportRequested?.Invoke(this, EventArgs.Empty), () => _rack.Occupied > 0);
        RestoreRun();
    }

    /// <summary>Raised after a dish is placed on the rack, so the view can animate it there.</summary>
    public event EventHandler<DishShelvedEventArgs>? DishShelved;

    public event EventHandler<DishRemovedEventArgs>? DishRemoved;

    public event EventHandler? ExportRequested;

    public ObservableCollection<SlotViewModel> Slots { get; }

    public RelayCommand GenerateCommand { get; }

    public RelayCommand ClearRackCommand { get; }

    public RelayCommand MoreSeedsCommand { get; }

    public RelayCommand FewerSeedsCommand { get; }

    public RelayCommand CloseInspectorCommand { get; }

    public RelayCommand PlaceFromCodeCommand { get; }

    public RelayCommand ToggleCodeEntryCommand { get; }

    public RelayCommand ExportCommand { get; }

    public Rack Rack => _rack;

    public bool IsCodeEntryOpen
    {
        get => _isCodeEntryOpen;
        set
        {
            if (!Set(ref _isCodeEntryOpen, value)) return;
            CodeError = null;
        }
    }

    public string CodeEntry
    {
        get => _codeEntry;
        set
        {
            if (Set(ref _codeEntry, value ?? "")) CodeError = null;
        }
    }

    public string? CodeError
    {
        get => _codeError;
        private set
        {
            if (Set(ref _codeError, value)) OnPropertyChanged(nameof(HasCodeError));
        }
    }

    public bool HasCodeError => CodeError is not null;

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

    public Task GenerateAsync() => PlaceAsync(null);

    /// <summary>Rebuilds the exact dish a code describes and shelves it in a random empty slot.</summary>
    public async Task PlaceFromCodeAsync()
    {
        if (!LayoutCode.TryParse(CodeEntry, out var code))
        {
            CodeError = "That isn't a layout code. Codes look like RS8V-BTJ0.";
            return;
        }
        await PlaceAsync(code);
        CodeEntry = "";
        IsCodeEntryOpen = false;
    }

    private async Task PlaceAsync(LayoutCode? code)
    {
        if (!CanGenerate) return;
        _generating = true;
        RefreshRack();
        try
        {
            var settings = new PlacementSettings(SeedCount, SpacingMm);
            var layout = await Task.Run(() => code is { } c ? SeedSampler.Generate(c) : SeedSampler.Generate(settings));
            var slot = _rack.Shelve(layout);
            var dish = new DishItem(_rack, slot, SaveRun);
            Slots[slot].Dish = dish;
            Bench = dish;
            SaveRun();
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
        if (Slots[slot].Dish is not { } dish || _rack.Remove(slot) is null) return;
        Slots[slot].Dish = null;
        if (Bench?.Slot == slot) Bench = null;
        if (Inspector?.Slot == slot) Inspector = null;
        SaveRun();
        DishRemoved?.Invoke(this, new DishRemovedEventArgs(dish, slot));
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
        var removed = Slots.Where(s => s.Dish is not null).Select(s => new DishRemovedEventArgs(s.Dish!, s.Index)).ToList();
        _rack.Clear();
        foreach (var slot in Slots) slot.Dish = null;
        Bench = null;
        Inspector = null;
        SaveRun();
        foreach (var args in removed) DishRemoved?.Invoke(this, args);
        RefreshRack();
    }

    private void RestoreRun()
    {
        if (_store?.Load() is not { } record) return;
        try
        {
            record.RestoreInto(_rack);
        }
        catch (Exception e) when (e is ArgumentException or FormatException)
        {
            _rack.Clear();
            return;
        }
        for (var slot = 0; slot < Rack.SlotCount; slot++)
        {
            if (_rack.Slots[slot] is not null) Slots[slot].Dish = new DishItem(_rack, slot, SaveRun);
        }
        Bench = Slots.Select(s => s.Dish).OfType<DishItem>().MaxBy(d => d.Number);
        RefreshRack();
    }

    private void SaveRun() => _store?.Save(_rack);

    private void RefreshRack()
    {
        OnPropertyChanged(nameof(IsRackFull));
        OnPropertyChanged(nameof(CanGenerate));
        OnPropertyChanged(nameof(Occupied));
        OnPropertyChanged(nameof(RackStatus));
        OnPropertyChanged(nameof(ClearLabel));
        GenerateCommand.Refresh();
        ClearRackCommand.Refresh();
        PlaceFromCodeCommand.Refresh();
        ExportCommand.Refresh();
    }
}
