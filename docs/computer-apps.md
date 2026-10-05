# Computer apps (`IAliveNpcsComputerApi`)

AliveNpcs 1.7.0 puts a computer in the farmhouse. Its desktop has AliveNpcs' own screens and an **Apps** folder, and your mod can add apps to that folder with one call. AliveNpcs' own Perfection app goes through the same registry, so an addon app behaves exactly like a built-in one.

```
AliveNpcs computer (in the farmhouse)
└── Desktop: AliveNpcs' screens + Apps
    └── Apps folder (a window on the monitor)
        ├── Perfection   ← built into AliveNpcs
        ├── Your app     ← added by your mod
        └── …            ← any number, in pages when they don't fit
```

- The folder shows each app's icon and name: AliveNpcs' own apps first, then the others in the order they were added. When there are more than fit, it turns into pages (arrows, mouse wheel, PageUp/PageDown or LB/RB).
- When the player clicks your app, AliveNpcs calls your `createMenu` and opens the menu it returns **as a child of the folder**. Closing your menu (Escape, the controller's B or your close button) brings the player back to the folder.

The sample mod has a complete app: [`samples/ExampleAddon`](../samples/ExampleAddon) (`ModEntry.cs` registers it, `RadioMenu.cs` is the screen).

---

## Quick start

### 1. Copy the interface

Copy [`api/IAliveNpcsComputerApi.cs`](../api/IAliveNpcsComputerApi.cs) into your mod. The namespace can be your own.

### 2. Declare the dependency

```json
"Dependencies": [
  { "UniqueID": "Lucas.AliveNpcs", "MinimumVersion": "1.7.0", "IsRequired": false }
]
```

The computer API exists since 1.7.0. See [Getting started](getting-started.md#3-declare-alivenpcs-as-a-dependency) for what `MinimumVersion` does.

### 3. Register the app when the game launches

```csharp
using AliveNpcsIntegration;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;

namespace MyMod;

public sealed class ModEntry : Mod
{
    private IDisposable? _app;

    public override void Entry(IModHelper helper)
    {
        helper.Events.GameLoop.GameLaunched += OnGameLaunched;
    }

    private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
    {
        var computer = Helper.ModRegistry.GetApi<IAliveNpcsComputerApi>("Lucas.AliveNpcs");
        if (computer == null)
            return; // AliveNpcs isn't installed: nothing to do.

        var icon = Helper.ModContent.Load<Texture2D>("assets/radio-icon.png"); // 16×16 pixel art

        _app = computer.RegisterComputerApp(
            ownerModId: ModManifest.UniqueID,
            appId: "Radio",
            getName: () => Helper.Translation.Get("app.name"),
            iconTexture: icon,
            iconSourceRect: null, // the whole texture
            createMenu: () => Context.IsWorldReady ? new RadioMenu() : null);
    }
}
```

Load a save, use the AliveNpcs computer, open **Apps**, and your app is there.

---

## `RegisterComputerApp` reference

| Parameter | What to pass |
|---|---|
| `ownerModId` | Your manifest `UniqueID`. AliveNpcs' own ID (`Lucas.AliveNpcs`) is reserved for built-in apps. |
| `appId` | An ID that is unique within your mod, such as `"Radio"`. IDs are compared without case. |
| `getName` | Returns the name shown under the icon. It is read each time the folder opens or turns a page, so return the translation for the current language rather than a fixed string. Long names wrap to two lines and the rest is shortened; the full name shows on hover. |
| `iconTexture` | The texture that holds your icon, or `null` for AliveNpcs' generic window icon. |
| `iconSourceRect` | The icon's area inside the texture (handy for sprite sheets), or `null` for the whole texture. |
| `createMenu` | Builds your app's menu **each time** the player opens it. Return `null` when the app can't open right now (for example before something is unlocked); the folder then tells the player the app is turned off. |

**Returns** an `IDisposable` handle. Dispose it to remove the app. Calling `RegisterComputerApp` again with the same `ownerModId` and `appId` replaces your app and keeps its place in the folder; a handle from before the replacement then does nothing.

Invalid calls are logged as a warning and ignored: a blank ID, a missing `getName` or `createMenu`, or AliveNpcs' own mod ID.

---

## Writing the app's menu

Your app is a normal `IClickableMenu`. A few rules make it behave well inside the computer:

1. **Don't touch `Game1.activeClickableMenu`.** The desktop is the active menu, and your app is a child of the Apps folder. To close, call `exitThisMenu()`. The game then removes the child and the player is back in the folder.
2. **Close on Escape and B.** The controller's B arrives as `Keys.Escape`. If your app has a popup, close the popup first and the app on the next press.
3. **Draw the cursor last.** End `draw` with `drawMouse(b)`; the folder below you stops drawing its cursor while your app is open.
4. **Lay out from `Game1.uiViewport`**, and redo the layout in `gameWindowSizeChanged`. The folder passes window resizes on to your app.
5. **Support the controller.** Give your clickable parts `ClickableComponent`s with neighbour IDs (or `ClickableComponent.SNAP_AUTOMATIC`), fill `allClickableComponents` in `populateClickableComponentList`, and point `snapToDefaultClickableComponent` at a sensible first stop. The folder calls `snapToDefaultClickableComponent` when it opens your app with snappy menus on.
6. **Read the save in `createMenu`.** It runs each time the app opens, so the app always shows fresh data. Viewing the app should not change the save.
7. **Strip emoji from text you didn't write** (player text, AI text). The game's fonts can't draw them.

### Matching the AliveNpcs look

AliveNpcs' own screens (story arcs, letters, diary, Perfection) share a parchment look. To match it, use the game's own textures with these values:

| Element | How AliveNpcs draws it |
|---|---|
| Backdrop | `Game1.fadeToBlackRect` over the whole `Game1.uiViewport`, `Color.Black * 0.6f` |
| Main frame | `drawTextureBox(b, Game1.menuTexture, new Rectangle(0, 256, 60, 60), …, Color.White)` |
| Title banner | The same cream frame (64–84 px tall) above the main frame, with the title in `SpriteText.drawString` |
| Brown panels | `drawTextureBox(b, Game1.mouseCursors, new Rectangle(403, 373, 9, 9), …, new Color(185, 125, 80), 4f, drawShadow: false)` |
| Text on brown | `new Color(255, 250, 240)`, with a shadow in `new Color(86, 46, 22)` drawn at (−2, +2) and (0, +2) |
| Headings on brown | `new Color(255, 225, 170)`, over a 2 px rule in the same colour at 35–45% |
| Secondary text | `new Color(235, 200, 150)` |
| Selected rows and small cards | `drawTextureBox(b, Game1.mouseCursors, OptionsDropDown.dropDownBGSource, …, Color.White, 4f, false)` with dark `Game1.textColor` text |
| Hover | `new Color(255, 220, 150) * 0.22f` behind the row |
| Scroll arrows | `Game1.mouseCursors` up `(421, 459, 11, 12)` and down `(421, 472, 11, 12)` |
| Small captions | `Game1.smallFont` at scale 0.75–0.8 (the game's `tinyFont` draws larger than it looks) |

[`RadioMenu.cs`](../samples/ExampleAddon/RadioMenu.cs) in the sample is a complete screen in this look: backdrop, banner, frame, brown panel, text with shadow, close button, Escape/B and window resizing.

---

## Icons

- **16×16 pixel art looks best.** The folder draws the icon on a light tile at the biggest whole-number scale that fits (4× on most screens, 3× on small ones), so the pixels stay even. Bigger art is scaled down to fit.
- Transparent pixels show the tile behind the icon.
- Load the texture once, for example in `GameLaunched`, with `Helper.ModContent.Load<Texture2D>(...)`. If the texture is disposed later, the folder falls back to the generic icon.

## Names and translations

`getName` is called again whenever the folder lays out its icons, so return `Helper.Translation.Get(...)` and the name follows the game language. AliveNpcs itself is translated into English, Português (BR), Español, Français, Deutsch, Italiano, Tiếng Việt and 中文. Add translations for the languages you can.

---

## When something goes wrong

AliveNpcs never lets an app break the computer:

| Problem | What the player sees | What the log says |
|---|---|---|
| `getName` throws or returns blank | The app ID instead of the name | When it throws, one warning with the error (not one per frame) |
| `createMenu` throws | "This app couldn't open. The SMAPI log says why." | An error with the stack trace |
| `createMenu` returns `null` | "This app is turned off right now." | Nothing |
| Invalid registration | The app isn't in the folder | A warning naming the problem |

---

## Checklist before you ship an app

- [ ] Opens from the Apps folder, and closing it (button, Escape, B) returns to the folder.
- [ ] Works with mouse, keyboard and controller (snappy menus on and off).
- [ ] Fits small screens: test with a window shorter than 800 UI pixels.
- [ ] The name and every string follow the game language.
- [ ] Viewing the app doesn't change the save.
- [ ] `createMenu` returns `null` instead of opening a broken screen when the app can't work (for example before a save is loaded).
