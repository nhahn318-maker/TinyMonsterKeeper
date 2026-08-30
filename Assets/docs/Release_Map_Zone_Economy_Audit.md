# Tiny Monster Keeper - Release Map, Zone, and Economy Audit

Updated from `Assets/Scenes/GameplayScene.unity`, the current prefabs, and the
cooking recipe catalog. This document is a balancing target, not an instruction
to move scene objects without a backup.

## 1. Release Readiness Finding

The core loop is already playable:

`harvest -> cook -> attract monster -> collect coins -> unlock fog -> discover new harvest`

The current content is sufficient for a small launch version: 14 map areas
(`Zone00_Start` plus Zone01-13), 13 ingredients, 9 recipes, and 27 recipe
monster results. The release blocker is progression balance rather than content
quantity.

Current serialized values are debug values:

- Every fog entry in `FogZoneManager` has `unlockCost = 0`.
- Red berry, purple berry, and apple regrow in 2 seconds.
- Most harvest nodes respawn in 10 seconds; crystal respawns in 5 seconds.
- Honey butter is the only resource with a longer 60-second production time.
- Most monsters produce 1 coin every 8 seconds and store only 5 coins.
- Zone09-13 repeat crystal and glowing mushroom heavily, while several middle
  ingredients have only one practical source.

These values are useful for testing but would let a player exhaust the launch
progression in one session and would make notifications/offline persistence
feel unnecessary.

## 2. Map Structure to Preserve

Keep Zone00 as the cooking and arrival hub. The current map expands in four
readable directions:

- East lower route: orchard, berry grove, farm, and apiary.
- West lower route: bamboo, woodland mushroom, and bamboo sanctuary.
- North route: cave and magical cave resources.
- South: summon path cinematic, not a farming zone.

Every zone should retain at least 40% open walkable ground. Place harvest nodes
around the outer third of a zone, never in the center of a path intersection or
directly on a fog unlock button. Leave roughly 1.5-2 world units between large
touch targets.

## 3. Recommended Zone Progression

The sequence below uses all current assets without requiring a new ingredient.
Each zone introduces one meaningful source and may repeat one older source to
avoid forcing excessive camera travel.

| Zone | Theme and role | Recommended harvest placement | Unlock cost |
| --- | --- | --- | ---: |
| 00 | Starter cooking garden | 3 red bushes, 1 apple tree, 2 red mushrooms | Open |
| 01 | Purple berry orchard | 2 purple bushes, 1 apple tree, 1 green mushroom | 15 |
| 02 | Vegetable garden | 2 pumpkins, 2 eggplants; add 1 tomato at the far edge | 35 |
| 03 | Bamboo entrance | 2 bamboo shoots, with big bamboo only as framing decor | 60 |
| 04 | Mushroom woodland | 2 normal mushrooms, 1 green mushroom, 1 red mushroom | 90 |
| 05 | Deep bamboo grove | 2 bamboo shoots, 1 normal mushroom | 130 |
| 06 | Bamboo sanctuary | 2 bamboo shoots; add 1 BeeHome near flowers | 180 |
| 07 | Apiary meadow | 2 BeeHome nodes, 1 apple tree | 240 |
| 08 | Advanced farm | 2 tomatoes, 1 pumpkin, 1 eggplant | 310 |
| 09 | Cave entrance | 1 crystal, 1 green mushroom, 1 normal mushroom | 390 |
| 10 | Glowing cave | 1 crystal, 2 glowing mushrooms | 480 |
| 11 | Crystal chamber | 2 crystals, 1 glowing mushroom | 580 |
| 12 | Moon grotto | 1 crystal, 1 glowing mushroom, 1 purple bush | 700 |
| 13 | Endgame sanctuary | 1 crystal, 1 glowing mushroom, 1 BeeHome | 850 |

### Placement corrections from the current scene

- Zone00 currently has four red bushes and four red mushrooms. Reduce the
  effective starter yield to three bushes and two mushrooms; extra objects can
  remain as non-interactive decor or be moved to Zone04.
- Zone01 is healthy but currently has three purple bushes. Two productive
  bushes are enough for release pacing; the third can unlock later or be decor.
- Zone02 currently has pumpkin and eggplant but no tomato. Add one tomato here
  so `Harvest Stew` becomes discoverable immediately after opening the zone.
- Zone03 and Zone06 each have only one bamboo shoot. Increase both to two because
  `Bamboo Feast` consumes two shoots in one cook.
- Zone07 already visually reads as an apiary. Keep BeeHome here and add a second
  producer only if its production time is increased as proposed below.
- Zone08 already contains two tomatoes. Add one pumpkin and one eggplant to make
  it a reliable advanced farm rather than another single-resource stop.
- Zone09-13 currently overuse crystal/glowing mushroom. Keep them as the biome
  identity, but add the listed older ingredient in Zones09, 12, and 13 so these
  spaces have different gameplay purposes.

