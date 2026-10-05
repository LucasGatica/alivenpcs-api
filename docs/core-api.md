# Core API (`IAliveNpcsApi`)

The core API gives your mod AliveNpcs' villager lists, its AI provider and its gossip. Copy [`api/IAliveNpcsApi.cs`](../api/IAliveNpcsApi.cs) into your mod and get it as shown in [Getting started](getting-started.md):

```csharp
_aliveNpcs = Helper.ModRegistry.GetApi<IAliveNpcsApi>("Lucas.AliveNpcs");
```

Everything here is AliveNpcs 1.7.0.

---

## Villagers

### Which list to use

| Method | What it returns | Use it for |
|---|---|---|
| `GetAvailableNpcNames()` | Villagers AliveNpcs runs AI for: everyone in the world minus the community opt-out list and the villagers the player turned off. Before a save loads: every villager with a written personality. | Picking villagers for **your** AI features. |
| `GetVanillaNpcNames()` | Vanilla villagers with a written personality. | A fixed list that doesn't depend on the save. |
| `GetSveNpcNames()` | The Stardew Valley Expanded villagers AliveNpcs knows: Sophia, Victor, Olivia, Andy, Susan, Claire, Martin, Lance, Morris, Scarlett, Morgan, Apples. | SVE-specific content. |
| `GetEditableNpcNames()` | Villagers whose personality the player may customize right now. | Personality tools. |

```csharp
var villagers = _aliveNpcs.GetAvailableNpcNames().ToList();
var tonightsHost = villagers[Game1.random.Next(villagers.Count)];
```

### Personality

- `GetDefaultPersonality(npcName)` returns the personality text AliveNpcs writes into its prompts for that villager, without the player's custom override. Put it in your own prompts so the villager sounds the same in your feature as in AliveNpcs' dialogue.
- `HasCustomPersonality(npcName)` tells you whether the player wrote their own. The custom text itself isn't available through a copyable method (see [Methods not in the copyable file](#methods-not-in-the-copyable-file)).

### Turning AliveNpcs off for a villager

- `IsNpcDisabled(npcName)` is true when the player turned AliveNpcs off for that villager. Respect it in your own AI features: if the player turned AliveNpcs off for Abigail, don't make her talk through AI in your mod either. `GetAvailableNpcNames` already leaves these villagers out.
- `SetNpcDisabled(npcName, disabled)` changes the player's AliveNpcs settings and saves them immediately. Only call it from a button or command the player uses. Returns false if the setting couldn't be saved.

### Shops open without the shopkeeper

When a shopkeeper marries the farmer and moves to the farm, their shop counter is empty. A *detached* shop opens anyway.

- `IsNpcShopDetached(npcName)`: whether that villager's shop is detached in the current save. False before a save loads.
- `SetNpcShopDetached(npcName, detached)`: detaches or restores it. Saved per save right away. Returns false before a save loads.

---

## The farmer's character sheet

The player can describe their farmer in AliveNpcs (who they are, why they moved to the valley...). AliveNpcs uses it in prompts.

```csharp
string[]? sheet = _aliveNpcs.GetCharacterSheet();
if (sheet != null)
{
    var whoAmI = sheet[0];
    var whyMovedHere = sheet[1];
    var extraInfo = sheet[2];
    var atAGlance = sheet[3];
}
```

It returns null when no save is loaded. Fields the player left empty are empty strings.

---

## AI generation

