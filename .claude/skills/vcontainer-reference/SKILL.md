---
name: vcontainer-reference
description: Verified technical reference for VContainer (the DI framework used in this Unity project) — package install URL, asmdef/namespace layout, LifetimeScope API, entry-point lifecycle timing, MonoBehaviour injection, RegisterComponentInHierarchy scope, and how child scopes actually work (or don't). Consult this BEFORE searching the web whenever touching VContainer setup, writing or editing a LifetimeScope, registering services, wiring up MonoBehaviour injection, or debugging why an injected dependency is null/missing — even if the user doesn't say "VContainer" explicitly (e.g. "add DI for the new trader service", "why isn't this getting injected", "wire this into the container"). Facts here were verified against the official docs and source in 2026-07 against v1.19.0; re-verify anything version-sensitive if the installed version has since changed.
---

# VContainer reference (this project)

Read this before Googling — these facts were already verified against the official
VContainer docs/GitHub source (some by reading the actual source file, not just docs).
If something here contradicts what you observe in the installed package, trust the
installed source and update this file.

## 1. Package install

UPM git dependency for `Packages/manifest.json`:

```json
"jp.hadashikick.vcontainer": "https://github.com/hadashiA/VContainer.git?path=VContainer/Assets/VContainer#1.19.0"
```

OpenUPM alternative: `openupm add jp.hadashikick.vcontainer`.

This project already has it pinned at `1.19.0` — see `Packages/manifest.json`.

## 2. Assembly and namespace layout

The whole package is **one** asmdef named `VContainer`. Add exactly that string to an
`.asmdef`'s `"references"` array — that's it, no separate `"VContainer.Unity"` asmdef
exists, don't add it, it will fail to resolve.

Namespaces (both live in the same `VContainer` assembly):
- `VContainer` — core DI: `IContainerBuilder`, `Lifetime`, `IObjectResolver`.
- `VContainer.Unity` — Unity integration: `LifetimeScope`, `IStartable`, `ITickable`,
  `ILateTickable`, `IInitializable`, `IAsyncStartable`, `IPostStartable`, **and `IInstaller`**
  (verified by reading `Runtime/Unity/IInstaller.cs` — it's a "Unity" concept, not core DI,
  which is easy to get wrong since `IContainerBuilder`, the type it references, IS in
  `VContainer`). If a custom installer class fails with `CS0246: IInstaller could not be
  found` despite `using VContainer;`, add `using VContainer.Unity;` too.

**Gotcha:** every asmdef that has `using VContainer` (or `VContainer.Unity`) anywhere in its
code — not just `_Core` — needs `"VContainer"` in its own `.asmdef` `"references"` array.
It doesn't come along transitively through a reference to `Core`. We hit
`CS0246: The type or namespace name 'VContainer' could not be found` in `Gameplay.Flight`
because its asmdef referenced `Core` but not `VContainer` directly — adding `"VContainer"`
to `Gameplay.Flight.asmdef`'s `references` fixed it. Check this first whenever a feature
assembly's installer/controller code (anything under a `DI/` folder, or any MonoBehaviour
using `[Inject]`) fails with that error.

## 3. LifetimeScope basics

Subclass `LifetimeScope`, override:

```csharp
protected override void Configure(IContainerBuilder builder)
```

Common registration calls:

```csharp
builder.RegisterInstance(existingObject);
builder.Register<Impl>(Lifetime.Singleton).As<IInterface>();
builder.RegisterEntryPoint<T>(Lifetime.Singleton);
```

## 4. Entry-point lifecycle interfaces

`RegisterEntryPoint<T>()` auto-wires whichever of these interfaces `T` implements —
no manual PlayerLoop plumbing needed:

| Interface | Timing |
|---|---|
| `IInitializable.Initialize()` | Right after the container is built |
| `IStartable.Start()` | ~`MonoBehaviour.Start` |
| `ITickable.Tick()` | ~`MonoBehaviour.Update` |
| `ILateTickable.LateTick()` | ~`MonoBehaviour.LateUpdate` |
| `IDisposable` | On scope/container disposal (Singleton/Scoped) |

**Gotcha we hit in this project:** entry points in the same phase run in
**registration order**. If one entry point's `Start()` must happen before another's
(e.g. an event-bus subscriber must subscribe before something else publishes), register
the subscriber first. See `Assets/_Core/DI/GameLifetimeScope.cs` for a live example
(`InputMapSwitcher` registered before `GameStateMachineBootstrap`).

## 5. Injecting MonoBehaviours (scene components)

MonoBehaviours can't get constructor injection (Unity instantiates them, not the
container), so VContainer uses **method injection**:

```csharp
[Inject]
public void Construct(IFoo foo) { ... }
```

That alone isn't enough — VContainer also needs to know the component *exists* and
should be injected. Register it explicitly, e.g.:

```csharp
builder.RegisterComponentInHierarchy<T>();
```

## 6. `RegisterComponentInHierarchy<T>()` search scope

**Gotcha:** `RegisterComponentInHierarchy` (and the other `RegisterComponent*` helpers) are
extension methods declared in `VContainer.Unity` (`ContainerBuilderUnityExtensions.cs`), not
`VContainer` — same trap as `IInstaller` (section 2). A `ScriptableObjectInstaller` that
only has `using VContainer;` compiles fine right up until it calls
`builder.RegisterComponentInHierarchy<T>()`, which fails with
`CS1061: 'IContainerBuilder' does not contain a definition for 'RegisterComponentInHierarchy'`.
Add `using VContainer.Unity;` alongside `using VContainer;` in every installer that calls
these methods — `FlightInstaller.cs` and `ShipInstaller.cs` both need it.

Verified by reading `ContainerBuilderUnityExtensions.cs` source directly (not just docs):
it searches the **entire Unity scene** that the `LifetimeScope`'s own GameObject belongs
to (`lifetimeScope.gameObject.scene`) — **not** just children of the LifetimeScope's
GameObject. So the target component can live anywhere in that scene; no parenting under
the LifetimeScope is required.

Related registration helpers:
- `RegisterComponentOnNewGameObject<T>(Lifetime, name)` — spawns a new GameObject.
- `RegisterComponentInNewPrefab<T>(prefab, Lifetime)` — instantiates a prefab.

## 7. Child/parent LifetimeScope relationships are NOT automatic

Two `LifetimeScope` MonoBehaviours sitting in the same scene — even one nested under the
other in the GameObject hierarchy — do **not** become parent/child of each other just by
being nested. That's a reasonable assumption to make and it's wrong; verify before relying
on it.

Real mechanisms for parent/child scopes:
- `LifetimeScope.EnqueueParent(parentScope)` before loading an additive scene.
- An inspector "Parent Type" reference, for scenes loaded additively.
- Explicit code: `CreateChild()` / `CreateChildFromPrefab()`.

If you're not doing one of those three things on purpose, you don't have a child scope —
you have two independent containers.

## 8. No built-in Inspector "Installers" list on LifetimeScope

Verified by reading `LifetimeScope.cs` source directly: there is **no** serialized,
Inspector-assignable list of installers on the base class. It only has a private
`localExtraInstallers` list (populated programmatically) and a public
`autoInjectGameObjects` list. Don't assume you can just drag installer components onto a
LifetimeScope out of the box.

**Why this matters here:** this project's architecture forbids feature assemblies being
referenced by `_Core` (that would create an asmdef cycle, since every feature assembly
already references `Core`). But the root `GameLifetimeScope` lives in `_Core` and often
needs to pull in registrations owned by a feature assembly (e.g. `Gameplay.Flight`).

**The pattern we adopted** (already implemented — reuse it, don't reinvent it):

```csharp
// _Core — Assets/_Core/DI/ScriptableObjectInstaller.cs
using VContainer;
using VContainer.Unity; // IInstaller lives here, not in VContainer

public abstract class ScriptableObjectInstaller : ScriptableObject, IInstaller
{
    public abstract void Install(IContainerBuilder builder);
}
```

```csharp
// _Core — Assets/_Core/DI/GameLifetimeScope.cs
[SerializeField] private List<ScriptableObjectInstaller> _featureInstallers = new();

protected override void Configure(IContainerBuilder builder)
{
    // ...core registrations...
    foreach (var installer in _featureInstallers)
        installer.Install(builder);
}
```

```csharp
// Gameplay.Flight (or any other feature assembly) — its own DI/ folder
[CreateAssetMenu(menuName = "Game/Installers/Flight Installer", fileName = "FlightInstaller")]
public sealed class FlightInstaller : ScriptableObjectInstaller
{
    public override void Install(IContainerBuilder builder)
    {
        builder.Register<PlayerShipInput>(Lifetime.Singleton).As<IShipInputProvider>();
        builder.RegisterComponentInHierarchy<ShipMovementController>();
    }
}
```

The coupling happens by dragging the generated `.asset` into `GameLifetimeScope`'s
`Feature Installers` list in the Inspector — never via a C# reference. When a new feature
assembly needs DI registrations, add a new `ScriptableObjectInstaller` subclass in that
assembly and drag its asset in; don't add anything to `_Core`.
