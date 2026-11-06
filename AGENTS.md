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
  `PauseService`, `ModeLock`, input/HUD/debug entry points. Registration order matters
  (`InputMapSwitcher` before `GameStateMachineBootstrap`). It then runs the
  `_featureInstallers` list — `ScriptableObjectInstaller` assets from `Assets/Configs/Installers/`:
  `SharedInstaller`, `FlightInstaller`, `ShipInstaller`, `NarrativeInstaller`,
  `UiInstaller`. **Installer code** sits in `<Feature>/DI/`, the **`.asset`** in
  `Assets/Configs/Installers/`.
- Per-object child scopes: `EnemyLifetimeScope`, `TraderLifetimeScope`,
  `QuestGiverLifetimeScope`, `DialogueNpcScope`, `RaceAiScope`, `ShipInteriorScope`.
- See the `vcontainer-reference` skill before touching DI — notably the gotcha that new
  `*LifetimeScope.cs` files get wiped to an empty template on Unity import.
- Cross-system messaging goes through `EventBus` (`_Core/EventBus/`), not direct refs.

## Services (registered in `Gameplay.Shared/DI/SharedInstaller.cs`)

`ResourceService`, `ItemService`, `TradeService`, `QuestService`, `ReputationService`,
`ShipStatsService`, `RecipeService`, `CraftingService`, `UpgradeService`, `SoundService`,
`JournalService`, `InteractionHintService`, `RepairService` — most also registered
`As<ISaveable>()`.
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
The Corporation quota is a plain quest (`CorporateQuota_Quest`) whose goal is a
`QuotaGoalDefinition`: a resource amount plus a deadline in sectors, counted from the sector it
was issued in. That issuing sector is the goal's own saved progress (`ISaveableGoal`) — nothing
else knows it, and the amount is derived from it (`_amount + _amountPerSector * (sector - 1)`),
so a growing quota needs nothing extra in the save. The officer's Yarn node starts the quest with `<<apply_effect QuestTrigger_Quota>>`
and prints its numbers through `quota_amount()` / `quota_sectors()`, so the text never hardcodes
them; both read the live goal through `IQuotaService.Amount` / `SectorsLeft`, never the
definition, which only knows the first sector's figure. A goal implementing `IQuestGoalStatus` prints its own line in `QuestHudUI` instead of a
percentage. Handing the load over is the player's move: with the full amount aboard,
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
enemy prefab set, enemy count/chance and resource count. `EnemyCount` counts **groups**, not
enemies: each roll places a leader at `EnemyClearance` from everything else, then `GroupSize`-1
companions within `GroupRadius` of it, spaced by the much smaller `GroupMemberClearance` — so
squads stay tight while staying clear of landmarks. Quest givers draw from `QuestPoolConfig` (`Assets/Configs/Quests/QuestPool.asset`): tiers keyed by
`FromSector` **accumulate**, so a deep sector still offers the early jobs. A giver with a
`_quest` set keeps that one; otherwise it resolves from the pool on first use, preferring a quest
already active (after a reload the same NPC must still take it back). Because one NPC can now
offer different jobs, the offer/turn-in Yarn nodes live on the `QuestDefinition`
(`OfferNode`/`TurnInNode`), and the node names on the prefab are only a fallback.
`JumpCost` on the tier replaces the wormhole
prefab's own cost when `ZoneSpawner` spawns it (0 keeps the prefab's), so leaving a deep sector
costs more fuel; the hand-placed tutorial wormhole keeps its prefab cost. `ZoneSpawner` resolves the tier for the
sector the content was generated for and stores that number in its save, so loading can't pick
enemies from another tier. The generator places points by rejection sampling: every object claims a clearance radius
(`ResourceClearance` / `EnemyClearance` / `LandmarkClearance` on `ZoneConfig`) and candidates
closer than the larger of the two radii — or inside `PlayerClearRadius` around the zone
centre, where a jump drops the player — are rejected. The wormhole, trader and quest giver
are placed first and never skipped; resources and enemies that find no room after
`PlacementAttempts` tries simply don't spawn.

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
only runs on forward thrust, so braking is silent. With no throttle input the ship bleeds speed at
`_passiveDeceleration` (6 u/s²) and never exceeds `_maxSpeed` (12 u/s, 0 disables the cap), and the
player ship's `_angularDamping` is 360 °/s² so a released turn key stops the spin inside a quarter
second. Damping only runs on the axis with no input — applying it under thrust would just cancel
the thrust, since every ship here accelerates at 1–2.5 u/s².

One key does everything near the player: `Interact` is bound to **E** (F kept as a spare) in both
the Flight and Platformer maps, and the `Dock` action is left with no binding at all. `ShipInteractor`
and `PlayerInteractor` decide what the press means — something interactable in range gets
interacted with, an empty press publishes `ModeSwitchRequestedEvent` — so there is no second,
nearly identical key to learn. `ModeSwitchInput` still listens to `Dock` if a binding is ever
added back.

`ControlsLegendUI` (`ControlsLegend` under the HUD canvas, registered by `UiInstaller`) prints the
flight controls in the bottom-left corner and toggles with F1. It reads every key from the
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

`GameNotificationsSource` (root scope) also answers `QuestCompletedEvent`, publishing the same
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
does under its name (`UpgradeEffectText.Describe` turns `ShipStatModifier[]` into `Hook +1`,
falling back to `ItemDefinition.Description`) and colours each cost green or red rather than
only greying the button. Installed upgrades stay in the list without a button, and an empty
list prints where to get the missing thing. There is deliberately no running total of
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
whole catalogue from `SaveAssetRegistry.Items`, each line marked *installed / in the hold /
schematic known / not found yet*, because a list of what you already own is empty exactly when
the player most needs direction. The ship contour behind all of it is a watermark — a faint
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

One mixer: `Assets/Audio/MainMixer.mixer`, groups `Master / background music /
Cutscene Music / Environment / Player (Engine, Hook, Shot)`, snapshots `Gameplay`,
`Paused`, `Cutscene`. `PauseService` owns Gameplay↔Paused; `CutsceneController` switches
to `Cutscene` (space music muted, Cutscene Music audible) and `ReleasePause` returns to
`Gameplay`. Music itself is plain scene `AudioSource`s, not a service — space music lives
on `SceneNeeds/Music background` in `SpaceScene` and must be set to **Loop**, otherwise it
never comes back after a cutscene. `SoundService` is only for one-shot positional SFX.

Three more sources, all on the `Environment` group except footsteps (`Player`):
`IntercomCallSound` (`IntercomRing` under `SceneNeeds` in `SpaceScene`) loops a ring while a call
is pending and the player is **not** in the interior — inside, the intercom's own
`_callAudioSource` already rings, so they never double up. `CharacterFootsteps` on `PlayerCyborg`
plays one of `_clips` every `_interval` while the character is grounded and actually moving, with
a little pitch jitter; on pause `deltaTime` is zero, so it falls silent by itself. `ShipAmbience`
in `ShipInterior` is a plain looping source. The two ring sources currently point at
`Prison_buzzer.ogg` as a placeholder, and footsteps and the ambience have no clip yet.

## Save system

`ISaveable` (`SaveId` + `Save()`/`Load(json)` with `JsonUtility`) per service.
`SaveService` collects them; `SaveAssetRegistry` (`Assets/Configs/Saves/`) maps
ScriptableObject assets ↔ stable string ids. Hotkeys: **F5** save, **F9** load.

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
