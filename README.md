# Forest Mushrooms — Timberborn 1.1

An experimental native Timberborn mod. Old, moist forest interiors produce the game's existing **Mushroom** good as a second tree yield, without replacing resin, syrup, fruit, chestnuts or logs.

**Status:** built against the locally installed Timberborn 1.1 assemblies. Automated simulation checks pass. Gameplay, UI appearance, navigation and the procedural models still require an in-game playtest; this is not yet a verified Workshop release.

## Install

1. Download `ForestMushrooms-0.1.15.zip` from [GitHub Releases](https://github.com/bednarpe/ForestMushrooms/releases/latest) and extract it into `Documents/Timberborn/Mods`. The result should be `Mods/ForestMushrooms/version-1.1/manifest.json`. Local builds produce the same ZIP in `dist`.
2. Enable **Harmony** (2.4.1 or newer), **Mod Settings** (1.1.1.0 or newer), and **Forest Mushrooms** in Timberborn's mod manager, then restart the game.
3. Leaf Coats is optional. If used, enable it and its own dependencies too. This mod declares Leaf Coats as an optional dependency so its patches load afterwards.
4. Try a separate test settlement first. No installation, game settings, or existing saves are changed by the build script.

When upgrading, remove the old `ForestMushrooms` mod folder before extracting the new package so retired blueprints are not left behind. Version 0.1.4 removed the Hollow Trunk Fermenter prototype; dismantle any built prototypes before upgrading saves that contain them.

## In-game settings

Open **Menu → Mods → Forest Mushrooms → Settings**. Requires the **Mod Settings** helper mod, the same UI used by Faster Underwater Movement. Values are saved by that framework and apply across settlements.

| Setting | Default | Unit |
|---|---:|---|
| Minimum tree age | 40 | simulation days |
| Consecutive moist days | 8 | simulation days |
| Average initial spawn delay | 16 | eligible days; exponential mean |
| Minimum regrowth delay | 12 | eligible days |
| Maximum regrowth delay | 20 | eligible days |
| Picking time | 0.5 | working hours |
| Dead-tree mushroom lifetime | 2 | simulation days |

Values can be changed from the main menu or during a settlement. Age/moisture thresholds affect growth on the next tick; they do not remove an already ripe crop. Already sampled initial/regrowth waits are preserved, including across save/load. Updated random-delay settings apply when a new wait is sampled (new trees, cutting, picking, or losing a ripe crop). Picking time applies when the next picking job begins. Developer diagnostics display the current configured thresholds.

Existing saved settings keep their chosen values after updating. Use the settings reset controls to adopt the shorter defaults. Dead-tree lifetime changes apply to existing dead crops; zero removes them immediately. The elapsed death timer is saved with the tree.

Day values accept 0–10000; picking accepts 0.01–240 hours. A zero initial delay removes the random wait; equal regrowth bounds produce a fixed wait. Crossing regrowth bounds adjusts the other bound to match. Negative values are clamped and non-finite values reset to defaults. Use the framework's reset control to restore defaults. Four side-adjacent trees and succulent exclusion remain fixed rules.

## Rules and defaults

| Rule | Behavior |
|---|---|
| Tree age | At least 40 days since creation or last cutting. Age includes seedling growth; the host must also be mature. |
| Forest interior | Living trees occupy the four side-adjacent XY squares (north, south, east and west) at the **same base elevation**. Diagonals are ignored. Species may differ; saplings count as neighbors, but succulents, stumps and dead trees do not. |
| Moisture | Uses the game's irrigated-soil state, not standing water. Requires 8 consecutive moist days. Even a brief dry-state event resets the streak. |
| Initial crop | After age/moisture conditions are met, a saved exponential wait averaging 16 eligible days. 63.2% appear within 16 eligible days, 95% within 48, and 99% within about 73.7. No hard maximum. |
| Harvest | One raw Mushroom per crop; 0.5 working hours to pick it. Only beaver workers in gathering flags/huts with the `Gatherable` resource group. Bots, tappers, foresters and lumberjacks cannot pick it. |
| Regrowth | Uniform random 12–20 eligible days after picking: average 16. |
| Drying | On living trees the unpicked mushroom disappears immediately. A lost ripe crop starts a new regrowth wait. Existing countdowns pause, and all regrowth must pass the new 8-day moisture streak. |
| Contamination | Any contamination reported by the game at the ground tile removes ripe crops, including those on dead trees, and blocks harvesting and developer spawning. It resets the clean-moisture streak and samples a fresh initial growth wait once per contamination episode. Growth resumes only after the soil clears and the moisture requirement is met again. Tree age is preserved. |
| Forest edge | On living trees losing a neighbor removes the crop on the next simulation tick and pauses growth. Harvest completion also rechecks neighbors, so there is no stale harvest. |
| Cutting/death | Cutting resets age and the initial wait; ripe mushrooms on dead trees remain pickable for the configured lifetime (default 2 days), even if dry or missing neighbors. No new mushrooms grow on dead trees. A replacement tree starts fresh. |
| Save/load | Age, wet streak, remaining wait, crop presence, harvest count and PRNG state are saved per tree. Reloading does not reroll the timer. |

On an existing save, trees have no historical age/moisture data for this mod: tracking begins at zero when the mod is first enabled. Existing trees are never assumed to have already satisfied 40 days.

Timers run in simulation time, respect game speed and pause, and progress only under valid conditions. Species carrying the game's `TreeComponentSpec` are covered, including compatible mod trees, with succulents, mangroves and trees requiring standing water explicitly excluded from both mushroom hosts and neighboring trees.

## Factions and fermentation

- **All factions, including new mod factions:** automatically receive access to raw mushrooms, fermented mushrooms and the original fermented-mushroom nutrition need (+2 well-being when satisfied). Existing faction collections are preserved. Factions without a fermenter can still obtain the food through other means.
- **Fermenters:** automatically gain the mushroom recipe, preserving existing recipes without adding duplicates. Detection requires a standard `ManufactorySpec` and either an internal building name ending in `Fermenter` (before the faction suffix), or an existing recipe ID starting with `Fermented`. This includes Iron Teeth and Leaf Coats. Display language does not affect detection.
- **Folktails:** adds raw/fermented mushrooms and the fermented-mushroom nutrition need, plus a mushroom-only **Fermenter** in Food. It reuses the vanilla Fermenter model, cost and power requirements.
- **Gathering:** any faction's flag/hut using the standard `GathererFlag` component and `Gatherable` resource group can gather mushrooms with beaver workers. This includes both Leaf Coats gathering buildings.

Buildings using entirely custom gathering/production code, or fermenters with neither recognizable template names nor fermentation recipe IDs, may still require an explicit compatibility patch. The mod does not add a new fermenter building to arbitrary factions; Folktails retains its supplied fermenter.

The existing recipe is reused: **4 Mushrooms → 16 Fermented Mushrooms in 2 working hours**. No raw-food consumption is introduced. No Leaf Coats models, textures or code are redistributed.

## Developer controls

The normal tree panel shows only the harvestable count (when ready), whether mushrooms are growing, and the number of neighboring trees out of four. Detailed thresholds, causes, random waits and the dead-tree expiry timer are shown only in developer mode. Select a tree to see its mushroom status. Enable Timberborn developer mode with **Shift + Alt + Z** to show:

- Tree age, consecutive moist days, neighbor count, saved random countdown and a conditional earliest-crop estimate.
- **Mature mushrooms now:** skips age and moisture history, but requires a living mature tree, moist soil and all four side-adjacent trees. This changes only the selected tree.
- **Simulate picking (no goods):** clears the crop and samples the next 12–20-day regrowth timer.

Mushrooms also have small brown procedural fruiting bodies on the trunk while ripe. Debug controls are unavailable outside developer mode. The ordinary tree products retain their own timers and yields.

## Build and test

Requires .NET SDK 8, Timberborn **1.1**, and the installed Harmony and Mod Settings mod DLLs. Use `-ModSettingsPath` for a nonstandard Mod Settings Scripts directory. There are no NuGet package dependencies. Game assemblies are referenced locally and never copied into the package.

```powershell
./build.ps1
# Or explicit installation paths:
./build.ps1 -TimberbornPath 'D:\SteamLibrary\steamapps\common\Timberborn' -HarmonyPath 'D:\path\to\0Harmony.dll'
```

`build.ps1` compiles the DLL, runs the C# simulation tests, derives faction fermenter blueprints from the installed game's `Blueprints.zip`, and produces the zip in `dist`. It uses a project-local SDK if present, otherwise `dotnet` from PATH. `-SkipRestore` is available after an initial successful restore. Rebuild against new game versions; compatibility with 1.0 is not claimed.

The `tests` executable covers age/watering boundaries, drought, edges, death/cutting, single harvests, 12–20-day regrowth, timestep invariance, serialized random-state continuity, and 100,000-sample distribution checks. See [playtest checklist](docs/PLAYTEST.md) for the remaining live checks.

## Implementation notes

A blueprint eligibility filter adds `ForestMushroomHostSpec` to trees other than succulents. `ForestMushroom` decorates that marker and owns a separately named `Yielder`. A dedicated initializer avoids changing `GatherableSpec`, preserving the existing tree product. Gathering hooks append the extra yielder and permit its good in the workplace inventory. Guards revalidate crop availability and employment before walking and before completing harvest; tree yield reservations remain shared with normal harvest/cutting work.

The registry indexes trees by map coordinates. Moisture uses `DryObject` events, and simulation updates use `IDayNightCycle.FixedDeltaTimeInHours`. The game saves state under `Bedna.ForestMushrooms/StateV1`. Version-dependent Harmony patches are deliberately small and explicit.

## References

- [Official game wiki: Mushrooms](https://timberborn.wiki.gg/wiki/Mushroom)
- [Official game wiki: Fermented Mushrooms](https://timberborn.wiki.gg/wiki/Fermented_Mushrooms)
- [Official modding documentation](https://github.com/mechanistry/timberborn-modding/wiki)
- [Official blueprint modification rules](https://github.com/mechanistry/timberborn-modding/wiki/Blueprints)
- [Leaf Coats Workshop page](https://steamcommunity.com/workshop/filedetails/?id=3552203634)

Local API and blueprint inspection used Timberborn 1.1 and Leaf Coats 1.1.2.2 on September 5, 2026. The Leaf Coats integration paths and recipe fields were checked against its installed bundle. This is a separate mod, not an official Mechanistry or Leaf Coats release.