## 4. Release Harvest Timers

Use short timers for the first recipe, medium timers for mixed recipes, and
longer timers only for late-biome ingredients. A first-time player must be able
to complete the first cooking loop during the first session.

| Ingredient | Current | Release target | Yield |
| --- | ---: | ---: | ---: |
| Red berry | 2s | 45s | 1 |
| Apple | 2s | 90s | 1 |
| Red mushroom | 10s | 2m | 1 |
| Purple berry | 2s | 2m | 1 |
| Green mushroom | 10s | 3m | 1 |
| Pumpkin | 10s | 4m | 1 |
| Eggplant | 10s | 4m | 1 |
| Tomato | 10s | 4m | 1 |
| Normal mushroom | 10s | 5m | 1 |
| Bamboo shoot | 10s | 6m | 1 |
| Honey butter | 60s | 8m | 1 |
| Glowing mushroom | 10s | 12m | 1 |
| Crystal | 5s | 15m | 1 |

Do not make all timers long immediately in the tutorial. On a fresh account,
starter nodes should begin ready. Offline Unix timestamp restoration should
continue to make elapsed time count while the game is closed.

## 5. Cooking Times

The current 10-34 second recipe times are appropriate for the first two tiers
but too similar across the full progression. Recommended launch values:

| Recipe | Target cook time |
| --- | ---: |
| 3 Red Berries | 10-15s by monster result |
| 3 Purple Berries | 20-30s |
| Woodland Forage | 30-45s |
| Mushroom Medley | 45-60s |
| Harvest Stew | 60-90s |
| Sweet Orchard Blend | 90-120s |
| Bamboo Feast | 2-3m |
| Crystal Glow Soup | 3-4m |
| Moon Garden Elixir | 4-5m |
| Invalid recipe | 3s, consumes ingredients, then fail feedback |

Keep per-monster duration variation within roughly 25% of the recipe baseline.
Randomly receiving a rare monster should feel exciting, not like an unexpected
punishment with a dramatically longer timer.

## 6. Coin Economy and Fog Costs

Fog cost cannot be balanced while monsters produce one coin every 5-8 seconds.
For release, use this baseline before tuning zone prices:

- Starter monster: 1 coin every 3 minutes, store up to 5.
- Common monster: 1 coin every 4 minutes, store up to 8.
- Uncommon monster: 1 coin every 3 minutes, store up to 10.
- Rare monster: 1 coin every 2 minutes, store up to 12.
- Star upgrades may increase storage by `+2` per tier, not production speed by
  a large multiplier.
- Leafy should provide enough early coins to unlock Zone01 in approximately
  10-15 active minutes, helped by a one-time tutorial reward of 10 coins.

The proposed fog costs target a compact game where Zone01-04 open in the first
day, Zone05-08 across days 2-4, and Zone09-13 across the following week. Final
costs must be validated against measured median coin income, not intuition.

## 7. Recipe Access Check

- Zone00 supports `3 Red Berries` immediately.
- Zone01 supports `3 Purple Berries` and contributes to `Woodland Forage`.
- Zone02 completes `Harvest Stew` after adding tomato.
- Zone04 completes `Mushroom Medley`.
- Zones06-07 complete `Bamboo Feast` and `Sweet Orchard Blend`.
- Zones09-10 complete `Crystal Glow Soup`.
- Zone12 completes `Moon Garden Elixir` without making the player cross the
  entire map for every attempt.

This ordering gives a new recipe approximately every one or two unlocks and
prevents an unlocked zone from feeling cosmetic.

## 8. Release Playtest Gates

Do not ship the economy until these tests pass with a clean account and a
returning account:

1. First monster is cooked within 5 minutes without waiting for a respawn.
2. Zone01 unlocks within 10-15 active minutes.
3. The player discovers at least three recipes during the first day.
4. No required recipe ingredient has only one source after its debut zone.
5. A five-minute session always offers at least two meaningful actions.
6. Returning after 30 minutes produces visible harvest and coin progress.
7. Returning after 8 hours does not overflow inventory or create excessive coin.
8. Unlocking a zone immediately reveals one new interaction or recipe path.
9. Touch targets do not overlap fog buttons, monsters, or the camera swipe area.
10. Analytics should record harvest, cook start/result, monster result, coin
    collection, and zone unlock before final economy tuning.

## 9. Safe Implementation Order

1. Commit and back up `GameplayScene` before any movement.
2. Move/add harvest nodes only; do not repaint Tilemaps in the same commit.
3. Change resource timers in prefabs and verify offline restoration.
4. Change monster coin intervals/storage, then apply fog costs.
5. Update recipe durations and test invalid cooking.
6. Run a clean-account progression test and a 24-hour offline-return test.
7. Tune values from telemetry, then lock the launch economy for release QA.
