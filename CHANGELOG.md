# Changelog

## Unreleased

- Set up the repository: solution with Core, App, Tests and Snapshots projects on .NET 10, shared
  build settings with warnings as errors, and editor, attribute and ignore rules.
- Added agent guidance and the project roadmap.
- Added seed placement: uniform exact rejection sampling over the dish, a hard-disk Markov chain for
  crowded settings, and a specified xoshiro256** PRNG so results reproduce across platforms.
- Added eight-character layout codes that encode the sampler seed, seed count and spacing, and
  recreate a dish exactly.
- Added the 10-slot rack with uniform random slot assignment, numbered from the top left.
- Added tests for spacing and rim rules at every setting, uniformity, agreement between samplers,
  layout codes, rack behaviour and a golden layout.
- Added the Avalonia desktop app with a lab-bench look: seed and spacing controls, a bench dish, and
  a chrome wire rack with taped slot numbers.
- Seeds now drop into the dish one at a time, and each dish flies to its slot on the rack.
- Added the dish inspector with drag-to-tilt, numbered seeds, coordinates in mm, the layout code and
  a mark for the top of the dish.
- Clearing the rack now takes a second click to confirm.
- Added a script that generates the seed, filter paper, bench and icon images, and a tool that
  renders screenshots offscreen.
- Windows builds now publish as a single self-contained exe.
- Added seed looks for cucumber, wheat, lettuce, radish and sunflower, drawn to scale. They change how
  seeds look, never where they go.
- All colours now come from a palette. Added five: Bench Black, Slate, Soapstone, Greenhouse and a
  light Clean Room.
- The bench texture is now a neutral grain laid over the palette's bench colour.
- The inspector no longer shows the top-of-dish arrow or the drag hint.
- The snapshot tool can now render every palette and seed look from one session with `--options`.
- Dish number badges now stay legible on light palettes, and the inspector no longer clips the dish
  shadow.
- The interface now uses Atkinson Hyperlegible throughout, with sentence-case labels in place of
  small capitals and monospace.
- The accent is now masking-tape cream, and the bench caption is a strip of tape that reads, for
  example, "Dish 7 goes in slot 2".
- Slot numbers on the rack and the slot in the inspector are now torn tape labels.
- The seed count now uses a joined stepper, and Generate is renamed Place a dish.
- The credit now reads Nic Richard and opens github.com/Nic-Richard.
- Dishes now carry a tape label written in marker. Click it on the bench or in the inspector to
  rename the dish; clearing it restores "Dish N". Enter keeps the change and Escape undoes it.
- Rack dishes now show their tape label in place of the numbered badge.
- Added the PolyForm Noncommercial 1.0.0 license.
- The snapshot tool now renders a single main-screen image per palette.
- Added a bench colour dropdown with all five palettes. The app now remembers the bench colour and
  seed drawing between launches.
