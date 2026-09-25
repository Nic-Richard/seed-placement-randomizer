using System.Diagnostics;
using Avalonia;
using Avalonia.Headless;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using SeedPlacement.App;
using SeedPlacement.App.Controls;
using SeedPlacement.App.Styles;
using SeedPlacement.App.ViewModels;
using SeedPlacement.App.Views;

// Renders the app offscreen through a scripted session and saves each state as a PNG.
//   dotnet run --project tools/SeedPlacement.Snapshots                   README screenshots
//   dotnet run --project tools/SeedPlacement.Snapshots -- --options DIR  every palette and seed look
var options = args.Length > 0 && args[0] == "--options";
var outDir = Path.GetFullPath(options ? args.ElementAtOrDefault(1) ?? "snapshots" : args.ElementAtOrDefault(0) ?? "docs/screenshots");
Directory.CreateDirectory(outDir);

AppBuilder.Configure<App>()
    .UseSkia()
    .UseHarfBuzz()
    .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false })
    .SetupWithoutStarting();

var vm = new MainViewModel();
var window = new MainWindow { DataContext = vm, Width = 1320, Height = 880 };
window.Show();
vm.SelectedPalette = Palette.Default;

Pump(300);
if (!options) Save("empty");
for (var i = 0; i < 7; i++) await Generate(2800);
var labels = new[] { "Control A", "Control B", "Treated A" };
foreach (var (slot, label) in vm.Slots.Where(s => s.HasDish).OrderBy(s => s.Dish!.Number).Zip(labels))
{
    slot.Dish!.Label = label;
}
Pump(100);

if (!options)
{
    Save("rack");
    Inspect();
    Save("inspector");
    return;
}

var n = 1;
foreach (var palette in Palette.All)
{
    vm.SelectedPalette = palette;
    Pump(200);
    var slug = $"{n++}-{palette.Name.ToLowerInvariant().Replace(' ', '-')}";
    Save(Path.Combine("colours", slug));
}

vm.SelectedPalette = Palette.Default;
foreach (var kind in SeedKindInfo.All)
{
    vm.SeedKind = kind.Kind;
    Inspect();
    Save(Path.Combine("seeds", kind.AssetKey));
    CloseInspector();
}

void Inspect()
{
    vm.Inspect(vm.Slots.First(s => s.HasDish));
    Pump(700);
}

void CloseInspector()
{
    vm.CloseInspectorCommand.Execute(null);
    Pump(400);
}

void Pump(int ms)
{
    // The headless render timer only advances when ticked; real time keeps tweens on their schedule.
    var clock = Stopwatch.StartNew();
    while (clock.ElapsedMilliseconds < ms)
    {
        AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        Dispatcher.UIThread.RunJobs();
        Thread.Sleep(8);
    }
}

async Task Generate(int settleMs)
{
    var generating = vm.GenerateAsync();
    var clock = Stopwatch.StartNew();
    while (!generating.IsCompleted && clock.ElapsedMilliseconds < 5000) Pump(10);
    await generating;
    Pump(settleMs);
}

void Save(string name)
{
    Pump(50);
    var path = Path.Combine(outDir, name + ".png");
    Directory.CreateDirectory(Path.GetDirectoryName(path)!);
    using var file = File.Create(path);
    window.CaptureRenderedFrame()?.Save(file, new PngBitmapEncoderOptions());
    Console.WriteLine(path);
}