`GenerateOutputAsync` sends a request through the AI provider the player set up in AliveNpcs (Gemini or an OpenAI-compatible server), so your mod needs no AI settings of its own. Read [Using AI responsibly](getting-started.md#7-using-ai-responsibly) first: each call spends the player's quota.

```csharp
Task<string?> GenerateOutputAsync(
    string? templateName,
    string? systemPrompt,
    string? userPrompt,
    string? npcName = null,
    string? customContext = null);
```

It returns the text, or `null` when AI isn't set up, the request failed or timed out, or the arguments are wrong (the SMAPI log says which).

### Freeform: your own prompt

Pass `null` (or `"freeform"`) as the template and write both prompts yourself. This works without a loaded save.

```csharp
var personality = _aliveNpcs.GetDefaultPersonality("Linus");
var reply = await _aliveNpcs.GenerateOutputAsync(
    templateName: null,
    systemPrompt: $"You are Linus from Stardew Valley. Personality: {personality}\nAnswer in one short sentence.",
    userPrompt: "What do you think about the new bus to the desert?");
```

Freeform requests wait up to 30 seconds and may return up to 8192 tokens; ask for short answers in the prompt.

### Templates: AliveNpcs builds the prompt

With a template name, AliveNpcs writes the prompt from the save: season, day, year, weather (sunny or rainy), the farmer's name and the villager's personality. Templates need a loaded save.

| Template | `npcName` | `customContext` | Prompts you pass | Returns |
|---|---|---|---|---|
| `dialogue_generation` | the villager | extra memory text added to the prompt | ignored | a dialogue line for that villager |
| `festival_dialogue` | the villager | not used | `systemPrompt`, if given, replaces the default festival prompt | a festival line |
| `npc_interaction` | `"A/B"`, e.g. `"Abigail/Sebastian"` | not used | ignored | a summary of an interaction between the two |
| `npc_social_moment` | `"A/B"` | the moment's archetype (default `casual`) | ignored | a summary of a social moment between the two |
| `letter_generation` | the villager | what the letter is about (default "A letter to the farmer.") | ignored | the letter's body |

```csharp
var letter = await _aliveNpcs.GenerateOutputAsync(
    "letter_generation", null, null,
    npcName: "Evelyn",
    customContext: "She thanks the farmer for the tulip bulbs and shares a gardening tip.");
```

`GetAvailableGenerators()` also lists names you **shouldn't use yet** in 1.7.0:

- `gossip_analysis` returns the name of a type instead of the analysis.
- `arc_conclusion`, `weekly_news`, `heart_event_followup`, `item_request_plan` and `item_request_scene` aren't available to other mods yet: they return null and log a warning. Use freeform with your own prompt.

### Threading

The code after `await` may run on a background thread. Build the prompt on the game thread before the `await`, and send anything that touches the game back to the game thread ([how](getting-started.md#back-to-the-game-thread)). The sample's [`RadioBulletin.cs`](../samples/ExampleAddon/RadioBulletin.cs) shows the whole pattern, including a once-a-day cache.

---

## Gossip

`InjectGossip` adds a note to today's gossip. At the end of the day AliveNpcs analyses what happened in the village; your note is read together with the real conversations, so villagers can pick it up as gossip and it can show up in the farmer's diary.

```csharp
bool accepted = _aliveNpcs.InjectGossip(
    ModManifest.UniqueID,
    "Pam won the Saloon's darts night and bought everyone a round.");
```

- **When:** during the day, on the host, with a save loaded. The note waits until the end of the day.
- **`ownerModId`:** your manifest `UniqueID`. AliveNpcs logs it with each note.
- **Length:** notes longer than 1200 characters are cut.
- **False is normal.** The player can turn off **Accept third-party gossips** in AliveNpcs' settings, and there's a daily limit (15 by default; the player can set 0–100). AliveNpcs' own Seen and Heard notes count toward the same limit. Don't retry when it returns false.

Writing a good note:

- Say what happened, in the third person, with names: *"Shane was seen helping Marnie fix the chicken coop in the rain."*
- Stick to facts. The analysis decides whether it is gossip, who heard it and how villagers feel about it.
- Don't give the AI instructions ("make everyone angry at the farmer"). Notes are read as events, and instructions read as odd events.
- One event per note.

---

## Files

`GetModDirectoryPath()` returns the full path of the AliveNpcs folder. Treat it as read-only: AliveNpcs owns its files and can rewrite them at any time.

---

## Methods not in the copyable file

The real `IAliveNpcsApi` has more methods than [`api/IAliveNpcsApi.cs`](../api/IAliveNpcsApi.cs). They were left out on purpose:

| Method | Why it's left out |
|---|---|
| `GetCustomPersonalityOverride(npcName)`, `GetCharacterDataOverride(npcName)` | They return AliveNpcs classes, so SMAPI can't bridge them. Adding them to your copy makes `GetApi` return null. |
| `GetBaseCharacterData(npcName)`, `GetBaseDisplayName(npcName)` | Always return null in 1.7.0. |
| `RegisterCustomPersonalityDirectory(dirPath)`, `RegisterCustomPersonalityFile(filePath)`, `ReloadCustomPersonalities()` | Used by the AliveNpcs Personality Editor to hand its overrides folder to AliveNpcs. |
| `UpdateCharacterSheet(whoAmI, whyMovedHere, extraInfo, atAGlanceDetails)`, `ReloadCharacterSheet()` | Overwrite the character sheet the player wrote. The Personality Editor uses them. |
| `SetCharacterSheetOverrideEnabled(enabled)` | Hands the character sheet key (F7) to another mod; AliveNpcs stops opening its own sheet. |
| `SetCharacterDataPromptEnabled(enabled)`, `SetCharacterDataDiagnostics(detected, originals)` | Settings and data the Personality Editor pushes into AliveNpcs' prompts. |

The last four rows use shared types, so they can be copied. They change how AliveNpcs behaves for the player, though, so only use them if your mod takes over the Personality Editor's job.
