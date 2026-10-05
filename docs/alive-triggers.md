# AliveNpcs AliveTrigger packs

*Seen and Heard* is the part of AliveNpcs where villagers notice what the farmer does near them: they remember it, it changes how they feel, it can turn into gossip, and sometimes a villager walks over to say something about it.

An AliveTrigger pack lets a content pack add its own Seen and Heard triggers with a single JSON file and no code. This document is the reference for the file format (AliveNpcs 1.7.0). For a step-by-step tutorial and a working pack, see the example repository [alive-npcs-trigger-example](https://github.com/LucasGatica/alive-npcs-trigger-example).

---

## Discovery

At startup AliveNpcs reads `alive-trigger.json` from the root of every installed SMAPI content pack whose `manifest.json` declares:

```json
"ContentPackFor": { "UniqueID": "Lucas.AliveNpcs", "MinimumVersion": "1.7.0" }
```

A pack needs nothing else. An Experimental Content pack may also ship an `alive-trigger.json`. Each pack logs `AliveTrigger pack '<name>': loaded N trigger(s).`; every rejected entry logs a warning and the rest of the file still loads.

## File

```json
{
  "Format": 1,
  "Triggers": [ { "Id": "...", "When": "...", "Summary": "..." } ]
}
```

`Format` must be `1`. At most 50 triggers are loaded per pack.

## Trigger fields

| Field | Type | Default | Description |
|---|---|---|---|
| `Id` | `string` | (required) | Unique within the pack. Letters, digits, `_`, `.`, `-`; up to 64 characters. |
| `When` | `string` | — | Game state query. Required unless `On` is set. Up to 500 characters. |
| `On` | `string` | — | Sensor event to attach to. Required unless `When` is set. |
| `Match` | `object` | `{}` | `On` only. Detail name → expected value; all must match. |
| `Summary` | `string` | — | The remembered fact. Required for `When` triggers; optional for `On` triggers, where it replaces the event's own text. Up to 200 characters. |
| `Reaction` | `string` | `""` | How the villager should react when speaking. Up to 300 characters. |
| `Npcs` | `string[]` | `[]` | Internal NPC names allowed to react. Empty means everyone. |
| `MinHearts` | `int` | `0` | Minimum hearts with the farmer (0–14). |
| `RangeTiles` | `number` | `8` | `When` only. Maximum distance between farmer and villager (1–30). |
| `Importance` | `number` | `5` | `When` only. 1–11, the same scale as `Grade` in `assets/seen-and-heard/event-types.json` in the AliveNpcs folder. |
| `GiveItem` | `string` | — | Qualified item id handed over after the villager has spoken. |
| `MaxPerDay` | `int` | `1` | Times the trigger may fire per in-game day (1–20). |
| `MemoryOnly` | `bool` | `false` | Remember the fact but never walk up to the farmer. |

`Summary` and `Reaction` accept `{farmer}`, `{npc}` and `{location}`. Both are flattened to one line and truncated, because they are inserted into AI prompts.

## `When` triggers

Checked once a second for the host while the player is free (no menu, dialogue, event or day transition). The condition is evaluated with `GameStateQuery.CheckConditions` using the farmer's current location and the local farmer. The trigger fires for the nearest eligible villager within `RangeTiles` that passes `Npcs` and `MinHearts`; that villager is the direct witness, and other villagers within `RangeTiles` also remember it. One trigger fires per check.

An unknown query key disables the trigger and logs a warning. The observation's action id is `trigger:<pack UniqueID>/<Id>`.

## `On` triggers

Applied to an observation made by a built-in sensor before it is recorded. The observation keeps its action id, witnesses, range and importance; the trigger replaces `Summary` when set and adds `Reaction`, `GiveItem` and `MemoryOnly`. Only the first matching trigger is applied. `Npcs` and `MinHearts` are checked against the event's subject and are ignored for events without one. A `When` on an `On` trigger must also hold at that moment.

| `On` | Subject | Details for `Match` |
|---|---|---|
| `gift` | recipient | `taste` (`loved`, `liked`, `neutral`, `disliked`, `hated`), `itemId`, `partner` (your partner standing nearby), `birthday` (`true`) |
| `consume` | — | `itemId`, `kind` (`alcohol`, `drink`, `food`), `alcohol` (`true`) |
| `bomb` | — | `radius` |
| `trash` | — | `can`, `owners` (comma-separated list) |
| `tool_hit`, `tool_near_miss` | villager | `weapon` (`true`, `false`) |
| `slingshot_hit`, `slingshot_near_miss` | villager | — |
| `room_locked`, `room_unlocked` | room owner | `owners`; `room_locked` also `attempt` (tries at that door today) |
| `room` | room owner | `owners` |
| `place` | resident, when it is someone's home | `action` (`enter`, `leave`), `place`, `owners`, `from`, `to` |
| `map` | — | `from`, `to` |
| `talk` | villager talked to | `secret` (`true`), `turns` |
| `purchase` | shop owner when present | `itemIds` (comma-separated list), `vendor` |
| `dating_proposal`, `marriage_proposal` | villager | `result` (`accepted`, `rejected`) |
| `sleep_outside` | — | `reason` (`2am`, `exhausted`) |

`subject` is also accepted as a `Match` key. Matching is case-insensitive; for a comma-separated detail the expected value has to equal one element. `purchase` and `sleep_outside` are memory-only, so `Reaction` and `GiveItem` have no effect on them. In `talk`, the villager being talked to never counts as a witness.

Without a trigger, whether a villager walks over about a built-in event depends on how strongly they feel about it (see `assets/seen-and-heard/meters.json` in the AliveNpcs folder): small things are only remembered until they pile up. An `On` trigger with `Reaction` or `GiveItem` skips that and sends the nearest witness over directly, as a `When` trigger does. An `On` trigger with only `Summary` keeps the feelings, and villagers remember the event in your words instead of the catalog's.

## Testing a pack

These SMAPI console commands help while writing a pack. None of them needs a restart.

### `alive_trigger lint [pack]`

Re-reads `alive-trigger.json` from disk and explains every entry the way the loader sees it. Without an argument it checks every installed pack; with one it matches the pack's `UniqueID` or name (a unique part of either is enough).

```text
> alive_trigger lint Author.RainPack
AliveTrigger pack 'Rain Pack' (Author.RainPack): 2 trigger(s) load, 1 rejected, 1 with an error, 0 with a warning.
  [rejected] trigger 'beer run': Id is required and may only use letters, digits, '_', '.' and '-'
  [ok] soaked  When "WEATHER Here Rain"  range 8  importance 5  hearts 2+
           info: When holds right now: no
  [error] typo  When "WETHER Here Rain"  range 8  importance 5
           error: When does not parse and the trigger would be disabled: ...
```

| Mark | Meaning |
|---|---|
| `[rejected]` | The entry is dropped at load. The rest of the file still loads. |
| `[error]` | The trigger loads but its `When` does not parse, so it is disabled the first time it is checked. |
| `[warn]` | The trigger loads and runs, but part of it has no effect: an unknown `GiveItem` or `Npcs` name, a `Match` key the event never sets, `Reaction` or `GiveItem` on a memory-only event, `Npcs` or `MinHearts` on an event without a subject. |
| `[ok]` | Nothing to fix. |

With a save loaded, each `When` also reports whether it holds at that moment, for the farmer's current location. Lint only reads the file. Use `alive_trigger reload` to apply it.

### `alive_trigger reload` and `alive_trigger list`

`reload` loads every pack again and replaces the running triggers. Daily counts and disabled triggers are reset. `list` (or `alive_sh triggers`) shows the triggers that are running, how often each fired today and whether one was disabled for an invalid `When`.

### `alive_sh fire <event> [npc] [key=value ...]`

Records an observation as if a sensor had made it, then reports the witnesses, whether the memory was stored and whether a villager is walking over. Stand near the villagers you want as witnesses first: range, line of sight and the busy checks all still apply. Host only.

```text
> alive_sh fire gift Abigail item=Amethyst taste=loved
> alive_sh fire trash can=Saloon
> alive_sh fire marriage_proposal Leah result=rejected
> alive_sh fire Author.RainPack/soaked Abigail
> alive_sh fire dance summary=Lucas danced alone in the square. importance=9
```

- `<event>` is any `On` event from the table above, `hurt`, `fishing`, or a loaded trigger as `<pack UniqueID>/<Id>` (the bare `Id` works when it is unique). `alive_sh events` lists them with their details.
- Words before the first `key=value` are the villager's internal name. Words after a `key=value` continue that value, so quotes are not needed.
- Firing a `When` trigger skips its condition, `MinHearts`, range to the farmer and daily limit, and needs the villager who notices it. Firing an `On` trigger fires its event with the trigger's `Match` filled in, so the trigger is applied by the normal path, including its `When`, `MinHearts` and daily limit.
- Every event also accepts `summary`, `reaction`, `give=<qualified item id>`, `memoryOnly=true`, `range=<tiles>` and `importance=<1-11>`.
- An unknown event name is refused unless `summary=` is given, in which case it is recorded as a custom event.

A fired observation is real: it is saved in the villagers' memory, feeds their feelings, can become gossip at the end of the day and can hand over an item. Use a test save.

### `alive_sh feelings [npc]`

Shows what each villager feels about you today: every meter's reading and level, how many times they already spoke about it, what it takes to reach the next level, which event types are already settled on one feeling, what they are keeping to bring up later and what will carry over to tomorrow. Useful together with `alive_sh fire` to see why someone did or did not walk over.

## Limits

- `MaxPerDay` is counted when the trigger fires, and the same trigger waits at least 3600 update ticks before firing again.
- Triggers only run for the host and only while Seen and Heard is enabled.
- Villagers that are leased by an approach, fishing with the farmer or disabled for AliveNpcs never react.
- An unknown `GiveItem` id is ignored with one warning; the trigger still fires.
