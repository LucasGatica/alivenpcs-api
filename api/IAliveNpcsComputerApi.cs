#nullable enable
using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley.Menus;

namespace AliveNpcsIntegration;

/// <summary>
/// Adds apps to the Apps folder of the AliveNpcs computer (AliveNpcs 1.7.0). Copy this file into your
/// mod and get it with <c>helper.ModRegistry.GetApi&lt;IAliveNpcsComputerApi&gt;("Lucas.AliveNpcs")</c>.
/// See docs/computer-apps.md.
/// </summary>
public interface IAliveNpcsComputerApi
{
    /// <summary>Adds an app to the computer's Apps folder, or replaces the one your mod added under the same ID.</summary>
    /// <param name="ownerModId">Your mod's manifest UniqueID.</param>
    /// <param name="appId">An ID for the app, unique within your mod.</param>
    /// <param name="getName">The name under the icon. It is read each time the folder opens or turns a page, so it can follow the game language.</param>
    /// <param name="iconTexture">The icon's texture, or null for a generic app icon.</param>
    /// <param name="iconSourceRect">The icon's area in the texture (16×16 pixel art looks best), or null for the whole texture.</param>
    /// <param name="createMenu">
    /// Builds the app's menu each time the player opens it. It opens over the computer and closing it goes
    /// back to the Apps folder. Return null when the app can't open right now; the folder then tells the
    /// player the app is turned off.
    /// </param>
    /// <returns>Dispose it to remove the app.</returns>
    IDisposable RegisterComputerApp(
        string ownerModId,
        string appId,
        Func<string> getName,
        Texture2D? iconTexture,
        Rectangle? iconSourceRect,
        Func<IClickableMenu?> createMenu);
}
