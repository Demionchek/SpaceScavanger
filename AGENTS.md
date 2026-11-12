# AGENTS.md — SpaceScavanger

Unity 2D game: space scavenging / flight + walkable ship interior, with quests, trade,
crafting, reputation, saves and a story campaign. DI via **VContainer**, dialogue via
**Yarn Spinner**, tweening via **DOTween**, text via **TMPro + TMPEffects**.

## Conventions

- **All player-facing text is in English.** Russian is only used when talking to the user,
  and in `[Tooltip]`/`[Header]`/`Debug.Log`, which only the developer ever reads. Key names in
  hints and the legend go through `InputKeyLabels` (`_Core/Input/`), which builds them from the
  binding path: `GetBindingDisplayString` takes names from the OS keyboard layout and prints "У"
  instead of "E" on a Russian layout.
- **Minimal comments.** Only where the code is genuinely non-obvious; fewer is better.
- Match the surrounding code style: `_camelCase` private fields, `sealed` classes,
  explicit `Game.*` namespaces, one type per file (nested helper save-DTOs excepted).
- Scene/prefab/`.asset` wiring is done by the user in the Unity Editor. When a change
  needs new serialized fields or scene references, say exactly what to set up there.

## On-disk layout

Scripts are **not** directly under `Assets/` — they live under `Assets/Scripts/`:

```
Assets/
  Scripts/
    _Core/        Core.asmdef           — Contracts/, DI/, Debug/, EventBus/, Input/,
                                          Pause/, StateMachine/ (+ States/), World/
    _Data/        Data.asmdef           — ScriptableObject configs & event/effect types
    Gameplay.Flight/  Gameplay.Flight.asmdef — ship flight, zones, enemies, loot, races
    Gameplay.Ship/    Gameplay.Ship.asmdef   — interior: character, interactables
    Gameplay.Shared/  Gameplay.Shared.asmdef — services (resources, items, quests, …)
    Narrative/    Narrative.asmdef      — Yarn dialogue presentation (speaker talk
                                          animation + optional speaker icons)
    Save/         Save.asmdef
    UI/           UI.asmdef             — all HUD/windows
  Configs/        ScriptableObject data (see below)
  Scenes/         SpaceScene.unity, ShipInterior.unity, PrisonScene.unity
  Prefabs/  Sprites/  Settings/  Fonts/
```

Each feature folder holds its own `.asmdef` of the same name. If a path like
`Assets/<Feature>/Foo.cs` fails, try `Assets/Scripts/<Feature>/Foo.cs` or glob the
filename before concluding the file is gone.

`Assets/Configs/` subfolders: `AI`, `Fractions`, `Installers`, `ItemDefinitions`,
`Narrative`, `Quests`, `Recipies`, `Saves`, `Trader`, `UI`, `Upgrades`, `Weapons`, `Zones`.

## DI / composition

- `Assets/Scripts/_Core/DI/GameLifetimeScope.cs` is the root scope: `EventBus`,
  `PlayerContext`/`GameContext`, the three game states + `GameStateMachine`,
  `PauseService`, `ModeLock`, input/HUD/debug entry points. **Every debug key is off until
  `DebugMode` (`_Core/Debug/`) is switched on with F1**, which banners "Debug keys ON": they are
  1 / 2 / 3 (jump straight into interior / flight / boarding), 4 (force a random event), R
  (regenerate the sector) and F5 / F9 (save and load), plus the two Console loggers that print
  every resource and reputation change. Each of them asks `DebugMode.Enabled` first — none of it
  is `#if UNITY_EDITOR`, so it all ships in a build, and 1 or R pressed by accident in flight used
  to be enough to lose the sector. `DebugMode` is registered before them so its own key is read
  first. Registration order matters
  (`InputMapSwitcher` before `GameStateMachineBootstrap`). It then runs the
  `_featureInstallers` list — `ScriptableObjectInstaller` assets from `Assets/Configs/Installers/`:
  `SharedInstaller`, `FlightInstaller`, `ShipInstaller`, `NarrativeInstaller`,
  `UiInstaller`. **Installer code** sits in `<Feature>/DI/`, the **`.asset`** in
  `Assets/Configs/Installers/`.
- Per-object child scopes: `EnemyLifetimeScope`, `TraderLifetimeScope`,
  `QuestGiverLifetimeScope`, `DialogueNpcScope`, `RaceAiScope`, `ShipInteriorScope`.
  A scope injects **only what its `Configure` registers**: `autoInjectGameObjects` is empty on
  these prefabs, so a new `[Inject]` component on an enemy stays silently uninjected (null
  dependencies, no error) until it gets its own serialized field and `RegisterComponent` line —
  that is why the enemy explosion did nothing at first.
- See the `vcontainer-reference` skill before touching DI — notably the gotcha that new
  `*LifetimeScope.cs` files get wiped to an empty template on Unity import.
- Cross-system messaging goes through `EventBus` (`_Core/EventBus/`), not direct refs.

## Services (registered in `Gameplay.Shared/DI/SharedInstaller.cs`)

`ResourceService`, `ItemService`, `TradeService`, `QuestService`, `ReputationService`,
`RelationshipService`, `ShipStatsService`, `RecipeService`, `CraftingService`, `UpgradeService`,
`SoundService`, `JournalService`, `InteractionHintService`, `RepairService` — most also registered
`As<ISaveable>()`. The player's `Health` is not a service: the installer grabs it with
`RegisterComponentInHierarchy<Health>().AsSelf().As<ISaveable>()`, which is how `RepairService`
and `ShipInfoUI` reach the hull.
An upgrade is unique per ship: `UpgradeService.IsInstalled` blocks a second install, `CanCraft`
refuses an `UpgradeItemDefinition` that is already installed or already in the hold, and the
service saves its installed list (`upgrades`) without re-applying modifiers on load —
`ShipStatsService` restores those itself.
The hull is mended in the hold, not in flight: `RepairService` turns one unit of
`_repairResource` into `_hullPerRepairUnit` hull points (both on `SharedInstaller.asset`) and
always spends only what the hold actually carries, because a partial repair reads better than a
button that refuses to work. `ShipInfoUI` shows `Hull x/y` and the price on its `_repairButton`.
The same scrap pays the quota, and that competition is the point — nothing else makes scrap
feel scarce once the hook is good.
Entry points: `JournalRecorder`, `StoryDialogueService`, `SectorService`, `StartingResources`,
`SaveService`. `SectorService` counts sectors (1 at game start, +1 per wormhole jump),
publishes `SectorChangedEvent`, saves under `sector` and backs the Yarn function `sector()`.
A quest's goal is a `QuestGoalDefinition` asset that builds an `IQuestGoal` at runtime:
`CollectObjectsGoal`, `CollectAndDeliverGoal`, `CraftItemGoal`, `DestroyGroupGoal`, `RaceGoal`,
`TutorialCorridorGoal`, `QuotaGoal`, `NoticeGoal`. Three optional interfaces change how a goal
behaves: `ISaveableGoal` gives it its own slot in the save, `IQuestGoalStatus` lets it print its
own line in `QuestHudUI`, and `IDeliverableGoal` makes it something the
player hands over rather than something that completes by itself. **Every** goal implements
`IQuestGoalStatus` now, because a percentage never told the player how much was left to haul:
the counting ones print units (`Scrap 7/10`, `Pirates 2/3`, `3/10 collected`) and the two that
are simply done or not print a phrase. `QuestHudUI` keeps a fallback line for a goal without a
status, but it says "in progress" — no percentages anywhere.

