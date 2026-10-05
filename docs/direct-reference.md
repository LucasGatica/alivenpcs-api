# Direct reference: Experimental Content from C# and the multiplayer coordinator

Two parts of AliveNpcs can't be used by copying an interface:

- **`IAliveNpcsExperimentalContentApi`**, which registers Experimental Content from code. Every method takes or returns AliveNpcs classes (`ExperimentalModeRegistration`, `StoryArcInfo`...), and SMAPI can't bridge those.
- **`IAliveNpcsMultiplayerCoordinator`**, which is registered on AliveNpcs' mod entry, not through `GetApi`.

To use them, your mod compiles against `AliveNpcs.dll` itself. Before you do, check whether you really need code:

- Prompt text, villager notes, lore, dialogue actions, scenes and story arc templates all work from a **content pack**. See [Experimental Content packs](experimental-content.md).
- Gossip, AI generation and villager lists are in the copyable [core API](core-api.md).

What only code can do: prompt text computed at the moment it's needed (dynamic prompt blocks), reacting to story arc events, proposing story arcs, deciding when a dialogue action shows up, mode-scoped lifecycle hooks, and reading active story arcs.

---

## The trade-offs

- **AliveNpcs becomes a required dependency.** Your mod can't load without it.
- **Your mod is tied to AliveNpcs' classes.** If a later AliveNpcs version changes one of them, your mod can fail with `MissingMethodException` or `TypeLoadException` until you rebuild. Pin `MinimumVersion` and test each AliveNpcs update.
- **Never ship `AliveNpcs.dll` with your mod.** The reference is for compiling only; at runtime SMAPI gives your mod the one the player installed.

---

## Setup

### `manifest.json`

```json
"Dependencies": [
  { "UniqueID": "Lucas.AliveNpcs", "MinimumVersion": "1.7.0" }
]
```

`IsRequired` defaults to `true`, which is what you want here.

### `.csproj`

Reference the DLL from your own AliveNpcs install, without copying it:

```xml
<ItemGroup>
  <Reference Include="AliveNpcs" HintPath="$(GamePath)/Mods/AliveNpcs/AliveNpcs.dll" Private="false" />
</ItemGroup>
```

Change the path if your AliveNpcs folder has another name. `$(GamePath)` comes from `ModBuildConfig`.

### Getting the API

Use AliveNpcs' own interface type (namespace `AliveNpcs.Api`). SMAPI then hands you the API object directly, with no bridge:

```csharp
using AliveNpcs.Api;

private IAliveNpcsExperimentalContentApi? _ec;

private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
{
    _ec = Helper.ModRegistry.GetApi<IAliveNpcsExperimentalContentApi>("Lucas.AliveNpcs");
}
```

---

## Registering a mode from code

All registration classes use `init` properties, so you fill them with object initializers.

```csharp
private const string PackageId = "YourName.ChefMode";
private const string ModeId = "YourName.ChefMode/Chef";

private readonly List<IDisposable> _subscriptions = new();

private void RegisterChefMode(IAliveNpcsExperimentalContentApi ec)
{
    ec.RegisterExperimentalContentPackage(new ExperimentalContentPackageRegistration
    {
        OwnerModId = ModManifest.UniqueID,
        PackageId = PackageId,
        DisplayName = "Chef Mode",
        Description = "Villagers think the farmer is a famous chef.",
        Version = ModManifest.Version.ToString()
    });

    ec.RegisterExperimentalMode(new ExperimentalModeRegistration
    {
        OwnerModId = ModManifest.UniqueID,
        ModeId = ModeId,
        DisplayName = "Chef Mode",
        Description = "Villagers think the farmer is a famous chef.",
        PromptBlock = "In this mode, everyone in Pelican Town believes the farmer is a famous chef who left the city. "
                      + "Villagers ask for cooking advice and hint that they'd love a taste of the farmer's food.",
        GossipPromptBlock = "Gossip often turns to what the farmer cooked lately."
    });

    // Text computed each time a dialogue prompt is built while the mode is active.
    _subscriptions.Add(ec.RegisterDynamicPromptBlock(new DynamicPromptBlockRegistration
    {
        OwnerModId = ModManifest.UniqueID,
        BlockId = "todays-special",
        ModeId = ModeId,
        Target = "dialogue",
        Provider = context => _todaysSpecial is null
            ? null
            : $"Today the farmer's kitchen is famous for {_todaysSpecial}."
    }));

    // React when an arc ends.
    _subscriptions.Add(ec.RegisterStoryArcHook(new StoryArcHookRegistration
    {
        OwnerModId = ModManifest.UniqueID,
        ModeId = ModeId,
        OnArcResolved = arc => Monitor.Log($"Arc '{arc.ArcTitle}' resolved: {arc.ResolutionSummary}", LogLevel.Info)
    }));
}
```

