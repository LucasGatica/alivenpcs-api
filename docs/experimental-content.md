# Experimental Content packs

Experimental Content (EC) changes how AliveNpcs' AI plays the village. An EC **package** holds one or more **modes**. The player picks one active mode in AliveNpcs' settings, and that mode's text and content are added to the AI's prompts: extra instructions for dialogue and gossip, notes about villagers, canonical lore, special dialogue options, story arc ideas and scenes.

AliveNpcs ships several modes built in (Alien in the Valley, Vampire, Mayor and others). This guide covers making your own as a **content pack**: JSON and text files, no code. To register modes from C# instead, see [Direct reference](direct-reference.md).

The official Mayor Mode pack is a complete, working example: [alive-npcs-ec-pelican-town-mayor](https://github.com/LucasGatica/alive-npcs-ec-pelican-town-mayor).

Everything here is AliveNpcs 1.7.0.

---

## Package layout

```
[EC] My Pack/
├── manifest.json              SMAPI content pack manifest
├── alive-npcs-ec.json         EC package manifest
├── prompts/
│   ├── dialogue_prompt.txt    prompt block text files
│   └── gossip_prompt.txt
├── scenes/
│   └── gus_soup.json          scene event files
├── story-arcs/
│   └── template.json          story arc templates
├── arc-items/
│   └── catalog.json           items story arcs may ask for
└── i18n/
    ├── default.json           translations
    └── pt-BR.json
```

Only `manifest.json` and `alive-npcs-ec.json` are required. All paths inside `alive-npcs-ec.json` are relative to the pack folder.

### `manifest.json`

```json
{
  "Name": "[EC] My Pack",
  "Author": "Your Name",
  "Version": "1.0.0",
  "Description": "Villagers think the farmer is a famous chef.",
  "UniqueID": "YourName.MyECPack",
  "ContentPackFor": {
    "UniqueID": "Lucas.AliveNpcs",
    "MinimumVersion": "1.7.0"
  }
}
```

`ContentPackFor.MinimumVersion` is what actually stops an older AliveNpcs from loading your pack.

### `alive-npcs-ec.json`

```json
{
  "schemaVersion": 1,
  "packageId": "YourName.MyECPack",
  "displayNameKey": "package.name",
  "descriptionKey": "package.description",
  "author": "Your Name",
  "version": "1.0.0",
  "modes": [
    {
      "id": "YourName.MyECPack/Chef",
      "displayNameKey": "mode.chef.name",
      "descriptionKey": "mode.chef.description",
      "featureStatusKey": "mode.chef.status",
      "promptBlocks": [
        { "path": "prompts/dialogue_prompt.txt", "target": "dialogue" },
        { "path": "prompts/gossip_prompt.txt", "target": "gossip" }
      ]
    }
  ]
}
```

| Field | Type | Required | Description |
|---|---|---|---|
| `schemaVersion` | `int` | Yes | Must be `1`. |
| `packageId` | `string` | Yes | Unique package ID. Convention: `Author.PackName`. |
| `displayName` / `displayNameKey` | `string` | One of them | Package name, or an i18n key for it. The key wins when both are set. |
| `description` / `descriptionKey` | `string` | One of them | Package description, or an i18n key. |
| `author` | `string` | No | Author name. |
| `version` | `string` | No | Package version. |
| `aliveNpcsMinVersion` | `string` | No | Informational only; use `ContentPackFor.MinimumVersion` in `manifest.json` to enforce a version. |
| `modes` | `array` | Yes | One or more modes (below). |
| `storyArcDirectories` | `array` | No | Story arc template folders for the whole package. |
| `storyArcPromptBlocks` | `array` | No | Story arc prompt blocks for the whole package. |
| `arcItemCatalogs` | `array` | No | Arc item catalogs for the whole package. |

Every `*Key` field is looked up in the pack's `i18n` folder for the game language, with `default.json` as the fallback.

---

## Modes (`modes[]`)

| Field | Type | Default | Description |
|---|---|---|---|
| `id` | `string` | (required) | Unique mode ID. Convention: `PackageId/ModeName`. |
| `legacyAliases` | `string[]` | `[]` | Old IDs that should still select this mode. |
| `displayName` / `displayNameKey` | `string` | `""` | The name in the mode picker. |
| `description` / `descriptionKey` | `string` | `""` | What the mode does, shown in the picker. |
| `featureStatus` / `featureStatusKey` | `string` | `""` | A short status line. |
| `promptBlocks` | `array` | `[]` | Text added to the AI's prompts. |
| `personalityContributions` | `array` | `[]` | Extra personality text for specific villagers. |
| `npcMetadata` | `array` | `[]` | Facts about specific villagers. |
| `canonicalLore` | `array` | `[]` | Statements that are true in this mode. |
| `dialogueActions` | `array` | `[]` | Extra options in the conversation menu. |
| `sceneEventDirectories` | `string[]` | `[]` | Folders with scene event files. |
| `sceneImageDirectories` | `string[]` | `[]` | Folders with PNGs for the `AliveNpcsShowImage` event command. |
| `storyArcDirectories` | `array` | `[]` | Story arc template folders. |
| `storyArcPromptBlocks` | `array` | `[]` | Text added to story arc detection. |
| `arcItemCatalogs` | `array` | `[]` | Items story arcs may ask for. |
| `safety` | `object` | (defaults) | Safety profile. |

`slug` and `exclusiveGroup` are accepted but have no effect yet. Only one mode is active at a time.

---

## Prompt blocks (`promptBlocks[]`)

Each block adds text to one stage of the AI pipeline.

| Field | Type | Default | Description |
|---|---|---|---|
| `path` | `string` | `""` | A text file with the block. |
| `text` | `string` | `""` | Inline text, used when `path` is empty. |
| `maxChars` | `int` | `1800` | The text is cut to this length. |
| `target` | `string` | `"dialogue"` | Where the text goes (below). |

| `target` | Where the text goes |
|---|---|
| `dialogue` | The system prompt of every villager conversation: daily dialogue, festivals, villager-to-villager interactions and social moments, replies to the farmer and dialogue actions. |
| `greeting` (also `intro`, `daily_greeting`) | The first greeting from a villager who doesn't know the farmer yet, placed after AliveNpcs' "new in town / first meeting" framing so it can override it (for example in a mode where the farmer grew up in the valley). |
| `gossip` | The instructions of the end-of-day gossip analysis. |
| `gossip_interaction` | A made-up conversation fed into the gossip analysis, as if villagers had really talked about it. |

Use exactly these names in a pack. `gossip_analysis` and `gossip_interactions` are only understood by blocks registered from code.

Write prompt blocks as instructions to the AI about the world, not as dialogue:

```text
In this mode, everyone in Pelican Town believes the farmer is a famous chef who left the city.
Villagers often ask for cooking advice, compare their own recipes nervously, or hint that they
would love a taste of the farmer's food. Gus treats the farmer as a friendly rival.
```

---

## Safety profile (`safety`)

| Field | Type | Default | Effect |
|---|---|---|---|
| `romanceLevel` | `string` | `"none"` | `"adult"` or `"explicit"` turns the mode off for child and child-coded villagers; they get a safety notice instead of your prompt text. |
| `requiresAdultOptIn` | `bool` | `false` | `true` has the same effect as an adult `romanceLevel`. |
| `childPolicy` | `string` | `"allowed_non_romantic"` | Accepted, no effect yet. |
| `violenceLevel` | `string` | `"none"` | Accepted, no effect yet. |
| `canonMutation` | `string` | `"perception_only"` | Accepted, no effect yet. |

Any mode with romantic or flirty content should set `"romanceLevel": "adult"`.

---

## Villager personality (`personalityContributions[]`)

| Field | Type | Default | Description |
|---|---|---|---|
| `npcName` | `string` | (required) | Internal villager name. |
| `id` | `string` | (required) | Unique within the mode. |
| `text` / `path` | `string` | `""` | The text, inline or from a file. |
| `strategy` | `string` | `"append"` | How it combines with the villager's personality (below). |
| `priority` | `int` | `0` | Sort order: lower first, then by `id`. |
| `maxChars` | `int` | `1800` | The text is cut to this length. |

| `strategy` | Effect |
|---|---|
| `append` | Added as a note next to the villager's personality. |
| `replace_custom_only` | Replaces the villager's personality text in the prompt; with several, the highest `priority` wins. |
| `replace_if_missing` | Currently works like `append`. |

Any other value is read as `append`.

---

## Villager facts (`npcMetadata[]`)

Facts about a villager that are written into the prompt while the mode is active. They don't change the game: no renames, no saved data.

| Field | Type | Default | Description |
|---|---|---|---|
| `npcName` | `string` | (required) | Internal villager name. |
| `displayName` | `string` | `""` | Written as "display name: X". |
| `aliases` | `string[]` | `[]` | Other names that match this villager in prompts. |
| `isAdult` | `bool?` | `null` | Written as adult or child-safe. |
| `romancePolicy` | `string` | `""` | Free text, e.g. `"not romanceable in this mode"`. |
| `gossipPolicy` | `string` | `""` | Free text, e.g. `"never gossips about the farmer"`. |
| `knownRelationships` | `array` | `[]` | `{ "targetNpc": "Gus", "relationship": "business rival" }` |
| `homeRegion` | `string` | `""` | Where they live. |
| `sourceModId` | `string` | `""` | The mod that adds the villager, for custom NPCs. |
| `maxChars` | `int` | `1200` | The block is cut to this length. |

---

## Canonical lore (`canonicalLore[]`)

Statements that are true while the mode is active.

| Field | Type | Default | Description |
|---|---|---|---|
| `id` | `string` | (required) | Unique lore ID. |
| `text` / `path` | `string` | `""` | The statement, inline or from a file. |
| `affectedNpcs` | `string[]` | `[]` | Only these villagers know it. Empty means everyone. |
| `tags` | `string[]` | `[]` | Accepted, no effect yet. |
| `maxChars` | `int` | `900` | The statement is cut to this length. |

---

## Dialogue actions (`dialogueActions[]`)

Extra options in the conversation menu. When the player picks one, AliveNpcs asks the AI for the villager's reaction using your prompt.

| Field | Type | Default | Description |
|---|---|---|---|
| `id` | `string` | (required) | Unique within the mode. |
| `label` / `labelKey` | `string` | `""` | The option's text. |
| `tooltip` / `tooltipKey` | `string` | `""` | Shown on hover. |
| `promptPath` / `promptText` | `string` | `""` | The prompt for the reaction, from a file or inline. |
| `maxPromptChars` | `int` | `2200` | The prompt is cut to this length. |
| `contextScopes` | `string[]` | `[]` | What the AI also gets to read (below). |
| `recordConversation` | `bool` | `true` | Keep the exchange in the villager's conversation history. |
| `availability` | `object` | (always) | When the option shows up (below). |
| `placement` | `string` | `"response-menu"` | Accepted, no effect yet: actions always appear in the conversation menu. |

| `contextScopes` value | What the AI also reads |
|---|---|
| `currentDialogue` | What the villager just said. |
| `conversationHistory` | Recent conversations with this villager. |
| `gossip` | Current gossip involving this villager. |
| `emotionalProfile` | The villager's memories of the farmer and how they feel. |
| `storyArcs` | Active story arcs involving this villager. |
| `socialDrama` | Tensions and conflicts in the village. |
| `worldState` | Season, day, weather and festivals. |

| `availability` field | Type | Description |
|---|---|---|
| `npcNames` | `string[]` | Only for these villagers. Empty means everyone. |
| `minHearts` | `int?` | Minimum hearts with the farmer. |
| `locations` | `string[]` | Only in these locations. Empty means anywhere. |
| `timeFrom`, `timeTo` | `int?` | Time window, in game time (`600` = 6am, `2600` = 2am). |

---

## Story arcs

AliveNpcs looks at recent events and can start a **story arc**: a small multi-day storyline with objectives. A mode can steer which arcs appear. Everything in this section is guidance for the AI: it reads it, but nothing forces an arc to follow it.

### Story arc prompt blocks (`storyArcPromptBlocks[]`)

| Field | Type | Default | Description |
|---|---|---|---|
| `path` / `text` | `string` | `""` | The text, from a file or inline. |
| `maxChars` | `int` | `1400` | The text is cut to this length. |
| `requiredModeId` | `string` | `""` | Only while this mode is active. Empty means the mode it belongs to. |

### Story arc templates (`storyArcDirectories[]`)

| Field | Type | Description |
|---|---|---|
| `path` | `string` | A folder of `*.json` templates. |
| `requiredModeId` | `string` | Only while this mode is active. |

Each template file:

| Field | Type | Default | Description |
|---|---|---|---|
| `templateId` | `string` | (required) | Unique template ID. |
| `title`, `description` | `string` | `""` | What the arc is about. |
| `eligibleNpcs` | `string[]` | `[]` | Villagers who suit it. |
| `objectives` | `array` | `[]` | Suggested objectives: `id`, `type`, `description`, `targetNpc`, `targetItem`, `completionCriteria`. |
| `sceneLocation`, `sceneMusic` | `string` | `""` | Where the ending scene could happen, and its music. |
| `priority` | `int` | `0` | How strongly to suggest it. |
| `maxPerSave` | `int` | `1` | Suggested limit per save. |

### Arc item catalogs (`arcItemCatalogs[]`)

Items that story arcs may ask the farmer to bring.

| Field | Type | Description |
|---|---|---|
| `path` | `string` | A catalog JSON file. |
| `requiredModeId` | `string` | Only while this mode is active. |

```json
{
  "items": [
    {
      "id": "prismatic_shard",
      "displayName": "Prismatic Shard",
      "aliases": ["prism", "rainbow"],
      "qualifiedItemId": "(O)74",
      "tags": ["rare", "mineral"],
      "availabilityHint": "Found in the mines at level 100+",
      "allowAsStoryArcObjective": true
    }
  ]
}
```

Set `allowAsStoryArcObjective` to `false` to keep an item out of the list.

---

## Scene events (`sceneEventDirectories[]`)

Scenes are small authored moments. Every morning AliveNpcs arms up to one eligible scene per day by default (at most one per location), picked by `weight`. An armed scene is delivered in one of three ways:

| `delivery` | What the player sees |
|---|---|
| `cinematic` | A classic cutscene. It plays when the player enters `location`. |
| `walkUp` | The lead villager, already at `location` because of their schedule, notices the farmer, walks over and starts a conversation, then goes back to their day. |
| `overheard` | Villagers who are already near the player talk to each other in speech bubbles. Nothing stops the player; they can ignore it. A single participant thinks out loud instead. |

### Cinematic scene (schema 1)

```json
{
  "schemaVersion": 1,
  "id": "YourName.MyECPack.scene.intro",
  "enabled": true,
  "titleKey": "scene.intro.title",
  "location": "Farm",
  "eventKey": "9702302",
  "weight": 100,
  "maxTriggersPerSave": 1,
  "cooldownDays": 0,
  "dailyConditions": {
    "experimentalDialogueModes": ["YourName.MyECPack/Chef"],
    "chance": 1
  },
  "entryConditions": {
    "time": { "min": 600, "max": 2600 }
  },
  "scene": {
    "scriptKey": "scene.intro.script"
  },
  "completion": {
    "markSeen": true
  }
}
```

The script is a normal Stardew event script and must contain `AliveNpcsSceneComplete <id>`: that command marks the scene as played for `maxTriggersPerSave` and `cooldownDays`. `eventKey` must be a unique event ID.

### Walk-up and overheard scenes (schema 2)

A walk-up: Gus is at the Saloon in the evening and comes over to ask about a new soup.

```json
{
  "schemaVersion": 2,
  "id": "YourName.MyECPack.scene.gus_soup",
  "enabled": true,
  "titleKey": "scene.gus_soup.title",
  "location": "Saloon",
  "delivery": "walkUp",
  "participants": ["Gus", "Emily"],
  "premiseKey": "scene.gus_soup.premise",
  "fallbackLines": [
    { "speaker": "Gus", "textKey": "scene.gus_soup.line1" },
    { "speaker": "Gus", "text": "Emily says it needs more pepper. Be honest, would you order it?$h" }
  ],
  "weight": 100,
  "maxTriggersPerSave": 1,
  "dailyConditions": {
    "experimentalDialogueModes": ["YourName.MyECPack/Chef"],
    "minHearts": { "Gus": 2 },
    "chance": 0.5
  },
  "entryConditions": {
    "time": { "min": 1700, "max": 2300 },
    "maxDistanceTiles": 10
  }
}
```

An overheard scene with inline text:

```json
{
  "schemaVersion": 2,
  "id": "YourName.MyECPack.scene.pierre_caroline_sign",
  "location": "SeedShop",
  "delivery": "overheard",
  "participants": ["Pierre", "Caroline"],
  "premise": "Pierre wants to repaint the shop sign. Caroline thinks the faded green is part of its charm.",
  "fallbackLines": [
    { "speaker": "Pierre", "text": "A fresh coat on that sign would bring in more customers." },
    { "speaker": "Caroline", "text": "People like the faded green. It feels like home." },
    { "speaker": "Pierre", "text": "Fine, but I'm fixing the crooked letter at least." }
  ],
  "dailyConditions": { "chance": 0.4 },
  "entryConditions": { "time": { "min": 900, "max": 1700 } }
}
```

How they play:

- **Lines.** With AI, the lines are written from `premise`, each villager in their own voice; the premise is the topic and is never quoted. Without AI, or if generation fails or takes too long, the authored `fallbackLines` are used. Write the premise as background, not as dialogue.
- **Only when the villagers are really there.** Nobody is teleported. The lead's schedule must bring them to `location` within the time window, and at that moment the lead must be on screen and within `maxDistanceTiles` of the player.
- **Never in the way.** A scene doesn't start right after the player enters a map, during events, festivals, menus, dialogue or another scene, or while the player uses a tool or rides a horse. Each scene gets up to 3 tries a day.
- **Host only.** Walk-ups and overheard scenes play for the host (or the single player). Cinematic scenes play for everyone.
- **Walk-up lines.** The lead has already called the farmer's name before walking over, so fallback lines shouldn't start with a greeting. Give every fallback line of a walk-up to the lead; lines for other participants make it play as an overheard conversation without AI.
- **Overheard lines.** Generated exchanges have 2–4 lines (1–2 for one person thinking aloud). Long lines are split across bubbles, and dialogue codes such as `$h` are removed from bubbles.

Players can change how scenes are delivered in AliveNpcs' settings (**Village Life → Everyday scenes → Delivery Style**): `Natural` uses each scene's own delivery, `AmbientOnly` never has anyone walk up (walk-ups become monologues), and `Cinematic` only affects AliveNpcs' built-in scenes. Older AliveNpcs versions skip schema 2 walk-up and overheard files, so shipping them is safe.

### Scene fields

| Field | Type | Description |
|---|---|---|
| `schemaVersion` | `int` | `1` (cinematic) or `2` (adds `delivery` and the fields below). |
| `id` | `string` | Unique scene ID. |
| `enabled` | `bool` | Turns the scene on or off. |
| `title` / `titleKey` | `string` | Title, or an i18n key. |
| `location` | `string` | Where the scene happens. |
| `eventKey` | `string` | Unique event ID. Cinematic only. |
| `delivery` | `string` | Schema 2, required: `cinematic`, `walkUp` or `overheard`. |
| `participants` | `string[]` | Schema 2 walk-up/overheard, required: internal villager names. The first one is the lead. |
| `premise` / `premiseKey` | `string` | What the scene is about, used to write the lines with AI. Cut to 400 characters. |
| `fallbackLines` | `object[]` | Lines used without AI: `{ "speaker", "text" }` or `{ "speaker", "textKey" }`. `speaker` must be a participant; `@` is the farmer's name. |
| `weight` | `int` | Selection weight when several scenes compete (minimum 1). |
| `maxTriggersPerSave` | `int` | How many times it can play per save. `0` or less means no limit. |
| `cooldownDays` | `int` | Days before it can play again. |
| `dailyConditions` | `object` | Checked each morning: `seasons`, `weather`, `minHearts`, `maxHearts`, `experimentalDialogueModes`, `minDaysPlayed`, `minYear`, `maxYear`, `chance` (0–1). |
| `entryConditions` | `object` | `time` (`{ "min": 600, "max": 2600 }`) and, for walk-up/overheard, `maxDistanceTiles` (default 12, 2–30). |
| `scene` | `object` | Cinematic script: `script` / `scriptKey`, `branches` / `branchKeys`. |
| `completion` | `object` | `markSeen`, `recordMemory`, `memoryText` / `memoryTextKey`. |

A schema 2 walk-up or overheard scene needs at least one of `premise`, `premiseKey` or `fallbackLines`. Invalid files are skipped with a message in the SMAPI log.

### Testing scenes

- `alivenpcs_scene_status` shows today's scene plan.
- `alivenpcs_scene_arm <id>` arms a scene for today.

---

## Scene images (`sceneImageDirectories[]`)

Folders of PNGs you can show full-screen during a cinematic scene with the `AliveNpcsShowImage` event command. A file `scene_images/intro/saloon.png` in a folder listed as `"scene_images/"` is addressed as `Mods/Lucas.AliveNpcs/SceneImages/intro/saloon`.

| # | Argument | Required | Default | Description |
|---|---|---|---|---|
| 1 | asset key | Yes | — | `Mods/Lucas.AliveNpcs/SceneImages/...` |
| 2 | duration | Yes | — | How long the image stays, in milliseconds (not counting the fade). |
| 3 | fade | No | `250` | Fade in and out, in milliseconds. `0` turns fading off. |
| 4 | letterbox | No | `true` | Black bars to keep the image's proportions. |

```text
AliveNpcsShowImage Mods/Lucas.AliveNpcs/SceneImages/intro/saloon 3000 500 true/speak Abigail \"Welcome.\"/end
```

The event waits while the image is shown.

---

## Built-in mode IDs

Useful in `dailyConditions.experimentalDialogueModes` and `requiredModeId`:

| Mode ID | Name |
|---|---|
| `AlienValley` | An Alien in the Valley |
| `ValleyHeartthrob` | Valley Heartthrob |
| `ValleyHeartthrobCute` | Valley Heartthrob (Cute) |
| `Vampire` | Vampire Mode |
| `Slasher` | Slasher Mode |
| `Yandere` | Yandere Mode |
| `Lucas.AliveNpcs.EC.PelicanTownMayor/Mayor` | Mayor Mode (legacy alias `PelicanTownMayor`) |

`ValleyHeartthrob`, `ValleyHeartthrobCute` and `Yandere` are always off for child villagers.