The Corporation quota is a plain quest whose goal is a
`QuotaGoalDefinition`: a list of `QuotaDemand` lines plus a deadline in sectors, counted from the
sector it was issued in. That issuing sector is the goal's own saved progress (`ISaveableGoal`) —
nothing else knows it, and every line is derived from it: a line counts only once
`FromSector` is reached and then asks `Amount + AmountPerSector * (sector - FromSector)`, so a
growing quota needs nothing extra in the save. Depth adds **lines**, not only tonnage: scrap stays
flat at 180 and the squeeze arrives as iron (from sector **2**, so the second quota is already
what sends the player after the hook), gold (7), titanium (10) and uranium (13). That is why the numbers are small — a sector only holds so much of a rare rock, and a sector
with seven kinds of rock in it holds very little of each. The lines are resolved lazily, after
`LoadProgress`, since the issuing sector arrives from the save later than the constructor.
The officer's Yarn node starts the quest with `<<apply_effect QuestTrigger_Quota>>`
and prints the invoice through `{$quota_invoice}` / `{quota_sectors()}`, so the text never
hardcodes it; both come from the live goal (`IQuotaService.Demand` / `SectorsLeft`), never the
definition, which only knows the first sector's figures. `QuotaGoal.DemandText` is what builds
that sentence ("180 units of scrap, 60 of iron and 20 of gold"), because a dialogue line cannot
keep a list in step by hand. The invoice is a **variable** and not a function for a compiler
reason (see below), and `NarrativeDirector.PushQuotaInvoice` writes it twice: once before a
dialogue starts, and once after every `apply_effect` — the officer announces the quota in the
very node that issues it, so by the next line the invoice has to be the new one. Each line is measured against what a sector actually holds: at every
depth the worst line is about two sectors of scavenging against a three-sector deadline, and scrap
is the one resource the quota shares with hull repair, so it is deliberately not the whole sector.
Handing the load over is the player's move: with the full amount aboard,
`IQuotaService.CanDeliver` turns the intercom's lamp to a steady light (blinking still means a
call), its prompt to `_quotaPrompt`, and interacting raises a `ConfirmRequestedEvent` whose
confirm calls `TryDeliver()`. Nothing is taken automatically until the last allowed jump:
`TryEnforceDeadline()`, called by `WormholeComponent` **before** the jump is paid for, delivers
the quota if the hold is full and otherwise publishes `PlayerDestroyedEvent`, so the ship blows up
in space instead of behind the whiteout transition. Once a quota is delivered the next sector brings another: `QuotaService` sees the quest sitting in
`TurnedIn` on `SectorChangedEvent` and calls `IQuestService.Reset` before starting it again, so the
state of the quest is the only memory of the last delivery and nothing extra has to be saved.
Between the delivery and that jump the panel would otherwise go quiet, so `TryDeliver` starts a
second quest — `_nextQuotaNotice` on `SharedInstaller.asset`, a `NoticeGoalDefinition` whose goal
never completes and only prints its line — and `OnSectorChanged` drops it with
`IQuestService.Abandon` (removes an active quest without marking it turned in) the moment the real
quota arrives. The
officer explains it over the intercom through `IStoryDialogueService.Enqueue` — a call put in the
same queue as the story beats (so it rings the intercom, banners and saves like any other) without
advancing the one-shot story list. The quest starts regardless of whether the player answers;
the call only tells them the terms.
`PlayerDeathController` (Flight entry point)
answers `PlayerDestroyedEvent` — and the player `Health`'s own `Died`, which is what puts a game
over behind dying in combat — by spawning `_playerExplosionPrefab` from `FlightInstaller`, hiding
the ship and publishing `GameOverEvent` after `_playerDeathDelay`.

## Game modes

`GameStateMachine` with `SpaceFlightState`, `BoardingState`, `ShipInteriorState`.
Interior↔flight is an **additive** `ShipInterior` scene load with a whiteout transition
(`UI/ModeTransitionController.cs`) and a pause gate; `SpaceRoot`/`Content` are frozen and
regenerated. Zone content is generated/persisted by `ZoneSpawner` + `RandomZoneGenerator`.
How rich and how dangerous a sector is comes from `ZoneProgressionConfig`
(`Assets/Configs/Zones/`): tiers keyed by `FromSector` (the last matching one wins) override the
enemy prefab set, enemy count/chance and resource count. That set **accumulates** rather than
swaps: Scout (10 hp) and Gunner (25) from the start, Sniper (20) from sector 3, Lancer (45) from
5, and from there all four. Replacing the list instead left the opening sectors with a single
prefab, so every pirate in the first hours of the game was the same scout; depth now shows in the
mix, the group size and the count, not in which one kind you face. `EnemyCount` counts **groups**, not
enemies: each roll places a leader at `EnemyClearance` from everything else, then `GroupSize`-1
companions within `GroupRadius` of it, spaced by the much smaller `GroupMemberClearance` — so
squads stay tight while staying clear of landmarks. A companion measures its **own** leader by
that small radius and everything else by the larger of the two (`Gap`'s `ownLeaderIndex`): the
leader's `EnemyClearance` is wider than `GroupRadius`, so without that exception no companion
ever fit and every group was a single ship, whatever `GroupSize` said. Quest givers draw from `QuestPoolConfig` (`Assets/Configs/Quests/QuestPool.asset`): tiers keyed by
`FromSector` **accumulate**, so a deep sector still offers the early jobs. A giver with a
`_quest` set keeps that one; otherwise it resolves from the pool on first use, preferring a quest
already active (after a reload the same NPC must still take it back). Because one NPC can now
offer different jobs, the offer/turn-in Yarn nodes live on the `QuestDefinition`
(`OfferNode`/`TurnInNode`), and the node names on the prefab are only a fallback. So does the
`[E]` label: it prints the resolved quest's **`Title`** and falls back to the prefab's `_prompt`
only when nothing resolved, because a giver carrying `_prompt: Quest_Pirates` announced a pirate
hunt over what the pool had actually handed it — a cargo run. The resolve is cached even when it
comes back empty: the prompt asks ten times a second and `Resolve` sweeps the scene for the other
givers.
A sector holds `QuestGiverCount` of them (`ZoneConfig`), each with a different prefab out of
`QuestGiverPrefabs` — the race starter is one of those, so a sector can offer a race and a job —
and only the first is placed unconditionally. Two of them must not resolve to the same work:
a giver ignores whatever a neighbour has already resolved, and once the sector has no fresh job
left it offers a `QuestDefinition` marked `Repeatable` instead, calling `IQuestService.Reset` as
it picks it. The pool is four jobs deep, so without repeats the second giver would stand mute
after two sectors.
`JumpCost` on the tier replaces the wormhole
prefab's own cost when `ZoneSpawner` spawns it (0 keeps the prefab's); the hand-placed tutorial
wormhole keeps its prefab cost (3, against the 5 antimatter the player starts with). The ladder
is **10 / 12 / 14 / 15** and it is read in *patches*, not in round numbers: antimatter is one lot
in the lottery at weight 4, so a sector holds about 4.7 antimatter patches early and only 2.6
once seven kinds share twelve patches, i.e. ~49 / 42 / 34 / 27 units by tier. A jump is therefore
one patch at the start and nearly three at depth — **20% / 29% / 41% / 56%** of everything the
sector carries, against 8% → 37% before. The ramp still flattens where the resource list stops
growing (sector 8), since past that the supply is flat and a steeper toll would be pure squeeze;
what changed is the floor, so even the first jumps are a decision rather than small change. Keep
the deep numbers in view when tuning: past about half a sector's antimatter, a player who banks
nothing simply stops travelling. The hand-placed tutorial wormhole is **not** part of this ladder
and still costs 3, against the 5 antimatter the player starts with — the corridor does not hold
ten. Antimatter has no other sink, which is also why it sits at hook level 0 and
weight 4 and why this number is not allowed past a third or so of a sector: a player who never
mines it simply stops travelling (they do not die — `TryEnforceDeadline` only fires on a jump
attempt, so a stranded ship just sits there, which is worse than dying because nothing tells the
player what went wrong). The trader is the way out of that, and the **only one-way row in the
shop**: antimatter buys at 60 and its `SellPrice` is **0**, which `TraderUI` reads as "no sell
button at all" (`offer.SellPrice > 0` gates it). Both halves are deliberate. Buying is a bailout,
not a supply line — a deep jump bought with credits is 600, more than a second-rung schematic,
against the ~100 credits the same patch would fetch if it could be sold. And it cannot be sold,
because antimatter is the **most common** rock in the game (weight 4, hook 0): at any sane sell
price it would have been the cheapest money crop in the game and scrap would have stopped being
scarce. Fuel, not cargo. `ZoneSpawner` resolves the tier for the
sector the content was generated for and stores that number in its save, so loading can't pick
enemies from another tier. `_zoneSeed` on `FlightInstaller` is **0 = a fresh seed per run**:
`ZoneSpawner` only increments the seed on each regeneration, so a fixed start meant the sector
behind the first wormhole was byte-for-byte the same every new game. A save still carries its own
seed, so loading returns to the sector you left. The generator places points by rejection sampling: every object claims a clearance radius
(`ResourceClearance` / `EnemyClearance` / `LandmarkClearance` on `ZoneConfig`) and candidates
closer than the larger of the two radii — or inside `PlayerClearRadius` around the zone
centre, where a jump drops the player — are rejected. The wormhole, trader and the first quest
giver are placed first and never skipped; resources and enemies that find no room after
`PlacementAttempts` tries simply don't spawn. Counts and clearances are tuned against each other,
not alone: a 100x100 zone jams at roughly half its free area, so raising a count without cutting
the matching clearance only burns `PlacementAttempts` (80, up from 30 once space got crowded).
`LandmarkClearance` is the expensive one — every landmark clears a resource-free disc of its own
radius, which is why it sits at 12 rather than 16.