The mode shows up in AliveNpcs' mode picker. A mode registered from code has no translation files, so its scenes should use inline `premise` and `text` rather than i18n keys.

### Callbacks

- Methods returning `IDisposable` stay registered until you dispose the handle.
- **Keep callbacks fast and side-effect free.** Prompt providers run while AliveNpcs builds a prompt, which may not be on the game thread. Read values you prepared earlier (in `DayStarted`, for example) rather than walking the world.
- A dynamic prompt block or runtime hook that throws three times in a row is turned off for the session, with an error in the log.

---

## Which calls need the mode to be active

Three calls are refused (they return false) unless the mode named in the registration is the player's active mode:

| Call | When the mode isn't active |
|---|---|
| `RecordGossipInteraction` | The note isn't recorded. |
| `ProposeStoryArc` | The proposal is rejected. |
| `RecordCanonicalLore` | The lore isn't recorded. |

Everything else can be registered at any time and takes effect once the player picks the mode. For gossip that shouldn't depend on a mode, use `InjectGossip` from the [core API](core-api.md#gossip).

---

## Method reference

| Method | What it does |
|---|---|
| `RegisterExperimentalContentPackage(registration)` | Adds a package. `PackageId` is required. |
| `RegisterExperimentalMode(registration)` | Adds a mode with its prompt blocks (`PromptBlock` → dialogue, `GossipPromptBlock` → gossip, `GossipInteraction`/`GossipInteractions` → gossip interactions) and its scene and scene image folders. |
| `RegisterDialogueAction(registration)` | Adds an option to the conversation menu for a mode. |
| `RegisterDialogueActionAvailabilityProvider(registration)` | Decides, per conversation, whether a dialogue action shows up. Receives `NpcName`, `Hearts`, `LocationName`, `TimeOfDay`. |
| `RegisterDynamicPromptBlock(registration)` | Prompt text computed at build time. `Target`: `dialogue`, `greeting` (`intro`, `daily_greeting`), `gossip` (`gossip_analysis`), `gossip_interaction` (`gossip_interactions`). An empty `ModeId` (the default), null or `"*"` runs the block in every mode, even when no mode is active. `MaxChars` defaults to 1800. |
| `RegisterNpcPersonalityContribution(registration)` | Extra personality text for a villager (see the content pack equivalent). |
| `RegisterNpcMetadata(registration)` | Facts about a villager, written into prompts. |
| `RegisterArcItemCatalog(registration)` | Items story arcs may ask for. |
| `RegisterStoryArcTemplate(registration)` / `RegisterStoryArcTemplateDirectory(registration)` | Story arc templates; the directory form reads every `*.json` in a folder. |
| `ProposeStoryArc(registration)` | Suggests a story arc for the current save. Mode must be active. |
| `RegisterStoryArcHook(registration)` | Callbacks: `OnArcCreated`, `OnObjectiveCompleted`, `OnArcCompletable`, `OnArcResolved`, `OnArcFizzled`. Each receives a `StoryArcHookContext` (`ArcId`, `ArcTitle`, `Status`, `InvolvedNpcs`, `CompletedObjectiveId`, `SelectedChoiceId`, `ResolutionSummary`, `LoreTags`, `CurrentDay`). |
| `RegisterModeScopedRuntimeHook(registration)` | SMAPI lifecycle callbacks that only run while your mode is active: `OnGameLaunched`, `OnSaveLoaded`, `OnDayStarted`, `OnTimeChanged`, `OnUpdateTicked`, `OnSaving`, `OnDayEnding`. Each receives `ActiveModeId`, `SaveId`, `PlayerName`, `Day`, `Season`, `Year`, `TimeOfDay`, `NewTime`. |
| `RecordCanonicalLore(registration)` | Adds a lore statement. Mode must be active. |
| `RecordGossipInteraction(registration)` | Adds a note to tonight's gossip for a mode. Mode must be active. |
| `GetNpcContextSnapshot(npcName)` | `NpcName`, `DisplayName`, `CurrentLocation`, `Hearts`, `ActiveExperimentalModeId`, `ActiveStoryArcIds`. |
| `GetActiveStoryArcs()` / `GetStoryArcsForNpc(npcName)` | Active arcs: `Id`, `Title`, `Description`, `Status`, `InvolvedNpcs`, `Objectives`, `ResolutionSummary`. |
| `IsExperimentalModeActive(modeId)` / `TryGetActiveExperimentalMode(out mode)` / `GetRegisteredExperimentalModes()` | Which mode is active and which exist. |
| `GetCapabilities()` | Feature flags. In 1.7.0 everything is `true` except `RuntimeSceneEventJson`. |
| `RegisterSceneEventJson(...)` | Reserved: always returns false in 1.7.0. Ship scene files through `SceneEventDirectories` instead. |

