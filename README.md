# AliveNpcs Addon API

Everything you need to make an addon for [AliveNpcs](https://www.nexusmods.com/stardewvalley/mods/43475): the API interfaces to copy into your mod, a guide for each kind of addon, and a sample mod that builds.

This repository documents **AliveNpcs 1.7.0** (UniqueID `Lucas.AliveNpcs`).

## Pick your kind of addon

| You want to… | Build | Code | Guide |
|---|---|---|---|
| Make villagers notice and react to something new | AliveTrigger pack | JSON only | [AliveTrigger packs](docs/alive-triggers.md) |
| Add a mode: prompt text, villager notes, lore, dialogue options, scenes, story arc ideas | Experimental Content pack | JSON and text | [Experimental Content packs](docs/experimental-content.md) |
| Use AliveNpcs' AI, feed it gossip, read its villager lists | SMAPI mod with `IAliveNpcsApi` | C# | [Core API](docs/core-api.md) |
| Add an app to the AliveNpcs computer | SMAPI mod with `IAliveNpcsComputerApi` | C# | [Computer apps](docs/computer-apps.md) |
| Register modes from code, hook story arcs, coordinate multiplayer | SMAPI mod referencing `AliveNpcs.dll` | C# | [Direct reference](docs/direct-reference.md) |

New to C# addons? Start with [Getting started](docs/getting-started.md).

## Quick start (C#)

1. Copy [`api/IAliveNpcsApi.cs`](api/IAliveNpcsApi.cs) into your mod.
2. Add AliveNpcs to your `manifest.json`:

   ```json
   "Dependencies": [
     { "UniqueID": "Lucas.AliveNpcs", "MinimumVersion": "1.7.0", "IsRequired": false }
   ]
   ```

3. Get the API once the game has launched, and use it when you need it:

   ```csharp
   private IAliveNpcsApi? _aliveNpcs;

   public override void Entry(IModHelper helper)
   {
       helper.Events.GameLoop.GameLaunched += (_, _) =>
           _aliveNpcs = helper.ModRegistry.GetApi<IAliveNpcsApi>("Lucas.AliveNpcs");

       helper.Events.GameLoop.DayStarted += (_, _) =>
           _aliveNpcs?.InjectGossip(ModManifest.UniqueID, "Someone saw a strange light over the quarry last night.");
   }
   ```

`_aliveNpcs` is null when AliveNpcs isn't installed, so your mod keeps working without it.

## What's in here

```
api/                          Interfaces to copy into your mod
  IAliveNpcsApi.cs            Core API: villagers, AI generation, gossip
  IAliveNpcsComputerApi.cs    Apps for the AliveNpcs computer
docs/                         Guides and reference
samples/ExampleAddon/         A SMAPI mod using both interfaces
```

The sample adds a **Radio** app to the AliveNpcs computer with an AI-written bulletin (one a day), tells the village when the farmer carries a Prismatic Shard, and adds the console command `example_ask <villager> <question>`. Build it with `dotnet build` from its folder.

Content pack examples have their own repositories:

- [alive-npcs-trigger-example](https://github.com/LucasGatica/alive-npcs-trigger-example): an AliveTrigger pack.
- [alive-npcs-ec-pelican-town-mayor](https://github.com/LucasGatica/alive-npcs-ec-pelican-town-mayor): the official Mayor Mode Experimental Content pack.

## Which interfaces can be copied

SMAPI maps your copy of an interface onto AliveNpcs' API, but it can only map methods whose types both mods share: .NET, MonoGame, Stardew Valley and SMAPI types, delegates and interfaces. A method that takes or returns one of AliveNpcs' own classes can't be mapped, and SMAPI then refuses the whole interface.

| Interface | Copy into your mod? |
|---|---|
| `IAliveNpcsApi`, as in [`api/`](api/IAliveNpcsApi.cs) | Yes |
| `IAliveNpcsComputerApi` | Yes |
| `IAliveNpcsExperimentalContentApi` | No: every method uses AliveNpcs classes. Ship a content pack, or reference `AliveNpcs.dll` ([Direct reference](docs/direct-reference.md)). |
| `IAliveNpcsMultiplayerCoordinator` | No: it's registered through `AliveNpcs.dll` ([Direct reference](docs/direct-reference.md)). |

Both files in `api/` are checked against the AliveNpcs 1.7.0 build with the same mapping library SMAPI uses.

## API history

| AliveNpcs | Added |
|---|---|
| 1.7.0 | `IAliveNpcsComputerApi` (computer apps); AliveTrigger packs |
| 1.4.7 | `InjectGossip` |
| 1.4.5 | `GetAvailableNpcNames`, `GetAvailableGenerators`, `GenerateOutputAsync` |