Resources come in patches, so `ResourceCount` counts **patches**, not rocks — the same trick as
`EnemyCount`. The first rock claims `ResourceClearance` (14, the gap *between* patches), then
`ResourceGroupSize`-1 more land within `ResourceGroupRadius` of it at `ResourceMemberClearance`
apart, measured against the first rock by that small radius (`Gap`'s `ownLeaderIndex` again).
One lot is drawn per patch and the whole patch uses it, so a patch is all scrap or all gold:
a field that reads as one find is worth flying to, while the same rocks spread evenly were noise.
16 patches of 2-5 rocks is ~48 rocks a sector, against ~35 scattered ones before.

What can be in a patch is `ZoneConfig.ResourceSpawns`: a prefab, the `FromSector` it starts
appearing in, and a `Weight` in the lottery (the generator pushes each entry into a list `Weight`
times and draws from it, so a weight of 1 against 4 really is four times rarer). One list holds
the whole progression — no per-tier resource arrays to keep in step with the quota — and because
`PrefabIndex` in the save is an index into it, new kinds go on the **end** or old saves change
their minds about what they contain. The order is scrap 4, iron 4, antimatter 4, gold 2 (sector 3),
titanium 2 (5), uranium 2 (8), spice 1 (8). Weights are not flavour: a deep sector splits the same
~12 patches between seven kinds, which is what every quota number is calculated against, and
antimatter carries weight 4 because it is jump fuel — a sector has to pay for more than one jump
out of itself.

The seven resource prefabs in `Assets/Prefabs/Resources/` differ only in `_resourceType`, yield,
`_requiredHookLevel` and `_color`: same sprite, same collider, same rigidbody. Yields fall as value
rises — scrap 10-20, iron 5-10, gold 3-8, titanium 2-6, uranium 1-4, antimatter 1-5, spice 1-3 —
and the trader's price ladder follows (scrap 10, iron 20, titanium 30, uranium 40, gold 80,
spice 120). `_color` is the resource icon's own hue lifted to full brightness (the same recipe
reproduces the scrap prefab's existing tint), which is how a green uranium rock and a violet spice
rock read as the thing their icon shows.

Ship upgrades come in **families of three**, each level its own `UpgradeItemDefinition` in
`Assets/Configs/Upgrades/` with one `RequiredUpgrade` step below it, so a family is a ladder and
not a shelf: Thruster (`ThrustForce` ×1.25/×1.25/×1.3), Autocannon (`Damage` ×1.3/×1.3/×1.35),
Autoloader (`FireRate` ×1.2/×1.2/×1.25), Plating (`MaxHealth` ×1.3/×1.3/×1.35) and Radar dish
(`RadarRange` ×1.4/×1.4/×1.35). Multipliers compound, so a full family is roughly double its stat.
Each level wears the icon its stat already uses on the ship screen (`element_0036_lightning`,
`Damage_icon`, `element_0037_fire`, `element_0040_hit_points`, `radar_icon`), so the workbench and
the ship screen name the same thing the same way. The third rung of every family costs gold,
titanium or uranium, which is what makes hook 2-4 worth having once the quota is paid; the trader
sells the schematics for 120 / 400 / 900 credits and the whole catalogue is on offer from sector 1,
aspirational rows included. Every one of these items and recipes is listed in `SaveAssetRegistry`
(ids are asset **names**), or the save forgets what the ship is wearing.

**Every resource past scrap sits behind its own hook level**: iron 1, gold 2, titanium 3,
uranium 4, spice 5 — and scrap and antimatter stay at 0, since one is the quota and the other is
jump fuel, and neither may ever be out of reach. So there are five `Hook_upgrade_N` items in
`Assets/Configs/Upgrades/`, each a plain `HookLevel` **+1** that stacks, and five recipes in
`Assets/Configs/Recipies/`. Each rung is paid for with the resource the rung below it opened —
80 scrap, then 120 + 30 iron, 160 + 25 gold, 200 + 20 titanium, 240 + 15 uranium — and the trader
sells the schematics for 40 / 150 / 350 / 700 / 1200 credits. Because the bonuses merely stack, a
player who bought the metal instead of mining it could have crafted "Hook 4" and silently got
level 1, so `CraftingRecipe.RequiredUpgrade` names the rung below and `CraftingService.CanCraft`
refuses without it; `WorkbenchUI` prints that as another red cost line ("needs Hook 3"), next to
the ones for missing resources. `HookProjectile` already tells the player what a rock wants
("Hook Lv.2 required"), which is the whole nudge towards the workbench.

When adding ship upgrades, check how `ShipStatsReceiver` actually consumes the stat: thrust,
damage, fire rate and **max health are multiplier-only** (`SetMaxHealthMultiplier` — a `FlatBonus`
on `MaxHealth` is read by nothing), hook level is bonus-only, and only `RadarRange` uses both
(`MinimapUI.CurrentRange`). `ApplyModifiers` ignores a `Multiplier` of 0 and multiplies the
existing one otherwise, so levels of the same family compound.

Islands are the solid obstacles in space: `AsteroidIsland` builds one at runtime — a `Grid` plus a
ground `Tilemap` (with `TilemapCollider2D` merged into a `CompositeCollider2D` on a static body,
layer Ground) and a detail `Tilemap` with no collider. The shape is a circle chewed by two sine
waves and then forced to be "thick": enclosed pockets are flood-filled and every cell whose
neighbour mask the sets have no picture for (a lone cell, a one-cell spur, a one-cell bridge) is
trimmed, looping until nothing changes. Skipping that left holes and sliced-looking corners. Each
cell picks its tile from the neighbour mask through `SpaceTileSet` (`SpaceTileSet_Dark` /
`SpaceTileSet_Light` in `Assets/Configs/Zones/`, slots named after the picture — the "top left"
corner is the one that continues east and south). A cell with all four neighbours but one missing
diagonal takes an inner corner (`ResolveInner`) — four of the tiles that look like fill are really
concave corners, each with one corner carved out. Deep interior must therefore use the one tile
that is genuinely solid (`tile1` / `stone_0000_Layer-50`, stored without an alpha channel at all);
filling with the carved ones punched a grid of little square holes through the rock. Size and shape come from a hash of the island's
own position, so nothing extra is saved: `ZoneItemTag` already stores the position and the tile-set
index, and a load rebuilds the same rock. The generator places islands first and sizes their
clearance per candidate (`WorldRadiusFor` + `IslandMargin`), since every other object has to fit
around them.