A mode registered from code supports fewer fields than a content pack: there's no API equivalent of a pack's `safety` profile, for instance. If your mode needs those, ship a content pack and use code only for the dynamic parts.

---

## Multiplayer coordinator

`IAliveNpcsMultiplayerCoordinator` is for a multiplayer addon. Without one, AliveNpcs runs as in single player on each machine. With one, AliveNpcs asks it before doing village-wide work, so that only the host runs gossip, story arcs, letters and similar jobs, and it tells the coordinator when there's something to send to the other players.

Implement the interface and register it once AliveNpcs has loaded:

```csharp
private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
{
    // global:: so it isn't confused with your own ModEntry class.
    global::AliveNpcs.ModEntry.Instance?.RegisterMultiplayerCoordinator(new MyCoordinator(Helper));
}
```

Only one coordinator is used; registering another replaces it. `AliveNpcs.ModEntry.Instance` is AliveNpcs' mod entry, not a stable API, so expect to adjust this call when AliveNpcs updates.

### Host and job gating

| Member | Return |
|---|---|
| `bool IsHost { get; }` | True on the host, false on farmhands. |
| `bool ShouldRunVillageWideJobs()` | False on farmhands, so they skip gossip analysis, story arcs, social moments, letters, weekly news and mood updates. |
| `bool ShouldSaveVillageWideData()` | False on farmhands: only the host saves village-wide state. |
| `bool ShouldLoadVillageWideData()` | False on farmhands: they get the state from the host instead of disk. |
| `bool ShouldRunDiaryJob()` | Whether this player writes their own diary entry at the end of the day. True for everyone: each player has their own conversations. |

### Things to broadcast (called on the host)

| Member | When |
|---|---|
| `OnVillageWideAIJobCompleted(jobType, result)` | A village-wide AI job finished; send the result to farmhands. |
| `OnFlowJobApplied(jobId)` | End-of-day results were applied on the game thread; send the updated state. |
| `OnFlowJobNotifications(jobId, notificationMessages, gossipChatMessage)` | The notifications and gossip chat message for that job, so farmhands see the same ones. |
| `OnArcStatusChanged(arcId, newStatus)` | An arc changed status locally; send it to the other players. |
| `OnRelationshipChanged(npcName, hearts, relationshipStatus, lastEmotion)` | A player finished a conversation; send the relationship change. |

### Requests from farmhands

Return true when you handled it (a farmhand sent the request to the host), false in single player or on the host so AliveNpcs does it locally.

| Member | Purpose |
|---|---|
| `TryRequestArcConclusion(arcId)` | Ask the host to write an arc's conclusion. |
| `TryRequestArcDeletion(arcId)` | Ask the host to delete an arc. |
| `TryUploadArcProgress(arcId, objectiveId, approach, completedDate, playerName)` | Send a completed objective to the host, so the host's copy doesn't overwrite it the next morning. |
