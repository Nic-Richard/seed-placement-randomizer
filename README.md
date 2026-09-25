# Seed Placement Randomizer

A desktop app that removes unconscious bias from seed bioassays. It places seeds at random inside a
petri dish, then puts each dish in a random slot on a 10-slot rack. It runs offline on Windows and
macOS and is built to be shown on a projector as well as used at the bench.

![The rack with seven randomized dishes](docs/screenshots/rack.png)

## What it does

- Places 1–10 seeds in a 90 mm dish, at least 15–20 mm apart and 10 mm from the rim
- Shelves each new dish in a random empty slot on a 2 × 5 rack, numbered from the top left
- Gives every dish a layout code, such as `AQSR-5NA0`, that recreates it exactly
- Labels each dish on a strip of tape. Click the label to rename a dish, such as "Control A"
- Opens any dish in an inspector with numbered seeds and their coordinates in millimetres
- Draws seeds as cucumber, wheat, lettuce, radish or sunflower, to scale. This only changes how they
  look
- Removes single dishes or clears the rack for the next run

![The dish inspector](docs/screenshots/inspector.png)

## How the placement stays random

Seed positions are drawn uniformly over the area of the dish. A layout is kept only if every seed
meets the spacing and rim rules; otherwise the whole layout is redrawn. Rejecting whole layouts
rather than nudging individual seeds makes every valid arrangement equally likely. The shortcut of
placing seeds one by one and skipping bad spots quietly favours some patterns over others.

For very crowded settings, such as 10 seeds at 20 mm, whole-layout redraws become too slow. There
the app switches to a Markov chain that samples the same distribution. The inspector names the
method used for each dish.

Each new dish goes into a uniformly random empty slot, so every assignment of dishes to slots is
equally likely.

Layout codes use a PRNG specified in the source (xoshiro256**) rather than the runtime's, so a code
produces the same dish on any machine and any future version. The test suite checks the spacing
and rim rules, uniformity (chi-square), agreement between the two samplers, and fixed outputs for
known codes.

## Running it

Download the latest build from the Releases page. Windows may show a SmartScreen prompt for an
unsigned app: choose **More info → Run anyway**.

### Building from source

Requires the [.NET 10 SDK](https://dotnet.microsoft.com/download).

```sh
dotnet run --project src/SeedPlacement.App
dotnet test
```

Publish a self-contained single-file build:

```sh
dotnet publish src/SeedPlacement.App -c Release -r win-x64 -o publish/win-x64
dotnet publish src/SeedPlacement.App -c Release -r osx-arm64 -o publish/osx-arm64
```

## Repository structure

```text
src/SeedPlacement.Core/        Geometry, PRNG, sampling, layout codes, rack assignment
src/SeedPlacement.App/         Avalonia desktop app: views, view models, drawing, motion
tests/SeedPlacement.Tests/     Tests for Core, including statistical checks
tools/SeedPlacement.Snapshots/ Renders the app offscreen to refresh the screenshots
assets-src/                    Script that generates the seed, paper, bench grain and icon images
docs/screenshots/              README screenshots
```

Regenerate the images with `python assets-src/generate_assets.py` (numpy, scipy, Pillow), and the
screenshots with `dotnet run --project tools/SeedPlacement.Snapshots`.

## License

[PolyForm Noncommercial 1.0.0](LICENSE.md). Anyone can use, share and modify it for noncommercial
purposes, including research, teaching and personal use. Selling it or using it commercially needs
permission.

## Credits

Made by **[Nic Richard](https://github.com/Nic-Richard)**. Commissioned for a seed germination bioassay.

Set in [Atkinson Hyperlegible](https://www.brailleinstitute.org/freefont/) (SIL Open Font License), with
labels in [Permanent Marker](https://fonts.google.com/specimen/Permanent+Marker) (Apache License 2.0). Built with
[Avalonia](https://avaloniaui.net).