**Two finds sit on the islands**, and they are the only thing in space that asks the player for
something other than flying. `SpacePuzzleComponent` (`Gameplay.Flight/`) is one script with a
`SpacePuzzleKind`: `SalvageMine` (the cutting rig, `platform.png`) runs the slider, `SatelliteWreck`
(`Robot.png`) runs the typing, and the two prefabs in `Assets/Prefabs/Space/Puzzles/` differ in
nothing else — the same trick as the seven resource prefabs. `[E]` opens a **Yarn node** (`_dialogueNode` on the
prefab — `Find_OreShaft` / `Find_DeadSatellite` in `SpaceEvents.yarn`), not a popup: a find is a
thing in the world that talks about itself, and the choice to take the risk belongs in the same
window as every other choice in the game. The node answers through the `apply_effect` hook that
already exists: `AcceptFind` (`Assets/Configs/Quests/Effects/`, an `AcceptFindEffect`) publishes
`FindAcceptedEvent`, the component hears it and then waits for `DialogueFinishedEvent` before
opening its window, since the dialogue holds a pause of its own. It stays subscribed only for the
length of that conversation, or every find in the sector would answer somebody else's dialogue;
declining costs nothing and the find stays. A node that is not there — one broken Yarn file drops
every node in the project at once — makes the find open its minigame without the briefing instead
of going dead, since the request is answered synchronously and the component can see that nothing
started. **No new Yarn command or function** was added on
purpose — one the analyser has not declared fails the whole Yarn Project (see "Dialogue UI"),
while an effect is one more row in `NarrativeDirector._effects`, a list the scene instance
overrides, so that row lives in `SpaceScene`. The minigame itself goes out as
`TimingPuzzleRequestedEvent` / `TypingPuzzleRequestedEvent` (`_Core/Contracts/PuzzleEvents.cs`),
whose payload is difficulty plus an `Action<bool>` — the window knows nothing about mines or
satellites, exactly as `ConfirmRequestedEvent` knows nothing about jumps.
`RandomZoneGenerator.PlacePuzzles` picks one island and an angle, but the find is **planted on the
rock by the spawner**, not by the generator: `AsteroidIsland.TrySurfacePoint` walks the same outline
the radar draws — real cells, after `Cleanup` — and returns the edge point in that direction plus
`PuzzleIslandMargin` (0.45, about half a tile), with the sprite's up turned away from the island so
it stands on the surface. Finds are the only content in the zone that saves a rotation
(`PrefabSpawnPoint.Rotation`). `WorldRadiusFor`, which the generator aims at, is the outer bound of
every cell the shape *could* have used, and the chewed silhouette sits well inside it, so a find
left at that radius hung in open space a few units off the rock. The snap runs **only while
generating**: the save keeps the finished point, and re-snapping on load would add the standoff
again every time. The angle is retried rather than clamped to the area, because a clamped point
lands inside the rock, where nothing can reach it. No clearance is
claimed: the island's own `IslandMargin` (6) ring is wider than the find's offset, so there is
nothing there to collide with. `ZoneItemKind.Puzzle` goes **last** in the enum — that number is
saved.

**The bet is the point, and it is one-shot**: both outcomes `Destroy` the find, because a
replayable bet is a resource printer — hull is mended with scrap, so a shaft that could be retried
would quietly trade scrap for gold. Success pays `RewardRocks` (2-3) rolls of a resource the sector
actually carries: `SectorResources` is the one lottery, shared with the generator, and the reward
asks a resource **prefab** for its own `ResourceType` and `RollYield()`, so a find can never pay in
a currency the sector does not have, nor in amounts the economy has not been balanced against. It
ignores hook level on purpose — a cracked shaft is the one way to hold uranium before the hook
reaches it, and 2-3 rocks once per island is a lucky find, not a way around the ladder, which gates
whole fields. `RecipeChance` (**0.06**) pays an unknown schematic instead, rarer because one schematic
outvalues any ore, and it falls back to ore once the player knows every recipe, since an empty
reward reads as a bug. It started at 0.15 and that was far too generous in play: a sector holds
about one find, so 0.15 handed out a schematic every six or seven sectors — against a trader who
charges 120 to 1200 credits for the same sheet. At 0.06 a whole run turns up one, maybe none, and
the shop stays the way you actually get them. Failure spends hull: `DamageBase` 8 + `DamagePerSector` 3, i.e. 8 in sector 1
and 44 in sector 13 — flat numbers against a hull that Plating roughly doubles, so the same bet gets
gentler as the ship improves. The plasma burst is parented to the **ship**, not left at the find
(the player is watching their own hull), and its sorting order is forced in code, because the
prefab authors it at 0, i.e. behind the hull and behind the fog. Everything above is tuned in one
asset, `Assets/Configs/Zones/SpacePuzzleConfig.asset`, which both prefabs point at: each number is
a pair (first sector → `DeepSector` 13) read by `Mathf.Lerp`, so depth narrows the green band
0.3 → 0.12, speeds the marker 0.55 → 1.15 bars/s, raises the hits from 1 to 3 and the words from
2 to 5.

The two windows (`UI/TimingPuzzleUI.cs`, `UI/TypingPuzzleUI.cs`, shared shell in `PuzzlePanel.cs`)
are built in code like `ControlsLegendUI` — in the scene both sit on one empty object
(`Main Camera / Canvas / PuzzleWindows`) that carries nothing but the font and two sprites, and
each window re-parents itself to the **root canvas** at runtime, so the host rect and its place
among the canvas children mean nothing. They wear the game's own window art rather than a flat
plate: `Information/info_0005_window1` (the frame the inventory, journal, `JumpConfirm` and the
tutorial popup all use) as a **sliced** image at `pixelsPerUnitMultiplier` **0.25**, which is how
every other window scales that art — it is drawn four times smaller than the 3840×2160 canvas.
Those two sprites live on **`UiInstaller.asset`** (`WindowSkin`, injected), not on the scene
objects, and that placement is the point: Unity reimports a changed `.asset` by itself, but it
**never reloads an open scene from disk** — a hand-edited `.unity` file reaches the game only
after the scene is reopened, and until then the component keeps whatever the editor loaded, which
for a field added since is the C# default. That is how both windows once came up as plain white
slabs: the sprites sat in the scene file, correct, while the running copy had `null`. Anything a
code-built window needs for its look therefore belongs in an asset. An `Image` with no sprite
fills white, so `PuzzlePanel.Plate` falls back to a dark plate instead of the white tint.
Nine-slicing is why the panel size is free, but the border is not decoration-free: 47 px of trim
top and bottom become 188 units, so content is laid out inside `_frameInset` (120 × 200) rather
than against the panel edge, or the title sits on the frame. The slider track and the word plate
use `info_0003_back`, the plain inner field of the same set, and the green band is inset by
`_bandInset` so the field's own edge still shows under it. The frame has a close cross painted
into its top-right corner; these two windows have no button there, because a minigame has no
abort. Both hold the pause, so they run on **unscaled** time and poll
`Keyboard.current` directly, like every other paused window. Each **releases the pause before the
callback**, since that callback spawns an explosion which then has to play. The slider ignores
input on its opening frame: the Enter that closed the briefing still counts as pressed in it. There
is no abort key and both windows have a clock — a window the player can walk out of makes the risk
free, and a slider with no timer would simply hang if they never pressed. Typing watches the
**key** the next letter needs (`Key.A + (letter - 'a')`), not `Keyboard.onTextInput`: that returns
what the OS layout produces, so on a Russian layout the letter "a" arrived as "ф" and an English
word was untypeable — the same trap `InputKeyLabels` exists for. A wrong key is not accepted at all,
the clock being the whole punishment.

