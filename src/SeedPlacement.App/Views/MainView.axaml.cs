using System.ComponentModel;
using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Platform;
using Avalonia.Controls.Shapes;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using Avalonia.VisualTree;
using SeedPlacement.App.Controls;
using SeedPlacement.App.Export;
using SeedPlacement.App.ViewModels;
using SeedPlacement.Core;

namespace SeedPlacement.App.Views;

public sealed partial class MainView : UserControl
{
    private const double MaxTilt = 30;

    private MainViewModel? _vm;
    private CancellationTokenSource _shelving = new();
    private CancellationTokenSource _inspecting = new();

    private bool _dragging;
    private Point _dragFrom;
    private Vector _tilt;
    private Vector _tiltVelocity;
    private Vector _tiltTarget;
    private Vector _hoverTarget;
    private bool _tiltRunning;
    private Control? _inspectedFrom;
    private int _pendingRemovals;
    private bool _compact;
    private Thickness _safeArea;

    private const double CompactWidth = 760;

    public MainView()
    {
        InitializeComponent();
        var version = Assembly.GetExecutingAssembly().GetName().Version;
        VersionText.Text = $"Version {version?.ToString(3)}";
        AddHandler(KeyDownEvent, OnKeyDown, RoutingStrategies.Tunnel);
        AddHandler(DragDrop.DragOverEvent, OnDragOver);
        AddHandler(DragDrop.DragLeaveEvent, OnDragLeave);
        AddHandler(DragDrop.DropEvent, OnDrop);
    }

    private TopLevel? Host => TopLevel.GetTopLevel(this);

