# Live acceptance checklist

These tests remain **not run**. A successful compile and simulation tests do not establish gameplay compatibility.

Use a new test settlement with Harmony and Forest Mushrooms. Repeat the gathering/processing checks for Folktails, Iron Teeth, and Leaf Coats 1.1.2.2 plus its dependencies.

- Start/load the settlement with no configurator, template, Harmony or unknown-good errors in Player.log.
- Plant a moist five-tree cross on level ground, with no diagonal trees. In developer mode, force mushrooms on the mature center tree. Verify the four outer trees refuse the command.
- Check center-tree mushrooms are visible, selectable through the tree, and listed alongside existing sap/fruit. Ordinary yields must still work independently.
- Connect a staffed gatherer within range. Confirm the beaver reaches the center, spends time gathering, carries one Mushroom, deposits it, and it can be hauled to storage.
- Repeat with Leaf Coats' two-worker Gatherer Hut. No duplicate mushroom harvests, reservation deadlocks, or infinite retries.
- Staff a tapper, forester and lumberjack nearby: none should harvest mushrooms. A bot-only gathering flag/hut must not pick mushrooms.
- Force a crop then dry its square while a worker is walking and while it is picking. No mushroom should be awarded after drying, and reservations must clear.
- Restore water for 7.9 days: no crop. Pass 8 days and the saved remaining countdown: a crop can grow.
- Remove each of the four side-adjacent trees in turn: growth stops and ripe mushrooms disappear. Adding or removing diagonal trees has no effect. Trees on other elevations do not qualify it. Replace a cardinal neighbor with a succulent: it must not count.
- Cut the host: no mushroom or residual age survives. Replanting starts at age zero.
- Save midway through regrowth, reload, and compare remaining days. Save while a worker has reserved mushrooms, reload and finish gathering once.
- Confirm Folktails warehouse filters show both goods. Build Forest Fermenter, connect 50 hp, deliver four mushrooms and produce 16 fermented mushrooms. Verify eating and the new nutrition need.
- Confirm Leaf Coats' normal Fermenter retains its three original recipes and gains fermented mushrooms. Confirm there is no Hollow Trunk Fermenter in the build menu. Produce fermented mushrooms with each faction's fermenter, including Iron Teeth.
- Install another faction with a standard gatherer and fermenter: confirm mushroom gathering, storage filters, the fermented-mushroom recipe appearing exactly once, and the +2 nutrition benefit after eating. Confirm its existing recipes/needs remain available. Repeat after save/reload and a main-menu faction switch.
- Toggle dev mode with a tree selected: debug controls must appear/disappear. Simulated picking produces no physical goods.
- Pause the game: timers stop. Change game speed: timers advance in days, not wall-clock seconds.
- Run with Leaf Coats disabled: no missing optional-blueprint errors or Leaf Coats toolbar entry.
- Check large-forest performance, repeated scene loads and preview creation/deletion for visual resource leaks.

Record the game build, enabled mod list and Player.log with any failure before declaring the release gameplay-tested.

- Hover/select the Succulent planting tool: no duplicate-Yielder exception. Succulents must have no mushroom panel, yield, or registry entry.

- Open Forest Mushrooms Settings from the main menu and a settlement. Verify all seven timing fields, tooltips and reset control. Change thresholds and confirm the tree panel updates. Save/restart and verify settings persist. Confirm existing random waits stay unchanged while the next regrowth uses the new bounds. Confirm picking time changes affect new jobs and ordinary sap/fruit harvesting is unchanged.

- Verify mushroom stem bases touch the ground on trees at several terrain elevations and caps/stems have subtle mottling. Normal panel should show only crop status and neighbor count; developer mode retains detailed diagnostics.
- Kill a tree with ripe mushrooms while its soil is still wet. Harvest during the default two-day grace period; verify no regrowth. Repeat without harvesting: save/reload midway and verify expiry after the remaining time. Drying or removing neighbors after death must not remove the retained crop; cutting must remove it immediately. Test zero timeout and changing the timeout on an already dead tree.

- Select/preview a mangrove and a mod tree requiring standing water: neither should have mushrooms or mushroom controls, and neither should count as an eligible neighbor.
- Contaminate the ground under a ripe tree, a partially grown crop, and a dead tree during its grace period. Crops must disappear and pending harvests must award no goods. No growth or developer spawning is allowed while contaminated. Clear the soil and verify a full new moisture streak plus the restarted initial wait; save/reload must preserve that wait.
- With ripe mushrooms on trees at different elevations, lower the visible-level limit below each tree: its mushrooms must disappear immediately, including while paused. Raise the limit: unpicked mushrooms reappear. Growing a crop while its level is hidden must not show a floating model. Hiding levels must not change crops, timers or harvesting.
- Check ripe crops on multiple species, rotations and elevations: exactly two mushrooms form one tight pair near an inset tile corner, one full-sized and one 65% sized. Save/reload preserves its corner. Trigger tree model changes and toggle visible levels while paused; the pair must remain visible whenever eligible and its level is shown. Harvest/cut/delete the tree: no orphan visual remains.
- Save with many ripe crops, reload while paused, and inspect trees loaded in different orders: all eligible visible crops must have models before any simulation tick or focus change. Repeat with the level limit hiding some trees, then reveal them while paused.
- Load a Folktails settlement without Leaf Coats enabled: Food toolbar and Forest Fermenter preview/construction must load without missing Iron Teeth material exceptions.
- With only eligible mushroom trees in gathering range, verify a staffed flag/hut does not show Nothing to do in range while crops are growing, ripe or reserved by another worker. Growing crops must not be reserved or harvested early. Dry/contaminate the trees or remove required neighbors: if no other resources exist, the warning may return on the next worker search.