The tutorial gate is one condition, not two: `TutorialResourceGoal` publishes
`TutorialCorridorClearedEvent` when every `HookableResource` under its root is gone, and
`CollectObjects_Quest` — the quest the first wormhole requires — uses `TutorialCorridorGoal`, which
waits for exactly that event. Counting a number of collected objects instead let the player open
the gate without clearing the corridor, which then swallowed a story beat.

Space has two background layers, both driven off the camera, neither of them tiled textures.
`BackgroundParallax` sits on the sky sprite that hangs under `Main Camera` and offsets it against
the camera's world position, clamping the offset to whatever the sprite overhangs the view by — so
the drift is free but an edge can never slide in. The sky is 9.6×5.4 units per 960×540 at PPU 100,
scaled 3.2 (15.36×8.64 half-size 7.68×4.32… i.e. 30.7×17.3 overall), which leaves ≈3.6 units of
vertical slack against the ortho-5 camera and so supports 3% parallax out to the 110-unit death
boundary. `PlanetFieldBackground` (`PlanetField` under `_________SPACE________`, so it hides with
space in the interior; `FlightInstaller` registers it so it can hear `SectorChangedEvent` and
reseed — a new sector gets a new sky) does the planets: one per cell of an explicit `_cellSize` lattice around the
camera, tiles recycled as cells leave view, and each cell's sprite, scale, jitter, alpha and flips
hashed from its coordinates so the same planets reappear when the player flies back. The step is a
plain number on purpose — the PNGs are 960×540 frames, but the importer slices each one down to
the planet itself (`spriteMode: 2`, rects of 45–139 px, i.e. 0.45–1.39 units), so deriving the step
from the sprite packed them into a tiny dense square around the ship. Keep the cell near the camera
view (17.8×10 at ortho 5) and remember `_gridRadius` has to cover half the view plus half a cell
plus jitter plus the largest scaled planet — 5×5 cells at `_cellSize` 9×5.5 holds up to 21:9.

## Space flight

`ShipMovementController` is Newtonian with the edges filed off, because pure asteroid inertia is
the single hardest thing for a non-gamer here. Thrust still pushes along the nose, but "back" is only a
brake now: `_allowReverse` is off on `PlayerShip`, so holding it kills speed and spin and never
backs up, while the AI keeps reverse (the Kite archetype retreats with it). `EngineSoundController`
only runs on forward thrust, so braking is silent. The numbers below are the ones
on the `PlayerShip` prefab, which overrides every default in the script: with no throttle input
the ship bleeds speed at `_passiveDeceleration` 1 u/s² and never exceeds `_maxSpeed` 12 u/s (0
disables the cap), thrust is a gentle `_thrustForce` 2 u/s², and the spin is held by
`_torqueStrength` 0.25 against a `_maxAngularSpeed` 90 °/s cap, shed at `_angularDamping` 60 °/s²
when the turn key is released and `_brakeAngularDamping` 240 °/s² while braking. Damping only
runs on the axis with no input — applying it under thrust would just cancel the thrust.

One key does everything near the player: `Interact` is bound to **E** (F kept as a spare) in both
the Flight and Platformer maps, and the `Dock` action is left with no binding at all. `ShipInteractor`
and `PlayerInteractor` decide what the press means — something interactable in range gets
interacted with, an empty press publishes `ModeSwitchRequestedEvent` — so there is no second,
nearly identical key to learn. `ModeSwitchInput` still listens to `Dock` if a binding is ever
added back.

`ControlsLegendUI` (`ControlsLegend` under the HUD canvas, registered by `UiInstaller`) prints the
flight controls in the bottom-left corner and toggles with **F2** (F1 now belongs to the debug
switch). It reads every key from the
`InputActionAsset` itself through `InputKeyLabels`, so a rebind can't leave the legend lying,
and it builds its own panel at runtime — the scene object only carries the font, so there is no
layout to re-wire by hand. It hides with the rest of the flight HUD through
`HudVisibilityChangedEvent`.

`MinimapUI` (`Radar.prefab`) draws three kinds of dot by layer mask — resources, enemies, NPCs —
plus a ring of boundary dots, all of them only while inside radar range. Anything carrying a
`RadarLandmark` (`_Core/World/`, holding its own icon sprite and tint, added to the trader,
quest-giver and wormhole prefabs) is treated differently: the scan that first sees it remembers it
for good, it draws its icon instead of a dot, and out of range the icon is pinned to the rim in
the object's direction and dimmed by `_offRangeAlpha`, with an optional `_directionArrow` sprite
(must point up) rotated to the bearing. Memory is per transform, so a regenerated zone drops its
landmarks by itself and nothing is saved; a reload therefore starts the sector unexplored again,
which matches flying it from the centre. The component must sit on the object the scan actually
returns — the rigidbody root, or the collider's own GameObject when there is no rigidbody.

Islands get a grey **perimeter** instead of a dot, because their edge is the thing you crash into.
`AsteroidIsland.Build` takes one point per angular sector (the outermost edge cell in it, stepped
by `OutlineStep` ≈ 1.3 world units, 8-32 points by size), stores them as world positions — the rock
never moves — and hangs them on an `IslandOutline` (`_Core/World/`, so `UI` can read it without
referencing `Gameplay.Flight`). That component goes on the **ground tilemap child**, the object
that carries the rigidbody and the composite collider, since the radar reaches islands through its
own physics scan: `_islandMask` (Ground, layer 3 — keep it equal to `ZoneConfig.IslandLayer`) is
part of the scan filter. Points outside radar range are simply not drawn, so a big rock shows only
the arc that is in range. The outline is read off the finished cells rather than the edge formula
because `Cleanup` trims cells and moves the silhouette by a tile or two.

**Draw order in space is a three-step ladder**, and only `m_SortingOrder` sets it — the project
has a single sorting layer (Default). Zone content runs from the islands (-10/-9) through
resources (5), the finds on the islands (6), their shine (7), enemies (10/11), the boundary fog
(15), the ships (19-21), a plasma burst stuck to the hull (25, set in code), the hook (30) and the
aim pointer (35). **40** is for world-space UI stuck to an object: the enemy
health bar, the wormhole's cost label and the `[E]` prompt — all of them were authored at 0, i.e.
behind the rocks and, since the fog arrived, behind the fog as well. **100** is `PlayerCanvas`
(`Assets/Prefabs/UI/HUD/Feed/`), the world-space canvas that follows the player ship and carries
`NotificationFeedUI` and `WarningBannerUI`: a message has to be readable over anything in space.
Nothing in space can reach the windows above it, because those live on the scene's
**Screen Space - Overlay** canvas, which Unity always draws last whatever the world sorts to.

Approaching anything interactable in space raises an `[E] Trade` / `[E] Quest` /
`[E] Enter wormhole` label, the flight twin of the interior's `InteractionHint`. `ShipInteractor`
drives it: the throttled search (`_hintRefreshInterval`, 0.1 s) uses the **same**
`Physics2D.OverlapCircle` that decides what the key press hits, so the prompt can never name
something that would not answer. The label is built in code (`InteractPromptLabel`) rather than
wired into the trader, quest-giver and wormhole prefabs, since zone content is generated and
there would be nothing to wire it into; it is parented to **`SpaceRoot`** (not `Content`, not the
ship) so it hides with space in the interior, survives a regeneration and never inherits the
ship's spin. It sits above the collider's `bounds.max.y`, which is why a bigger hull pushes it
higher on its own, and it goes away while `HudVisibility` is off (dialogue, cutscene). The font is
a field on the component (`_hintFont` on `PlayerShip`) because TMP's own default would break the
pixel look.

