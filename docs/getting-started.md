# Getting started with a C# addon

This guide sets up a SMAPI mod that talks to AliveNpcs. If you only want villagers to react to something, or to add prompt text and scenes, you don't need code: see [AliveTrigger packs](alive-triggers.md) and [Experimental Content packs](experimental-content.md).

The finished result of this guide is [`samples/ExampleAddon`](../samples/ExampleAddon).

---

## 1. What you need

- Stardew Valley 1.6 with SMAPI 4.
- The .NET SDK (6 or later; the mod itself targets `net6.0`).
- An editor such as Visual Studio, Rider or VS Code.
- AliveNpcs 1.7.0 or later installed, to test.

If you have never made a SMAPI mod, read the [Modder Guide](https://stardewvalleywiki.com/Modding:Modder_Guide/Get_Started) first. This guide only covers the AliveNpcs part.

## 2. Create the project

A minimal `.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <AssemblyName>MyAddon</AssemblyName>
    <TargetFramework>net6.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Pathoschild.Stardew.ModBuildConfig" Version="4.4.*" />
  </ItemGroup>
</Project>
```

`ModBuildConfig` finds your game folder, references the game and SMAPI, and copies the mod into `Mods` each time you build.

## 3. Declare AliveNpcs as a dependency

In `manifest.json`:

```json
"Dependencies": [
  { "UniqueID": "Lucas.AliveNpcs", "MinimumVersion": "1.7.0", "IsRequired": false }
]
```

- **`IsRequired: false`** lets your mod load without AliveNpcs. Your features that need it just stay off. Use `true` only when your mod is useless without AliveNpcs.
- **`MinimumVersion`** is checked even for an optional dependency: if the player has an *older* AliveNpcs installed, SMAPI won't load your mod and tells the player to update AliveNpcs. Set it to the version that added the newest API you use (see the history in the [README](../README.md#api-history)).

If you'd rather keep working with older versions, leave `MinimumVersion` out and check the version yourself before asking for a newer API:

```csharp
var aliveNpcs = Helper.ModRegistry.Get("Lucas.AliveNpcs");
if (aliveNpcs != null && !aliveNpcs.Manifest.Version.IsOlderThan("1.7.0"))
    _computer = Helper.ModRegistry.GetApi<IAliveNpcsComputerApi>("Lucas.AliveNpcs");
```

Asking for an interface that the installed AliveNpcs doesn't have logs an error in the SMAPI console.

## 4. Copy the interface

Copy [`api/IAliveNpcsApi.cs`](../api/IAliveNpcsApi.cs) (and [`api/IAliveNpcsComputerApi.cs`](../api/IAliveNpcsComputerApi.cs) if you add a computer app) into your project. You don't reference `AliveNpcs.dll`: SMAPI builds a bridge from your copy to the real API.

Rules for your copy:

- **Keep** each method's name, parameter types and return type exactly as they are.
- **Change** the namespace and the comments freely.
- **Delete** the methods you don't use. A smaller interface is fine.
- **Don't add** methods that take or return AliveNpcs' own classes, such as `GetCustomPersonalityOverride` or anything from the Experimental Content API. SMAPI can only bridge types both mods share (.NET, MonoGame, Stardew Valley and SMAPI types, delegates and interfaces). One method it can't bridge makes `GetApi` return null for the whole interface, with this in the log:

  ```text
  Tried to map a mod-provided API to interface 'MyAddon.IAliveNpcsApi', which isn't compatible with the actual mod API.
  ```

## 5. Get the API when the game launches

AliveNpcs builds its API while it loads, so ask for it in `GameLaunched`, not in `Entry`, and keep it in a field:

```csharp
using AliveNpcsIntegration;
using StardewModdingAPI;
using StardewModdingAPI.Events;

namespace MyAddon;

public sealed class ModEntry : Mod
{
    private IAliveNpcsApi? _aliveNpcs;

    public override void Entry(IModHelper helper)
    {
        helper.Events.GameLoop.GameLaunched += OnGameLaunched;
    }

    private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
    {
        _aliveNpcs = Helper.ModRegistry.GetApi<IAliveNpcsApi>("Lucas.AliveNpcs");
        if (_aliveNpcs == null)
            Monitor.Log("AliveNpcs isn't installed; its features are off.", LogLevel.Info);
    }
}
```

From then on, check `_aliveNpcs != null` before each call.

## 6. Call it at the right time

- **Save data needs a loaded save.** Gossip, the character sheet, shop detachment and the template generators return false or null before a save is loaded. Check `Context.IsWorldReady`.
- **Village-wide work happens on the host.** Gossip is analysed at the end of the day by the main player, so inject gossip from the host (`Context.IsMainPlayer`).
- **Don't call every tick.** Lists like `GetAvailableNpcNames` walk the world; read them when you need them, not in `UpdateTicked`.

## 7. Using AI responsibly

`GenerateOutputAsync` sends a request to the AI provider the *player* set up, with the player's API key and quota. Free tiers have small daily limits, and AliveNpcs needs that quota for its own dialogue.

- Only send a request when the player does something that needs it, never on a timer.
- Cache the answer when it can be reused (the sample's radio writes one bulletin a day).
- Expect `null`: the player may not have AI set up, the provider may be down, or the request may time out (freeform requests wait up to 30 seconds).
- Give the player a way to turn your AI feature off.

### Back to the game thread

The code after `await GenerateOutputAsync(...)` can run on a background thread. Reading strings and logging is fine there; touching the game (menus, NPCs, items, `Game1`) isn't. Queue that work for the next tick:

```csharp
private readonly ConcurrentQueue<Action> _gameThreadWork = new();

// In Entry:
helper.Events.GameLoop.UpdateTicked += (_, _) =>
{
    while (_gameThreadWork.TryDequeue(out var work))
        work();
};

// After the await:
_gameThreadWork.Enqueue(() => Game1.drawObjectDialogue(answer));
```

Read everything your prompt needs from the game *before* the `await`, on the game thread.

## 8. Test it

- Build. `ModBuildConfig` copies the mod into your `Mods` folder.
- Start the game through SMAPI and look for your mod and AliveNpcs in the console. An error mentioning `GetApi` or "isn't compatible with the actual mod API" means your copied interface doesn't match (see step 4).
- The sample adds the console command `example_ask <villager> <question>`. It's a quick way to check that AI calls go through.

## 9. Release it

- Build in Release (`dotnet build -c Release`). `ModBuildConfig` puts a ready-to-upload zip in `bin/Release/net6.0`.
- On your mod page, list AliveNpcs as a requirement (or as optional), with the minimum version you set in the manifest.
- Mention that your AI features use the player's AliveNpcs provider and quota.