    // Android draws the app behind the status and navigation bars, so content keeps clear of them by hand.
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        if (Host?.InsetsManager is { } insets) insets.SafeAreaChanged += OnSafeAreaChanged;
        RefreshSafeArea();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        if (Host?.InsetsManager is { } insets) insets.SafeAreaChanged -= OnSafeAreaChanged;
        base.OnDetachedFromVisualTree(e);
    }

    private void OnSafeAreaChanged(object? sender, SafeAreaChangedArgs e) => RefreshSafeArea();

    // Avalonia's own figure comes back in physical pixels on some screens, so Android measures the bars itself.
    private void RefreshSafeArea()
    {
        var safe = Phone.SafeArea?.Invoke() ?? (Phone.SafeArea is null ? Host?.InsetsManager?.SafeAreaPadding : null);
        if (safe is { } area && area != _safeArea) ApplySafeArea(area);
    }

    private void ApplySafeArea(Thickness safe)
    {
        _safeArea = safe;
        MainContent.Margin = safe;
        InspectorContent.Margin = InspectorMargin();
        SizeInspector(Bounds.Size);
    }

    private Thickness InspectorMargin()
    {
        var edge = _compact ? 12 : 40;
        return new Thickness(edge + _safeArea.Left, edge + _safeArea.Top, edge + _safeArea.Right, edge + _safeArea.Bottom);
    }

    protected override void OnSizeChanged(SizeChangedEventArgs e)
    {
        base.OnSizeChanged(e);
        RefreshSafeArea();
        ApplyLayout(e.NewSize.Width < CompactWidth, e.NewSize);
        SizeInspector(e.NewSize);
    }

    private void SizeInspector(Size client)
    {
        var dish = _compact
            ? Math.Max(0, CompactDish(client))
            : Math.Clamp(Math.Min(client.Height - 200, client.Width - 380 - 52 - 160), 320, 620);
        InspectStage.Width = dish;
        InspectStage.Height = dish;
    }

    // Short phones give up some dish so the card below still has room for a few coordinate rows.
    private double CompactDish(Size client)
    {
        var usable = client.Height - _safeArea.Top - _safeArea.Bottom - 24;
        return Math.Min(client.Width - 24, Math.Min(usable * 0.42, usable - 500));
    }

    private void ApplyLayout(bool compact, Size size)
    {
        StageArea.Height = compact ? Math.Round((size.Height - _safeArea.Top - _safeArea.Bottom) * 0.5) : double.NaN;
        if (compact == _compact) return;
        _compact = compact;

        MainContent.ColumnDefinitions = compact ? new ColumnDefinitions("*") : new ColumnDefinitions("320,*");
        MainContent.RowDefinitions = compact ? new RowDefinitions("Auto,*") : new RowDefinitions("*");
        Grid.SetColumn(StageArea, compact ? 0 : 1);
        Grid.SetRow(PanelBorder, compact ? 1 : 0);
        StageArea.Margin = compact ? new Thickness(12, 8, 12, 4) : new Thickness(36, 26, 36, 30);
        PanelBorder.BorderThickness = compact ? new Thickness(0, 1, 0, 0) : new Thickness(0, 0, 1, 0);
        PanelDock.Margin = compact ? new Thickness(16, 14, 16, 10) : new Thickness(28, 32, 28, 24);
        PanelHeader.IsVisible = !compact;
        SpaceHint.IsVisible = !compact;
        BenchCaption.Height = compact ? double.NaN : 66;
        BenchDetail.TextWrapping = compact ? TextWrapping.Wrap : TextWrapping.NoWrap;
        BenchDetail.FontSize = compact ? 13 : 14;
        BenchEmptyText.FontSize = compact ? 30 : 18;
        SetTouchSliders(compact);

        Move(PlaceBlock, compact ? PanelDock : PanelStack, compact ? Dock.Top : null, compact ? 0 : 3);
        PlaceBlock.Margin = compact ? new Thickness(0, 0, 0, 14) : default;
        Move(PanelFooter, compact ? PanelStack : PanelDock, compact ? null : Dock.Bottom, compact ? PanelStack.Children.Count : 1);

        InspectorContent.ColumnDefinitions = compact ? new ColumnDefinitions("*") : new ColumnDefinitions("Auto,380");
        // On a phone the inspector fills the screen and the coordinate list takes whatever height is left.
        InspectorContent.RowDefinitions = compact ? new RowDefinitions("Auto,*") : new RowDefinitions("*");
        InspectorContent.VerticalAlignment = compact ? Avalonia.Layout.VerticalAlignment.Stretch : Avalonia.Layout.VerticalAlignment.Center;
        InspectCard.VerticalAlignment = compact ? Avalonia.Layout.VerticalAlignment.Top : Avalonia.Layout.VerticalAlignment.Center;
        InspectorContent.Margin = InspectorMargin();
        Grid.SetColumn(InspectCard, compact ? 0 : 1);
        Grid.SetRow(InspectCard, compact ? 1 : 0);
        InspectStage.Margin = compact ? new Thickness(0, 0, 0, 12) : new Thickness(0, 0, 52, 0);
        InspectCard.Padding = compact ? new Thickness(18) : new Thickness(26);
        SeedRowsScroll.MaxHeight = compact ? double.PositiveInfinity : 330;
    }

    private void SetTouchSliders(bool touch)
    {
        var resources = PanelStack.Resources;
        if (!touch)
        {
            foreach (var key in new[] { "SliderHorizontalThumbWidth", "SliderHorizontalThumbHeight", "SliderThumbCornerRadius", "SliderTrackThemeHeight" })
                resources.Remove(key);
            return;
        }
        resources["SliderHorizontalThumbWidth"] = 30.0;
        resources["SliderHorizontalThumbHeight"] = 30.0;
        resources["SliderThumbCornerRadius"] = new CornerRadius(15);
        resources["SliderTrackThemeHeight"] = 6.0;
    }

    private static void Move(Control control, Panel to, Dock? dock, int index)
    {
        if (control.Parent is Panel from) from.Children.Remove(control);
        if (dock is { } side) DockPanel.SetDock(control, side);
        else control.ClearValue(DockPanel.DockProperty);
        to.Children.Insert(Math.Min(index, to.Children.Count), control);
    }

    private DishView BenchDishView => this.FindControl<DishView>("BenchDish")!;

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        if (_vm is not null)
        {
            _vm.DishShelved -= OnDishShelved;
            _vm.DishRemoved -= OnDishRemoved;
            _vm.ExportRequested -= OnExportRequested;
            _vm.SaveRunFileRequested -= OnSaveRunFile;
            _vm.OpenRunFileRequested -= OnOpenRunFile;
            _vm.RunOpened -= OnRunOpened;
            _vm.PropertyChanged -= OnViewModelChanged;
        }
        _vm = DataContext as MainViewModel;
        if (_vm is not null)
        {
            _vm.DishShelved += OnDishShelved;
            _vm.DishRemoved += OnDishRemoved;
            _vm.ExportRequested += OnExportRequested;
            _vm.SaveRunFileRequested += OnSaveRunFile;
            _vm.OpenRunFileRequested += OnOpenRunFile;
            _vm.RunOpened += OnRunOpened;
            _vm.PropertyChanged += OnViewModelChanged;
            if (_vm.Bench is { } restored) ShowBenchDish(restored);
        }
    }

    private void ShowBenchDish(DishItem dish)
    {
        BenchEmptyText.IsVisible = false;
        BenchDishView.IsEmptyGhost = false;
        BenchDishView.Reveal = double.MaxValue;
        BenchDishView.SeedKind = dish.Kind;
        BenchDishView.Layout = dish.Layout;
        BenchHost.Opacity = 1;
        CaptionTape.Opacity = 1;
    }

    private void OnViewModelChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (_vm is null) return;
        if (e.PropertyName == nameof(MainViewModel.Bench) && _vm.Bench is null)
        {
            _shelving.Cancel();
            BenchDishView.Layout = null;
            BenchDishView.IsEmptyGhost = true;
            BenchEmptyText.IsVisible = true;
        }
        else if (e.PropertyName == nameof(MainViewModel.Inspector))
        {
            if (_vm.Inspector is null) HideInspector();
            else ShowInspector();
        }
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (_vm is null || Host?.FocusManager?.GetFocusedElement() is TextBox) return;
        if (ConfirmLayer.IsVisible)
        {
            if (e.Key == Key.Escape) CloseConfirm(false);
            e.Handled = true;
            return;
        }
        switch (e.Key)
        {
            case Key.Escape when _vm.IsInspecting:
                _vm.CloseInspectorCommand.Execute(null);
                e.Handled = true;
                break;
            case Key.Delete when _vm.Inspector is { } inspector:
                inspector.RemoveCommand.Execute(null);
                e.Handled = true;
                break;
            case Key.Space or Key.Enter when !_vm.IsInspecting:
                _vm.GenerateCommand.Execute(null);
                e.Handled = true;
                break;
        }
    }

    private async void OnDishShelved(object? sender, DishShelvedEventArgs e)
    {
        _shelving.Cancel();
        _shelving = new CancellationTokenSource();
        var cancel = _shelving.Token;

        var bench = BenchDishView;
        var slotDish = FindSlotPart<DishView>(e.Slot, "SlotDish");
        var glow = FindSlotPart<Ellipse>(e.Slot, "SlotGlow");
        var badge = FindSlotPart<Control>(e.Slot, "SlotBadge");
        if (slotDish is not null) slotDish.Opacity = 0;
        if (badge is not null) badge.Opacity = 0;

        BenchEmptyText.IsVisible = false;
        bench.IsEmptyGhost = false;
        bench.Reveal = 0;
        bench.SeedKind = e.Dish.Kind;
        bench.Layout = e.Dish.Layout;
        CaptionTape.Opacity = 0;

        var end = DishView.RevealEnd(e.Dish.Layout.Seeds.Count);
        await Motion.Tween(BenchHost, TimeSpan.FromMilliseconds(260), t =>
        {
            BenchHost.Opacity = t;
            BenchHost.RenderTransform = new ScaleTransform(0.95 + 0.05 * t, 0.95 + 0.05 * t);
        }, cancel);
        await Motion.Tween(bench, TimeSpan.FromMilliseconds(end * 330), t => bench.Reveal = t * end, cancel, Ease.Linear);
        await Motion.Delay(TimeSpan.FromMilliseconds(260), cancel);

        if (slotDish is null) return;
        await FlyToSlot(e, slotDish, cancel);
        slotDish.Opacity = 1;
        if (badge is not null) badge.Opacity = 1;

        if (glow is not null)
        {
            await Motion.Tween(glow, TimeSpan.FromMilliseconds(900), t => glow.Opacity = Math.Sin(Math.PI * Math.Min(1, t * 1.6)) * (1 - t * 0.3), cancel);
            glow.Opacity = 0;
        }
    }

    private async Task FlyToSlot(DishShelvedEventArgs e, DishView target, CancellationToken cancel)
    {
        var from = BoundsIn(BenchDishView, FlightLayer);
        var to = BoundsIn(target, FlightLayer);
        if (from is not { } start || to is not { } end) return;

        var ghost = new DishView { Layout = e.Dish.Layout, SeedKind = e.Dish.Kind, Width = start.Width, Height = start.Height };
        Canvas.SetLeft(ghost, start.X);
        Canvas.SetTop(ghost, start.Y);
        FlightLayer.Children.Add(ghost);

        _ = Motion.Tween(CaptionTape, TimeSpan.FromMilliseconds(260), t =>
        {
            CaptionTape.Opacity = Math.Min(1, t * 2);
            var s = 1.12 - 0.12 * t;
            CaptionTape.RenderTransform = new TransformGroup
            {
                Children = { new ScaleTransform(s, s), new RotateTransform(-1.5) },
            };
        }, cancel);
        await Motion.Tween(ghost, TimeSpan.FromMilliseconds(720), t =>
        {
            var lift = 1 + Math.Sin(Math.PI * t) * 0.08;
            var w = Lerp(start.Width, end.Width, t) * lift;
            var h = Lerp(start.Height, end.Height, t) * lift;
            var cx = Lerp(start.Center.X, end.Center.X, t);
            var cy = Lerp(start.Center.Y, end.Center.Y, t) - Math.Sin(Math.PI * t) * 40;
            ghost.Width = w;
            ghost.Height = h;
            Canvas.SetLeft(ghost, cx - w / 2);
            Canvas.SetTop(ghost, cy - h / 2);
        }, cancel, Ease.InOutCubic);

        FlightLayer.Children.Remove(ghost);
    }

    // The slot has already emptied by the time this runs, so the animation needs its own copy of the dish.
    private async void OnDishRemoved(object? sender, DishRemovedEventArgs e)
    {
        var slotButton = FindSlotPart<Button>(e.Slot, null);
        if (slotButton is null || BoundsIn(slotButton, FlightLayer) is not { } rect) return;
        var delay = TimeSpan.FromMilliseconds(45 * _pendingRemovals++);

        var ghost = new DishView { Layout = e.Dish.Layout, SeedKind = e.Dish.Kind, Width = rect.Width, Height = rect.Height };
        Canvas.SetLeft(ghost, rect.X);
        Canvas.SetTop(ghost, rect.Y);
        FlightLayer.Children.Add(ghost);
        await Motion.Tween(ghost, TimeSpan.FromMilliseconds(320), t =>
        {
            var s = 1 - 0.12 * t;
            ghost.Opacity = 1 - t;
            ghost.RenderTransform = new TransformGroup
            {
                Children = { new ScaleTransform(s, s), new TranslateTransform(0, -18 * t) },
            };
        }, CancellationToken.None, Ease.OutCubic, delay);
        FlightLayer.Children.Remove(ghost);
        _pendingRemovals = Math.Max(0, _pendingRemovals - 1);
    }

    private void OnCodeKeyDown(object? sender, KeyEventArgs e)
    {
        if (_vm is null) return;
        if (e.Key == Key.Enter) _vm.PlaceFromCodeCommand.Execute(null);
        else if (e.Key == Key.Escape) _vm.IsCodeEntryOpen = false;
        else return;
        e.Handled = true;
    }

    private async void OnExportRequested(object? sender, EventArgs e)
    {
        if (_vm is null || Host is not { } host) return;
        var name = $"Seed run {DateTime.Now:yyyy-MM-dd HHmm}";
        using var picture = RenderPicture();
        var rack = _vm.Rack;
        ExportFile[] files =
        [
            new($"{name} seeds.csv", "text/csv", s =>
            {
                using var writer = new StreamWriter(s);
                writer.Write(RunCsv.Write(rack));
            }),
            new($"{name} templates.pdf", "application/pdf", s => TemplatePdf.Write(s, rack)),
            new($"{name} rack.png", "image/png", s => picture.Save(s, new PngBitmapEncoderOptions())),
        ];

        try
        {
            if (Phone.SaveToDownloads is { } save)
            {
                var place = await save(name, files);
                ShowExportStatus($"Saved the seed positions, rack picture and printable templates to {place}.");
                return;
            }

            var folders = await host.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                Title = "Choose where to save the run",
                AllowMultiple = false,
            });
            if (folders.Count == 0) return;
            var folder = folders[0];
            foreach (var file in files) await WriteFile(folder, file.Name, file.Write);
            ShowExportStatus($"Saved the seed positions, rack picture and printable templates to {folder.Name}.");
            await host.Launcher.LaunchFileAsync(folder);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            ShowExportStatus($"Couldn't save the run: {ex.Message}");
        }
    }

    private static readonly FilePickerFileType RunFileType = new("Seed run")
    {
        Patterns = ["*.seedrun"],
        MimeTypes = ["application/octet-stream", "application/json"],
    };

    private async void OnSaveRunFile(object? sender, EventArgs e)
    {
        if (_vm is null || Host is not { } host) return;
        var file = await host.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Save the run",
            SuggestedFileName = _vm.RunFileName(),
            DefaultExtension = MainViewModel.RunFileExtension,
            FileTypeChoices = [RunFileType],
        });
        if (file is null) return;
        try
        {
            await using var stream = await file.OpenWriteAsync();
            await using var writer = new StreamWriter(stream);
            await writer.WriteAsync(_vm.RunFileText());
            ShowExportStatus($"Saved the run as {file.Name}.");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            ShowExportStatus("Couldn't save the run there. To put it on a phone, save it on this computer, then drag the file into the phone's Download folder.");
        }
    }

    private async void OnOpenRunFile(object? sender, EventArgs e)
    {
        if (_vm is null || Host is not { } host) return;
        var files = await host.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Open a run",
            AllowMultiple = false,
            FileTypeFilter = [RunFileType],
        });
        if (files.Count > 0) await OpenRunAsync(files[0]);
    }

    private async Task OpenRunAsync(IStorageItem item)
    {
        if (_vm is null || item is not IStorageFile file) return;
        string text;
        try
        {
            // Read on the UI thread: a file dragged from a phone in Explorer is a COM stream that throws on any other.
            await using var stream = await file.OpenReadAsync();
            using var reader = new StreamReader(stream);
            text = reader.ReadToEnd();
        }
        catch (Exception ex)
        {
            ShowExportStatus($"Couldn't open the run: {ex.Message}");
            return;
        }

        if (RunRecord.FromJson(text) is null)
        {
            ShowExportStatus($"{file.Name} isn't a seed run.");
            return;
        }
        if (_vm.Occupied > 0 && !await ConfirmReplace(file.Name, _vm.Occupied)) return;
        ShowExportStatus(_vm.OpenRunFile(text) ? $"Opened {file.Name}." : $"{file.Name} isn't a seed run.");
    }

    private TaskCompletionSource<bool>? _confirm;

    private Task<bool> ConfirmReplace(string name, int dishes)
    {
        _confirm?.TrySetResult(false);
        _confirm = new TaskCompletionSource<bool>();
        ConfirmText.Text = $"Opening {name} replaces the {dishes} {(dishes == 1 ? "dish" : "dishes")} on the rack. " +
            "Save the current run first if you still need it.";
        ConfirmLayer.IsVisible = true;
        return _confirm.Task;
    }

    private void CloseConfirm(bool replace)
    {
        ConfirmLayer.IsVisible = false;
        _confirm?.TrySetResult(replace);
        _confirm = null;
    }

    private void OnConfirmCancel(object? sender, RoutedEventArgs e) => CloseConfirm(false);

    private void OnConfirmReplace(object? sender, RoutedEventArgs e) => CloseConfirm(true);

    private static bool IsRunFile(DragEventArgs e) =>
        e.DataTransfer.TryGetFiles() is [IStorageFile file]
        && file.Name.EndsWith($".{MainViewModel.RunFileExtension}", StringComparison.OrdinalIgnoreCase);

    private void OnDragOver(object? sender, DragEventArgs e)
    {
        var accept = IsRunFile(e) && _vm?.IsInspecting == false && !ConfirmLayer.IsVisible;
        e.DragEffects = accept ? DragDropEffects.Copy : DragDropEffects.None;
        DropHint.IsVisible = accept;
    }

    private void OnDragLeave(object? sender, DragEventArgs e) => DropHint.IsVisible = false;

    private async void OnDrop(object? sender, DragEventArgs e)
    {
        DropHint.IsVisible = false;
        if (IsRunFile(e) && _vm?.IsInspecting == false && e.DataTransfer.TryGetFiles() is [var file])
            await OpenRunAsync(file);
    }

    private void OnRunOpened(object? sender, EventArgs e)
    {
        if (_vm?.Bench is { } bench) ShowBenchDish(bench);
    }

    private static async Task WriteFile(IStorageFolder folder, string name, Action<Stream> write)
    {
        var file = await folder.CreateFileAsync(name) ?? throw new IOException($"Couldn't create {name}.");
        await using var stream = await file.OpenWriteAsync();
        write(stream);
    }

    // Scaled while drawing at 96 dpi: rendering straight to a high-DPI bitmap misplaces anything drawn after a clip.
    private RenderTargetBitmap RenderPicture()
    {
        const double scale = 2;
        var area = _compact && BoundsIn(PanelBorder, Root) is { } panel
            ? new Rect(0, _safeArea.Top, Root.Bounds.Width, panel.Y - _safeArea.Top)
            : new Rect(Root.Bounds.Size);
        var bitmap = new RenderTargetBitmap(new PixelSize((int)(area.Width * scale), (int)(area.Height * scale)));
        using var context = bitmap.CreateDrawingContext();
        using (context.PushTransform(Matrix.CreateTranslation(-area.X, -area.Y) * Matrix.CreateScale(scale, scale)))
        {
            var brush = new VisualBrush(Root) { Stretch = Stretch.None, AlignmentX = AlignmentX.Left, AlignmentY = AlignmentY.Top };
            context.DrawRectangle(brush, null, new Rect(Root.Bounds.Size));
        }
        return bitmap;
    }

    private void ShowExportStatus(string text)
    {
        ExportStatus.Text = text;
        ExportStatus.IsVisible = true;
    }

    private void OnLabelKeyDown(object? sender, KeyEventArgs e)
    {
        if (sender is not TextBox box || e.Key is not (Key.Enter or Key.Escape)) return;
        if (e.Key == Key.Escape) BindingOperations.GetBindingExpressionBase(box, TextBox.TextProperty)?.UpdateTarget();
        Root.Focus();
        e.Handled = true;
    }

    private void OnRootPressed(object? sender, PointerPressedEventArgs e)
    {
        if (Host?.FocusManager?.GetFocusedElement() is TextBox) Root.Focus();
    }

    private async void OnCreditClick(object? sender, RoutedEventArgs e)
    {
        if (Host is { } host) await host.Launcher.LaunchUriAsync(new Uri("https://github.com/Nic-Richard"));
    }

    private void OnSeedLookClick(object? sender, RoutedEventArgs e)
    {
        if (_vm is not null && sender is Control { DataContext: SeedLookOption look }) _vm.SeedKind = look.Info.Kind;
    }

    private void OnSlotClick(object? sender, RoutedEventArgs e)
    {
        if (sender is not Control { DataContext: SlotViewModel slot } control) return;
        _inspectedFrom = control;
        _vm?.Inspect(slot);
    }

    private void ShowInspector()
    {
        _inspecting.Cancel();
        _inspecting = new CancellationTokenSource();
        var cancel = _inspecting.Token;
        _tilt = _tiltVelocity = _tiltTarget = _hoverTarget = default;
        ApplyTilt();
        InspectDish.HighlightedSeed = -1;
        CopyButton.Content = "Copy";
        InspectorLayer.IsVisible = true;

        _ = Motion.Tween(InspectorLayer, TimeSpan.FromMilliseconds(260), t =>
        {
            InspectorLayer.Opacity = t;
            MainContent.Effect = new BlurEffect { Radius = 14 * t };
        }, cancel);
        InspectTilt.RenderTransform = null;
        InspectorLayer.UpdateLayout();
        var from = _inspectedFrom is { } origin ? BoundsIn(origin, Root) : null;
        var to = BoundsIn(InspectTilt, Root);
        var startScale = from is { } f && to is { } g && g.Width > 0 ? f.Width / g.Width : 0.82;
        var offset = from is { } a && to is { } b ? a.Center - b.Center : default;
        _ = Motion.Tween(InspectTilt, TimeSpan.FromMilliseconds(460), t =>
        {
            InspectTilt.Opacity = Math.Min(1, t * 3);
            var s = Lerp(startScale, 1, t);
            InspectTilt.RenderTransform = new TransformGroup
            {
                Children = { new ScaleTransform(s, s), new TranslateTransform(offset.X * (1 - t), offset.Y * (1 - t)) },
            };
        }, cancel, Ease.OutCubic);
        _ = Motion.Tween(InspectCard, TimeSpan.FromMilliseconds(360), t =>
        {
            InspectCard.Opacity = t;
            InspectCard.RenderTransform = new TranslateTransform(24 * (1 - t), 0);
        }, cancel, delay: TimeSpan.FromMilliseconds(80));
    }

    private async void HideInspector()
    {
        _inspecting.Cancel();
        _inspecting = new CancellationTokenSource();
        await Motion.Tween(InspectorLayer, TimeSpan.FromMilliseconds(180), t =>
        {
            InspectorLayer.Opacity = 1 - t;
            MainContent.Effect = new BlurEffect { Radius = 14 * (1 - t) };
        }, _inspecting.Token);
        if (_vm?.IsInspecting == true) return;
        InspectorLayer.IsVisible = false;
        MainContent.Effect = null;
    }

    private void OnCloseInspector(object? sender, RoutedEventArgs e) => _vm?.CloseInspectorCommand.Execute(null);

    private void OnBackdropPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.Source == InspectorLayer || e.Source == InspectorContent) _vm?.CloseInspectorCommand.Execute(null);
    }

    private void OnCardPressed(object? sender, PointerPressedEventArgs e) => e.Handled = true;

    private async void OnCopyCode(object? sender, RoutedEventArgs e)
    {
        if (_vm?.Inspector is not { } inspector || Host?.Clipboard is not { } clipboard) return;
        await clipboard.SetTextAsync(inspector.Dish.Code);
        CopyButton.Content = "Copied";
    }

    private void OnSeedRowEntered(object? sender, PointerEventArgs e)
    {
        if (sender is Control { DataContext: SeedRow row }) InspectDish.HighlightedSeed = row.Index;
    }

    private void OnSeedRowExited(object? sender, PointerEventArgs e) => InspectDish.HighlightedSeed = -1;

    private void OnStagePressed(object? sender, PointerPressedEventArgs e)
    {
        _dragging = true;
        _dragFrom = e.GetPosition(InspectStage);
        _tiltTarget = _tilt;
        e.Pointer.Capture(InspectStage);
        e.Handled = true;
        RunTilt();
    }

    private void OnStageMoved(object? sender, PointerEventArgs e)
    {
        var at = e.GetPosition(InspectStage);
        if (_dragging)
        {
            var delta = at - _dragFrom;
            _dragFrom = at;
            _tiltTarget = Clamp(_tiltTarget + new Vector(delta.X * 0.35, -delta.Y * 0.35));
        }
        else
        {
            var size = InspectStage.Bounds.Size;
            var nx = (at.X / Math.Max(1, size.Width) - 0.5) * 2;
            var ny = (at.Y / Math.Max(1, size.Height) - 0.5) * 2;
            _hoverTarget = new Vector(nx * 7, -ny * 7);
        }
        RunTilt();
    }

    private void OnStageReleased(object? sender, PointerReleasedEventArgs e)
    {
        _dragging = false;
        e.Pointer.Capture(null);
        RunTilt();
    }

    private void OnStageExited(object? sender, PointerEventArgs e)
    {
        _hoverTarget = default;
        RunTilt();
    }

    private void RunTilt()
    {
        if (_tiltRunning || Host is not { } host) return;
        _tiltRunning = true;
        host.RequestAnimationFrame(TiltFrame);
    }

    private void TiltFrame(TimeSpan now)
    {
        var target = _dragging ? _tiltTarget : _hoverTarget;
        _tiltVelocity = (_tiltVelocity + (target - _tilt) * 0.12) * 0.74;
        _tilt = Clamp(_tilt + _tiltVelocity);
        ApplyTilt();

        var settled = !_dragging && (target - _tilt).Length < 0.02 && _tiltVelocity.Length < 0.02;
        if (settled || !InspectorLayer.IsVisible)
        {
            _tiltRunning = false;
            return;
        }
        Host?.RequestAnimationFrame(TiltFrame);
    }

    private void ApplyTilt()
    {
        InspectDish.RenderTransform = new Rotate3DTransform(-_tilt.Y, _tilt.X, 0, 0, 0, 0, 1400);
        InspectDish.Tilt = new Vector(_tilt.X / MaxTilt, _tilt.Y / MaxTilt);
    }

    private static Vector Clamp(Vector v) =>
        new(Math.Clamp(v.X, -MaxTilt, MaxTilt), Math.Clamp(v.Y, -MaxTilt, MaxTilt));

    private T? FindSlotPart<T>(int slot, string? name) where T : Control =>
        RackSlots.ContainerFromIndex(slot)?.GetVisualDescendants().OfType<T>()
            .FirstOrDefault(c => name is null || c.Name == name);

    private static Rect? BoundsIn(Visual visual, Visual target)
    {
        if (visual.TransformToVisual(target) is not { } matrix) return null;
        var rect = new Rect(visual.Bounds.Size);
        var a = rect.TopLeft.Transform(matrix);
        var b = rect.BottomRight.Transform(matrix);
        return new Rect(a, b);
    }

    private static double Lerp(double a, double b, double t) => a + (b - a) * t;
}
