#nullable enable
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AliveNpcsIntegration;

/// <summary>
/// The AliveNpcs core API (AliveNpcs 1.7.0). Copy this file into your mod and get it with
/// <c>helper.ModRegistry.GetApi&lt;IAliveNpcsApi&gt;("Lucas.AliveNpcs")</c>. SMAPI maps your copy onto
/// AliveNpcs' API, so you don't reference AliveNpcs.dll.
/// </summary>
/// <remarks>
/// You can change the namespace and delete the methods you don't use. Don't add methods that take or
/// return AliveNpcs' own classes (such as <c>GetCustomPersonalityOverride</c>): SMAPI can't map them,
/// logs an error and gives you null for the whole API. See docs/getting-started.md.
/// </remarks>
public interface IAliveNpcsApi
{
    /*********
    ** Villagers
    *********/

    /// <summary>
    /// The villagers AliveNpcs runs AI features for, after the community opt-out list and the player's
    /// own disabled list. Use this list to pick villagers for your own AI features. Before a save is
    /// loaded it returns every villager AliveNpcs has a written personality for.
    /// </summary>
    /// <remarks>Added in AliveNpcs 1.4.5.</remarks>
    IEnumerable<string> GetAvailableNpcNames();

    /// <summary>The vanilla villagers AliveNpcs has a written personality for.</summary>
    IEnumerable<string> GetVanillaNpcNames();

    /// <summary>The Stardew Valley Expanded villagers AliveNpcs knows by name.</summary>
    IEnumerable<string> GetSveNpcNames();

    /// <summary>The villagers whose personality the player may currently customize.</summary>
    IEnumerable<string> GetEditableNpcNames();

    /// <summary>The personality text AliveNpcs uses for a villager, without the player's custom override.</summary>
    string GetDefaultPersonality(string npcName);

    /// <summary>Whether the player gave this villager a custom personality.</summary>
    bool HasCustomPersonality(string npcName);

    /// <summary>Whether the player turned AliveNpcs off for this villager (no AI dialogue, gossip, arcs, letters...).</summary>
    bool IsNpcDisabled(string npcName);

    /// <summary>
    /// Turns AliveNpcs off or on for a villager. This changes the player's AliveNpcs settings and saves
    /// them right away, so only call it when the player asked for it. Returns false if it couldn't be saved.
    /// </summary>
    bool SetNpcDisabled(string npcName, bool disabled);

    /// <summary>
    /// Whether the villager's shop opens without them standing at the counter (for example after they
    /// married the farmer and moved to the farm). Per save; false before a save is loaded.
    /// </summary>
    bool IsNpcShopDetached(string npcName);

    /// <summary>
    /// Makes a villager's shop open without them at the counter, or restores the vanilla behaviour.
    /// Saved per save right away; returns false before a save is loaded.
    /// </summary>
    bool SetNpcShopDetached(string npcName, bool detached);

    /*********
    ** Player
    *********/

    /// <summary>
    /// The farmer's character sheet as <c>[whoAmI, whyMovedHere, extraInfo, atAGlanceDetails]</c>,
    /// or null when no save is loaded.
    /// </summary>
    string[]? GetCharacterSheet();

    /*********
    ** AI generation
    *********/

    /// <summary>The template names you can pass to <see cref="GenerateOutputAsync"/>, including "freeform".</summary>
    /// <remarks>Added in AliveNpcs 1.4.5.</remarks>
    IEnumerable<string> GetAvailableGenerators();

    /// <summary>
    /// Asks the AI provider the player set up in AliveNpcs for a text, so your mod needs no AI settings
    /// of its own. Each call uses the player's API key and quota.
    /// </summary>
    /// <param name="templateName">
    /// Null, empty or "freeform" sends <paramref name="systemPrompt"/> and <paramref name="userPrompt"/>
    /// as they are. Any other name from <see cref="GetAvailableGenerators"/> builds the prompt from the
    /// save (needs a loaded save).
    /// </param>
    /// <param name="systemPrompt">Freeform: the system prompt (required). festival_dialogue: replaces the default prompt.</param>
    /// <param name="userPrompt">Freeform: the user prompt (required).</param>
    /// <param name="npcName">The villager for templates that need one, or "A/B" for two-villager templates.</param>
    /// <param name="customContext">Extra input whose meaning depends on the template (see docs/core-api.md).</param>
    /// <returns>The text, or null when AI isn't available or the request failed.</returns>
    /// <remarks>
    /// Added in AliveNpcs 1.4.5. The code after <c>await</c> may run on a background thread: don't
    /// touch game state there, hand the result back to the game thread first.
    /// </remarks>
    Task<string?> GenerateOutputAsync(
        string? templateName,
        string? systemPrompt,
        string? userPrompt,
        string? npcName = null,
        string? customContext = null);

    /*********
    ** Gossip
    *********/

    /// <summary>
    /// Adds a note to today's end-of-day gossip analysis, as if villagers had talked about it. It can
    /// become village gossip and show up in the farmer's diary.
    /// </summary>
    /// <param name="ownerModId">Your mod's manifest UniqueID.</param>
    /// <param name="text">What happened, in plain words. Longer than 1200 characters is cut.</param>
    /// <returns>
    /// False when the player turned "Accept third-party gossips" off, today's limit is reached
    /// (15 by default), AliveNpcs isn't ready, or an argument is blank.
    /// </returns>
    /// <remarks>Added in AliveNpcs 1.4.7.</remarks>
    bool InjectGossip(string ownerModId, string text);

    /*********
    ** Files
    *********/

    /// <summary>The full path of the AliveNpcs mod folder.</summary>
    string? GetModDirectoryPath();
}
