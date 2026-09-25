using System.ComponentModel;
using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.VisualTree;
using SeedPlacement.App.Controls;
using SeedPlacement.App.ViewModels;

namespace SeedPlacement.App.Views;

public sealed partial class MainWindow : Window
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

    public MainWindow()
    {
        InitializeComponent();
        var version = Assembly.GetExecutingAssembly().GetName().Version;
        VersionText.Text = $"Version {version?.ToString(3)}";
        SizeInspector(ClientSize);
        AddHandler(KeyDownEvent, OnKeyDown, RoutingStrategies.Tunnel);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == ClientSizeProperty && InspectStage is not null) SizeInspector(ClientSize);
    }

    private void SizeInspector(Size client)
    {
        var dish = Math.Clamp(Math.Min(client.Height - 200, client.Width - 380 - 52 - 160), 320, 620);
        InspectStage.Width = dish;
        InspectStage.Height = dish;
    }

    private DishView BenchDishView => this.FindControl<DishView>("BenchDish")!;

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        if (_vm is not null)
        {
            _vm.DishShelved -= OnDishShelved;
            _vm.PropertyChanged -= OnViewModelChanged;
        }
        _vm = DataContext as MainViewModel;
        if (_vm is not null)
        {
            _vm.DishShelved += OnDishShelved;
            _vm.PropertyChanged += OnViewModelChanged;
        }
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
        if (_vm is null || FocusManager?.GetFocusedElement() is TextBox) return;
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
            case Key.F11:
                WindowState = WindowState == WindowState.FullScreen ? WindowState.Normal : WindowState.FullScreen;
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
        var badge = FindSlotPart<Border>(e.Slot, "SlotBadge");
        if (slotDish is not null) slotDish.Opacity = 0;
        if (badge is not null) badge.Opacity = 0;

        BenchEmptyText.IsVisible = false;
        bench.IsEmptyGhost = false;
        bench.Reveal = 0;
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

        var ghost = new DishView { Layout = e.Dish.Layout, Width = start.Width, Height = start.Height };
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

    // Enter keeps the new label; Escape puts the old one back. Either way the label stops being edited.
    private void OnLabelKeyDown(object? sender, KeyEventArgs e)
    {
        if (sender is not TextBox box || e.Key is not (Key.Enter or Key.Escape)) return;
        if (e.Key == Key.Escape) BindingOperations.GetBindingExpressionBase(box, TextBox.TextProperty)?.UpdateTarget();
        Root.Focus();
        e.Handled = true;
    }

    private void OnRootPressed(object? sender, PointerPressedEventArgs e)
    {
        if (FocusManager?.GetFocusedElement() is TextBox) Root.Focus();
    }

    private async void OnCreditClick(object? sender, RoutedEventArgs e) =>
        await Launcher.LaunchUriAsync(new Uri("https://github.com/Nic-Richard"));

    private void OnSeedLookClick(object? sender, RoutedEventArgs e)
    {
        if (_vm is not null && sender is Control { DataContext: SeedLookOption look }) _vm.SeedKind = look.Info.Kind;
    }

    private void OnSlotClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Control { DataContext: SlotViewModel slot }) _vm?.Inspect(slot);
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
        _ = Motion.Tween(InspectTilt, TimeSpan.FromMilliseconds(420), t =>
        {
            InspectTilt.Opacity = t;
            var s = 0.82 + 0.18 * t;
            InspectTilt.RenderTransform = new ScaleTransform(s, s);
        }, cancel);
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
        if (_vm?.Inspector is not { } inspector || Clipboard is not { } clipboard) return;
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
        if (_tiltRunning) return;
        _tiltRunning = true;
        RequestAnimationFrame(TiltFrame);
    }

    // A damped spring toward the drag target, or back toward the hover lean once released.
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
        RequestAnimationFrame(TiltFrame);
    }

    private void ApplyTilt()
    {
        InspectDish.RenderTransform = new Rotate3DTransform(-_tilt.Y, _tilt.X, 0, 0, 0, 0, 1400);
        InspectDish.Tilt = new Vector(_tilt.X / MaxTilt, _tilt.Y / MaxTilt);
    }

    private static Vector Clamp(Vector v) =>
        new(Math.Clamp(v.X, -MaxTilt, MaxTilt), Math.Clamp(v.Y, -MaxTilt, MaxTilt));

    private T? FindSlotPart<T>(int slot, string name) where T : Control =>
        RackSlots.ContainerFromIndex(slot)?.GetVisualDescendants().OfType<T>().FirstOrDefault(c => c.Name == name);

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