`BoundaryFog` (Flight entry point) draws where the map ends. The vignette in
`BoundaryDeathController` says "bad" without saying "here", so a wall of haze stands at the edge:
one sprite, centred on `ZoneBounds.Center`, parented to **`SpaceRoot` itself** (not `Content`, or
a regeneration would delete it, and not the camera, since it has to stay put). The gradient is
baked into a generated 1024x1024 texture, so nothing is computed per frame and the thing has no
`Tick`. The ramp has **two stages**, because the generator fills a *square* `AreaSize` while the
fog is a circle: the far corner of that square (`AreaSize.magnitude / 2`, 70 units for 100x100)
is further out than the 60-unit safe ring, and a wall there would hide the rocks the player came
for. So from `_fogInnerRadius` (38) out to that corner the haze only reaches `_fogHazeFraction`
of full (0.25 → about a quarter alpha, a veil you read the zone through), and the real wall is
built between the corner and `_fogOuterRadius` (0 = the death line, `SafeRadius + DeathDistance`),
where there is nothing left to see anyway. Change `AreaSize` and the fog follows on its own.
It is meant to look drawn, not rendered: `FilterMode.Point` plus alpha quantised into
`AlphaSteps` (7) bands turns the ramp into concentric pixel rings — `Ceil` on purpose, so the
inner edge is a hard ring rather than a fade — and the texel lands at ~0.2 world units (~20
screen px at ortho 5), which is the knob to turn if the blocks are the wrong size. It sorts at 15: above the zone's content (5) and below the
ships (19+), because a pilot who cannot see their own hull is lost rather than warned. Colour,
density and that order are fields on `FlightInstaller`.

`BoardingTutorial` (Flight entry point) keeps the ship sealed in sector 1 with a `ModeLock` and
lifts it on `SectorChangedEvent`, so the tutorial corridor can't be skipped by walking inside;
a blocked switch now says so through `WarningMessageEvent` instead of silently doing nothing. The
first intercom call raises a `TutorialPopupEvent` explaining how to board — but only if the player
is not already in the interior, which is exactly where a jump leaves them.

## Ship interior movement

`CharacterMovementController` (Platformer action map: Move / Jump / Interact) is a plain
platformer: horizontal velocity from Move, a jump impulse when grounded, ground checked
with an overlap circle against `_groundMask`. `CharacterAnimationController` feeds `Speed`,
`IsGrounded` and `VerticalVelocity`.

