---
name: spacescavanger-layout
description: Actual on-disk file layout of this Unity project (SpaceScavanger), which differs from the folder paths shown in the architecture brief. Consult this BEFORE using Glob/Grep/find to locate a script, or before assuming a path like "Assets/Gameplay.Flight/..." from the brief is correct. Use whenever creating, editing, or reasoning about file paths for any feature assembly (_Core, _Data, Gameplay.Flight, Gameplay.Ship, Gameplay.Shared, Narrative, UI, Save).
---

# SpaceScavanger project layout

The architecture brief describes feature folders directly under `Assets/` (e.g.
`Assets/Gameplay.Flight/`). **That's not where they actually live.** The user reorganized
the project in the Unity Editor at some point after Stage 1: everything moved one level
deeper, under `Assets/Scripts/`.

Current real layout (verified 2026-07):

```
Assets/
  Scripts/
    _Core/
      Contracts/     -- IShipInputProvider etc.
      DI/            -- GameLifetimeScope.cs, ScriptableObjectInstaller.cs
      Debug/         -- DebugStateHotkeys.cs
      EventBus/
      Input/         -- GameControls.inputactions, InputMapSwitcher.cs
      StateMachine/  -- GameStateMachine, IGameState, States/
      Core.asmdef
    _Data/
      Data.asmdef
    Gameplay.Flight/
      DI/            -- FlightInstaller.cs, FlightInstaller.asset
      PlayerShipInput.cs
      ShipMovementController.cs
      EngineVisualsController.cs
      Gameplay.Flight.asmdef
    Gameplay.Ship/
      Gameplay.Ship.asmdef
    Gameplay.Shared/
      Gameplay.Shared.asmdef
    Narrative/
      Narrative.asmdef
    Save/
      Save.asmdef
    UI/
      UI.asmdef
  Prefabs/
  Scenes/
  Settings/
    Scenes/
  Sprites/
    Ships/
```

Each feature folder is still its own asmdef with the same name (`Gameplay.Flight.asmdef`
lives inside `Assets/Scripts/Gameplay.Flight/`, not `Assets/Gameplay.Flight/`) — only the
parent path changed, the asmdef graph and namespaces (`Game.Core`, `Game.Gameplay.Flight`,
etc.) are unaffected.

If a `Read`/`Edit` on a path like `Assets/<Feature>/Foo.cs` fails with "file does not
exist," don't assume the file was deleted — try `Assets/Scripts/<Feature>/Foo.cs` first,
or `Glob` for the filename, before concluding anything is missing. Re-verify this layout
if the user reorganizes again; treat this file as a snapshot, not a guarantee.