Everything the player can press inside the ship lives in `Gameplay.Ship/`:
`IntercomComponent`, `WorkbenchComponent`, `ShipComputerComponent`, `PilotConsoleComponent`,
`CargoChestComponent`. The chest (`Assets/Prefabs/Interior/CargoChest.prefab`) opens the ordinary
inventory through `InventoryWindowRequestedEvent` rather than a window of its own — the hold is
one per ship, and a second view of it would drift from the first. The prefab carries its own
`[E]` hint (the Canvas/TMP pair lifted from the intercom's), but no highlight glow: `_highlight`
is empty, which `InteractionHint` accepts. It takes the `EventBus` from the `PlayerContext` that
`Interact` is handed rather than through `[Inject]`, on purpose: `ShipInteriorScope` lists the
interior components by name (the additive scene is built after the root scope, so
`RegisterComponentInHierarchy` up there cannot see them), and a new component with `[Inject]`
simply keeps null fields until someone remembers to add a line — which is exactly how this one
threw on its first press. Anything in the interior that needs more than the context still has to
be registered there.

`InteractionHint` splits its two cues: the `[E] …` text follows the player — `PlayerInteractor`
focuses the hint on whatever its interaction circle would actually trigger, so the prompt shows on
every approach and always names the right object — while the highlight glow still marks objects
the player has never used (`IInteractionHintService`, saved). The hint lookup runs on
`_hintRefreshInterval` (0.1 s), not per frame; a key press queries the circle itself, so
interaction accuracy never depends on that interval.

Vertical traversal between tiers is **scene geometry, not code**: a slanted collider the
player runs up, with a one-way `PlatformEffector2D` so they pass through it from below and
must jump to land on it. Don't add climbing states or a ladder component for this.

## Dialogue UI

The dialogue window is an instance of Yarn's **package** prefab
`dev.yarnspinner.unity/Prefabs/Dialogue System` in `SpaceScene` (its Canvas overridden to
3840×2160, same as the HUD). The prefab is read-only, so project-specific parts are
instance overrides: `UnscaledTimeTypewriter` on `Line Presenter`, and
`SpeakerTalkPresenter` + `SpeakerPortraitPresenter` on the `Dialogue System` root, all
listed in the runner's `dialoguePresenters`. The portrait is a `Speaker Portrait` ▸ `Icon`
object under `Canvas` (a sibling of `Line Presenter`, so the bar's layout group never
touches it), 300×300 = 0.5 × the 600×600 radar, anchored to the canvas bottom-left. The bar
only grows when a portrait is actually shown. `SpeakerPortraitPresenter` drives two knobs
per line and holds **both** values for each: the vertical layout group's left padding
(`_paddingLeftWithPortrait` 340 / `_paddingLeftPlain` 40, so text clears the portrait) and
a `LayoutElement` on the `Text` child (`_minHeightWithPortrait` 240 / `_minHeightPlain` -1,
so the bar is tall enough). It deliberately does not read the scene's current values —
doing that once froze the bar at whatever the editor had loaded — so the presenter, not
`Line Presenter`, is where these sizes get tuned. The same left padding also goes to
`_optionsLayout` — the `Options Presenter` layout group — because the options list is a
separate panel with the same geometry as the bar (both inset 200 per side), so without it
the answers slide under the portrait. That panel shows the last line itself
(`showsLastLine`), so it replaces the bar rather than stacking under it; a `LayoutElement`
added to it (`_optionsHeight`, `_optionsMinHeightWithPortrait` 296 = 240 + the layout
group's 24/32 padding) keeps it from shrinking below the bar's height when the options
appear.

**Text that comes from C# goes into a Yarn variable, never a function.** Functions are added at
runtime with `DialogueRunner.AddFunction`, and the compiler only learns of them second-hand:
the editor analyser reads those calls out of the C# source and writes declarations
(`ProjectSettings/Packages/dev.yarnspinner/Narrative-generated.ysls.json` lists what it found),
which `YarnProjectImporter` then feeds to the compiler. A call it has no declaration for fails
with *"Can't determine the type of the expression"*, and one failed file fails the **whole** Yarn
Project — every node in the game disappears and each dialogue logs "Yarn node … not found". That
generation happens on assembly compile while the Yarn Project is only reimported when a `.yarn`
file changes, so the two can fall out of step and a reimport of the `.yarnproject` is what puts
them back. A variable has none of that: `<<declare $name = "">>` in the `Variables` node at the
top of `StoryDialogues.yarn` is the declaration, and C# writes the value with
`_dialogueRunner.VariableStorage.SetValue`. Yarn compile errors do not show up as script errors
in the Console — look in `Editor.log` for "Error compiling".

`RelationshipFeedUI` shows attitude changes where the player is actually looking — a short
"Vee +5" over the dialogue bar. It lives on the dialogue canvas (added to that prefab instance the
same way as the portrait), because the HUD notification feed is hidden during dialogue and the
dialogue canvas draws over it anyway; it counts on unscaled time, since a dialogue pauses the game.

`SpeakerPortraitPresenter` toggles whatever `_root` points at — the `Speaker Portrait`
container — and `Icon` inside it must stay **enabled**, otherwise the container turns on
with nothing in it. It shows only for speakers with an entry in
`Assets/Configs/Narrative/Dialogue/SpeakerIconSet.asset`; its `Awake` hides the container
so nothing is visible before the first line.

## HUD visibility

`GameNotificationsSource` (root scope) **coalesces resource changes**. Its group line prints
`NpcGroup.DisplayName` (falling back to the asset name), never the asset itself — interpolating a
`UnityEngine.Object` calls `ToString()`, and the feed read "Scavangers_Group (Game.Core.NpcGroup)
reputation +5". The asset name stays misspelled on purpose: `SaveAssetRegistry` ids **are** asset
names, so renaming the file would lose every save; only the `DisplayName` was corrected. Selling ten units one
click at a time is twenty `ResourceChangedEvent`s, and the feed holds each line for 1.5 s, so the
player watched half a minute of "+5 Credits". Deltas now pile up per resource and go out as one
line each (`-10 Scrap`, `+50 Credits`) once the stream has been quiet for 0.6 s, or after 2.5 s
at the latest. It ticks on **unscaled** time, because the trader and the workbench hold a pause
while the player clicks. `NotificationFeedUI` runs on unscaled time for the same reason
(`TMPWriter.UseScaledTime = false`): with the default scaled clock the writer froze while a
window held the pause, the queue filled up with everything the player did inside the ship, and
the whole backlog played out in space once the game resumed. Anything that waited longer than
`_staleSeconds` (6 s) is dropped when its turn comes rather than shown late — an event is worth
reporting while it is news. It also answers `QuestCompletedEvent`, publishing the same
`Objective complete: <title>` line as a `NotificationRequestedEvent` **and** a
`WarningMessageEvent`: in the feed alone it would queue behind the `+1 Scrap` lines that the same
class emits, and the one moment the player must notice is the moment the hold is finally full.

`HudVisibility` (root scope) owns one rule — flight HUD is visible only when **not** in the
interior, **not** in a dialogue and **not** in a cutscene — and publishes
`HudVisibilityChangedEvent`. `FlightHudVisibility` in the scene applies it by toggling the
`_flightHudRoots` list: ship health, quests panel, radar, race HUD, trader. Each entry must
be the instance's **prefab root**, not an inner child — pointing at a child leaves the
panel's own background drawn during cutscenes. Windows that manage themselves (inventory,
craft, journal, ship state, game over, tutorial popup) stay out of the list.

`TutorialPopupUI` pauses the game while a tutorial hint is up and releases on close, so the hint
is read rather than flown past; Enter, Space or Escape close it.

Every paused window polls `Keyboard.current` directly in `Update` rather than going through an
action map — they hold the pause, and the gameplay maps are off at that moment. `ConfirmDialogUI`
confirms on Enter or Space and cancels on Escape; Escape also closes inventory, journal, workbench
and ship state, which otherwise only had their own toggle key or a button.

**A hotkey window never opens while something else holds the pause.** `InventoryUI` (I) and
`JournalUI` (J) check `IPauseService.IsPaused` before opening and only before: closing still
works. Polling the raw keyboard is what made this necessary — the action maps are off under
pause, but a raw `iKey` is not, so typing `iridium` in the hacking minigame threw the inventory
over it. The same rule keeps windows from stacking on a dialogue, the trader or a cutscene.
`ShipInteractor` has the matching guard at the other end: it skips the whole frame the pause
lifts in (`justResumed`), because the action map is re-enabled while the key that closed the
window is still down, and the Input System reports that held key as pressed on the next frame —
the E that won a slider used to board the ship a frame later.

`InventoryUI` has three tabs and each answers a question nothing else does. **Resources** is the
hold. **Items** is what the ship carries that is not ore — and the known **schematics**, printed
with the output's own icon and `Schematic` where an item shows `x3`, because a schematic is
property the trader literally sells, and until now the only place to see the ones you own was the
workbench, mixed in with what you can build this minute. **Reputation** holds both ledgers under
`— FACTIONS —` and `— PEOPLE —` headers (a header prints only when something stands under it):
group reputation, and the personal attitude of each character, which used to exist only as a
`Vee +5` flash over the dialogue bar with nowhere to look it up afterwards. People get their own
words (`Cold` / `Warm` / `Loyal`), since "Allied" reads like a fleet, not a person. Installed
upgrades are deliberately **not** listed here — `ShipInfoUI` owns that list, by family. The three
lists are the three tab objects themselves (`ResourcesContent` / `ItemsContent` /
`ReputationContent` under one `Scroll View / Viewport`), so a row always lands in the list it
belongs to. A `ScrollRect` holds **one** content, so `EnsureScroll` gives each of the three a
`ContentSizeFitter` (the prefab has layout groups but no fitters, and without one the content
keeps the viewport's height and there is nothing to scroll) and `ShowTab` hands the active list to
the scroll — otherwise only Resources scrolled. The prefab's `scrollSensitivity` of 1 is raised to
60, the same figure `TraderUI` uses, since the canvas is 2160 tall. The `FACTIONS` / `PEOPLE`
dividers are **not** rows: `AddHeader` builds a bare label with a `LayoutElement`, borrowing the
font from `_emptyText`, because as rows they wore the item plate and read as two nameless
possessions; what killed Items and Reputation for a long while was not code but geometry — both
rects sat at `anchoredPosition.y -1138` with 1156 shaved off their height, i.e. below the
viewport, and the `Mask` ate every row while Resources, anchored at 0, looked fine.

`TraderUI` wraps its own offer list in a `ScrollRect` at `Awake` (`EnsureScroll`): the prefab has
the rows in a plain `VerticalLayoutGroup` that runs off the bottom of the window, and the list is
long now that every schematic is on sale. The container is reparented into a generated viewport
with a `RectMask2D`, re-anchored to the top and given a `ContentSizeFitter` — without the fitter
the content height stays zero and there is nothing to scroll. A row's price reads `Buy 60 / Sell 30`, or just
`Buy 60` when the other half is 0 (`PriceLabel`) — antimatter is the one-way row, and
"Sell 0" would have read as a broken row rather than as a refusal. Every trade call returns a bool and
**that bool is the only feedback the player gets**: `TradeService` silently does nothing when the
credits or the goods are short, so `TraderUI` turns each `false` into a `WarningMessageEvent`
("Not enough credits: 150 needed, 40 aboard"), since a button that does nothing reads as broken.

`ConfirmRequestedEvent` (message + `Action` callback + button labels) → `ConfirmDialogUI`
shows a paused modal and invokes the callback on confirm. Used by `WormholeComponent`,
which spends the jump cost only after the player confirms the one-way warning.

## Workbench and ship state

Both windows exist so the player can decide what to spend scrap on, so neither prints raw
save data.

`WorkbenchUI` has two tabs under the title — `CraftTab` and `UpgradesTab` on `CraftUI.prefab`,
both the same button sprite as the shop's, since one sprite has to serve as two states. The
**open** tab is the switched-off one: `interactable` false and dimmed to `_openTab` (plate and
label both), because there is nowhere to click it to; the closed tab stays bright at
`_closedTab`. Their `m_DisabledColor` is set to opaque white on the prefab so Unity's own
disabled tint doesn't dim the open tab a second time on top of ours. Both tabs sit at y 540 in
the 1500×2000 panel, under the title and clear of the close button (whose bottom edge is 625),
and the two scroll views start at 455 so nothing overlaps the frame's top furniture. Each row prints what the part
does under its name — `UpgradeEffectText.Describe` turns `ShipStatModifier[]` into `Hook +1` or
`Hull ×1.35` and `ItemDefinition.Description` is appended after a dash, since the number alone
("Thrust ×1.25") never says what the part is made of or what it is for — and colours each cost
green or red rather than only greying the button. A recipe's `RequiredUpgrade` prints as one more
red cost ("needs Plating 2"), because an uncraftable row has to say why. Installed upgrades stay
in the list without a button, and an empty list prints where to get the missing thing. There is deliberately no running total of
resources in the window: every row already shows `have/needed` for what it costs.

`ShipInfoUI` is laid out against the window art, not around it. `defeat_0039_window_bg` bakes
a column of four icon cells into the frame on the right, so every readout row is built in code
at fixed coordinates and each row lays its own crisp 31×31 tile over its cell
(`_readoutIconX` 368, `_readoutFirstRowY` 291, `_rowSpacing` 76.7, `_iconSize` 78, measured
from the panel centre — the baked cells are 28 px in a 775×391 sprite stretched to a 2000×1000
panel). The overlay is slightly larger than the cell so the blurry baked copy can't show at
the edge, and all four are overlaid rather than only the one that changed, so they stay equally
sharp. Order top to bottom: Damage (`Damage_icon`), Hull (`element_0039_shield`), Fire rate
(`element_0037_fire`), Thrust (`element_0036_lightning`). Four cells means four rows, so Hook
level and Radar get the same row treatment in the left half with their own icons
(`Hook_icon` / `radar_icon`, `_leftIconX` / `_leftFirstRowY`), and `_statsText` is reduced to
the `SYSTEMS` heading above them. Values are **effective** numbers, `Damage 12  ×1.2`, never
bare multipliers: a stat's base is known only to the component that owns it, so
`ShipStatsReceiver` (and `MinimapUI` for radar range) push theirs into
`IShipStatsService.SetBase`, and `TryGetEffective` returns `base × multiplier + bonus`. Bases
are never saved — their owners re-register them every run. `_upgradesText` under them is the
whole catalogue from `SaveAssetRegistry.Items`, because a list of what you already own is empty
exactly when the player most needs direction — but a line per item stopped fitting at twenty
upgrades, so a line is a **family**: its label, `fitted/total`, and the state of the first
unfitted rung (*in the hold / at the bench / no schematic*, or *complete*). A family is the stat
it drives, since no two families touch the same one, and `<pos=45%>` / `<pos=58%>` hold the three
columns apart without a layout group. The whole block has **seven lines** and not one more: the
field is 720x330 at font 36 with overflow (not clipping), and the hull bar and repair button are
built right under it — anything that wraps lands on top of them. That is why the status strings
are short and the rungs are a count instead of a row of boxes: five `[x]` ran past the third
column and took the line with them. The ship contour behind all of it is a watermark — a faint
wireframe — so text is meant to sit over it. The hull bar and the repair button are built in
code inside `_panel` (`_hullBlockPosition` / `_hullBlockSize`), placed by hand rather than by a
layout group because the left panel's edges come from the frame picture; the button wears the
same sprite as the workbench tabs (`_repairButtonSprite`).

The game font (`CyberpunkCraftpixPixel`) carries 117 glyphs: ASCII plus `×`, `—`, `–`, `…`,
`−`, `÷`. Anything else — `■ □ · ² ° •` — falls through to the Liberation Sans fallback and
breaks the pixel look, so player-facing strings use `[x]`/`[ ]` for checkboxes and `—` as a
separator. New icons dropped into `Assets/Sprites/UI/Windows/Elements/` must be reimported with
`filterMode: 0` and `textureCompression: 0` like the rest of that set, or they blur when the UI
scales them up ~2.5×.

## Audio

**3D sound is measured in the listener's plane.** The only `AudioListener` sits on `Main Camera`,
and that camera stands at **z = -10**, so a shot fired at the ship's nose was already ten units
away: with `SoundService`'s linear 3..25 rolloff it lost a third of its volume before it started,
and the far edge of hearing shrank to 23 units of actual XY. `PlayAtPosition` therefore puts the
one-shot at the listener's own z and keeps only x/y, which is the distance the player can see.
Moving the listener onto the ship would have been the other fix, but the interior is a separate
additive scene with its own sources, and they would have gone quiet.
`ISoundService.Play` is the 2D half of the same service — no position, no rolloff — for what
happens to the player themselves: the ship's own explosion is not "somewhere", so it does not fade.
Both halves of a ship blowing up (the `PlayerExplode` effect plus one of three `Взрыв N.ogg`
clips, picked at random) come from a single `ExplosionKit` on `FlightInstaller`: `PlayerDeathController`
plays it flat for the player, `ShipExplosion` (on all four enemy prefabs) spawns it at the wreck
and plays it positional, scaled by `_enemyExplosionScale` because the effect is drawn for the
player's hull. `Health` destroys its own GameObject right after `Died`, so the effect is
instantiated **unparented** — parented to the wreck it would vanish in the same frame.

One mixer: `Assets/Audio/MainMixer.mixer`. Groups nest as
`Master → Player (Shot, Footsteps) / Environment (Ring) / background music / Cutscene Music`,
snapshots `Gameplay`, `Paused`, `Cutscene`. Nothing routes to `Shot` from a scene: `ShipCannon`
and `HookableResource` carry the group on themselves and pass it to
`ISoundService.PlayAtPosition`, which sets it per one-shot. `PauseService` owns Gameplay↔Paused; `CutsceneController` switches
to `Cutscene` (space music muted, Cutscene Music audible) and `ReleasePause` returns to
`Gameplay`. Music itself is plain scene `AudioSource`s, not a service — space music lives
on `SceneNeeds/Music background` in `SpaceScene` and must be set to **Loop**, otherwise it
never comes back after a cutscene. The interior has music of its own, so `SpaceMusicMute`
(Flight entry point) mutes every source routed to the **background music** group on
`GameStateChangedEvent` into `ShipInteriorState` and unmutes on the way out — otherwise both
tracks played at once. It finds those sources by mixer group rather than by a scene reference,
and the group comes from `FlightInstaller`, since an asset is reimported on its own while an open
scene is not. Mute, not stop: the track continues where it left off. `SoundService` is only for one-shot positional SFX.

Four more sources, all under `Environment` except footsteps (`Player → Footsteps`):
`IntercomCallSound` (`IntercomRing` under `SceneNeeds` in `SpaceScene`, group `Ring`) loops
`Уведомление на комп.ogg` while a call is pending and the player is **not** in the interior —
inside, the intercom's own `_callAudioSource` plays the same clip once, so they never double up.
`CharacterFootsteps` on `PlayerCyborg` plays one of `_clips` (`Footstep_1…4`) every `_interval`
while the character is grounded and actually moving, with a little pitch jitter; on pause
`deltaTime` is zero, so it falls silent by itself. `ShipAmbience` in `ShipInterior` loops
`На корабле.ogg`, and a second looping source on `Interior lvl1/1_0` in the same scene hums
`Двигатель внутри.ogg` — plain scene `AudioSource`s with no script behind them. The `PlayerShip`
engine sources (`SingleSource` / `HookSource` / `LoopSource`) have **no** output group set, so
they bypass the mixer and land on Master.

## Save system

`ISaveable` (`SaveId` + `Save()`/`Load(json)` with `JsonUtility`) per service.
`SaveService` collects them; `SaveAssetRegistry` (`Assets/Configs/Saves/`) maps
ScriptableObject assets ↔ stable string ids. Hotkeys: **F5** save, **F9** load — both behind
the debug switch (F1), since a stray F9 throws away everything since the last jump.

Every jump autosaves (`WormholeTravelController` publishes `SaveGameRequestedEvent` once the zone
has been regenerated and the player sits at its centre), and that is the checkpoint death returns
to: `GameOverUI.Restart` loads the save instead of reloading the scene when `SaveService.HasSave`,
so the opening is played once and a later death drops the player at the start of the sector. The
player ship therefore must survive its own death — `Health._destroyOnDeath` is off on `PlayerShip`,
`PlayerDeathController` only hides it and switches it back on at `GameLoadFinishedEvent`. With no
save yet (death before the first jump) the old full scene reload still runs.

## Starting player state

- **Starting resources** (scrap/credits/etc.): `_startingResources` on
  `Assets/Configs/Installers/SharedInstaller.asset`, applied by
  `Gameplay.Shared/StartingResources.cs` (`IStartable`). `ResourceCost { Type, Amount }`,
  enum order in `_Core/Contracts/ResourceType.cs`.
- **Starting inventory items / known recipes**: no mechanism exists yet — `ItemService`
  and `RecipeService` start empty and are filled by loot, crafting, trade and quests.

## Build / run

Open the project in the Unity Editor and press Play (`SpaceScene` is the entry scene).
There is no CLI build or test suite; `.csproj`/`.sln` files are Unity-generated — never
edit them by hand.
