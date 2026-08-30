# Tiny Monster Keeper - Gameplay Content Balance Sheet

This sheet records the current launch content and the intended zone progression.
Values marked **Current** are serialized in the project. Values marked
**Recommended** describe the next placement pass and should not be treated as
already present in `GameplayScene`.

## 1. Map Resource Production

| Ingredient | Item ID | Current production time | Display | Yield | Current source zones | Recommended source zones |
| --- | --- | ---: | --- | ---: | --- | --- |
| Red Berry | `berry` | 45s | `45s` | 1 | Zone00 | Zone00 |
| Apple | `apple` | 90s | `1:30` | 1 | Zone00, Zone01 | Zone00, Zone01, Zone07 |
| Red Mushroom | `red_mushroom_harvest` | 120s | `2:00` | 1 | Zone00 | Zone00, Zone04 |
| Purple Berry | `purple_berry` | 120s | `2:00` | 1 | Zone01 | Zone01, Zone12 |
| Green Mushroom | `green_mushroom_harvest` | 180s | `3:00` | 1 | Zone01 | Zone01, Zone04, Zone09 |
| Pumpkin | `pumpkin_harvest` | 240s | `4:00` | 1 | Zone02 | Zone02, Zone08 |
| Eggplant | `eggplant_harvest` | 240s | `4:00` | 1 | Zone02 | Zone02, Zone08 |
| Tomato | `tomato_harvest` | 240s | `4:00` | 1 | Zone02, Zone08 | Zone02, Zone08 |
| Normal Mushroom | `mushroom_harvest` | 300s | `5:00` | 1 | Zone04, Zone05 | Zone04, Zone05, Zone09 |
| Bamboo Shoot | `bamboo_shoot_harvest` | 360s | `6:00` | 1 | Zone03, Zone06 | Zone03, Zone05, Zone06 |
| Honey Butter | `honey_butter` | 480s | `8:00` | 1 | Zone07 | Zone06, Zone07, Zone13 |
| Glowing Mushroom | `glowing_mushroom_harvest` | 720s | `12:00` | 1 | Zone10, Zone11, Zone12, Zone13 | Zone10, Zone11, Zone12, Zone13 |
| Crystal | `crystal_harvest` | 900s | `15:00` | 1 | Zone09, Zone10, Zone11, Zone12, Zone13 | Zone09, Zone10, Zone11, Zone12, Zone13 |

Countdown UI rules:

- Below one minute: show seconds, for example `45s`.
- One minute or longer: show `minutes:seconds`, for example `4:00` or `14:59`.
- Resource time is restored from Unix timestamp while the game is closed.

## 2. Current Cooking Recipes

Every recipe consumes exactly three ingredient units. One monster is selected
from the result pool. Weight `3` is common, `2` is uncommon, and `1` is rare.

| Recipe | Required ingredients | Common result | Uncommon result | Rare result | Current cook times |
| --- | --- | --- | --- | --- | --- |
| 3 Red Berries | Red Berry x3 | Cooconi, weight 3 | Leafy, weight 2 | Cotty, weight 1 | 10s / 12s / 15s |
| 3 Purple Berries | Purple Berry x3 | Mushy, weight 3 | Dewli, weight 2 | Moolo, weight 1 | 10s / 12s / 15s |
| Woodland Forage | Apple x1 + Red Berry x1 + Normal Mushroom x1 | Pebby, weight 3 | Woody, weight 2 | Rooty, weight 1 | 14s / 17s / 20s |
| Mushroom Medley | Normal Mushroom x1 + Red Mushroom x1 + Green Mushroom x1 | MushRibbit, weight 3 | Molli, weight 2 | Moss, weight 1 | 16s / 19s / 22s |
| Harvest Stew | Pumpkin x1 + Eggplant x1 + Tomato x1 | Kabuto, weight 3 | Antie, weight 2 | Arcant, weight 1 | 18s / 21s / 24s |
| Sweet Orchard Blend | Apple x1 + Honey Butter x1 + Red Berry x1 | Strawli, weight 3 | Leafbag, weight 2 | Bolla, weight 1 | 18s / 21s / 24s |
| Bamboo Feast | Bamboo Shoot x2 + Honey Butter x1 | Bambat, weight 3 | Bambam, weight 2 | Bamurtle, weight 1 | 22s / 25s / 28s |
| Crystal Glow Soup | Crystal x1 + Glowing Mushroom x1 + Green Mushroom x1 | Wispbo, weight 3 | Beo, weight 2 | Cacu, weight 1 | 26s / 29s / 32s |
| Moon Garden Elixir | Purple Berry x1 + Glowing Mushroom x1 + Crystal x1 | Lotus, weight 3 | LilyPadle, weight 2 | Pipcher, weight 1 | 28s / 31s / 34s |
| Failed mixture | Any unmatched three ingredients | No monster | - | - | 3s, ingredients consumed |

With weights `3 / 2 / 1`, the theoretical result chances are:

- Common: `50%`.
- Uncommon: `33.3%`.
- Rare: `16.7%`.

## 3. Recipe Unlock Progression

| Progress tier | Required zones | Newly practical recipe | Design purpose |
| --- | --- | --- | --- |
| Starter | Zone00 | 3 Red Berries | Teach harvest, inventory, cooking, and summon |
| Early 1 | Zone01 | 3 Purple Berries | Introduce fog unlock and a second monster family |
| Early 2 | Zone01 + Zone04 | Woodland Forage | Teach mixed ingredients |
| Mid 1 | Zone00 + Zone01 + Zone04 | Mushroom Medley | Encourage revisiting old zones |
| Mid 2 | Zone02 | Harvest Stew | Give the farm one complete local recipe |
| Mid 3 | Zone00 + Zone07 | Sweet Orchard Blend | Introduce timed production through BeeHome |
| Late 1 | Zone03/06 + Zone07 | Bamboo Feast | Connect west bamboo route to east apiary route |
| Late 2 | Zone01 + Zone10 | Crystal Glow Soup | Introduce cave resources |
| Late 3 | Zone01 + Zone12 | Moon Garden Elixir | Endgame cross-biome recipe |

## 4. Zone Design Sheet

| Zone | Unlock cost | Current interactive resources | Recommended final resources | Main gameplay purpose |
| --- | ---: | --- | --- | --- |
| Zone00 | Open | 4 Red Bush, 1 Apple Tree, 4 Red Mushroom | 3 productive Red Bush, 1 Apple Tree, 2 productive Red Mushroom | Starter hub and cooking plaza |
| Zone01 | 15 | 3 Purple Bush, 1 Apple Tree, 1 Green Mushroom | 2 productive Purple Bush, 1 Apple Tree, 1 Green Mushroom | Purple berry orchard |
| Zone02 | 35 | 2 Pumpkin, 2 Eggplant, 1 release Tomato | Keep current set; optionally add one second Tomato after playtest | Complete vegetable recipe zone |
| Zone03 | 60 | 1 Bamboo Shoot | 2 Bamboo Shoot | Bamboo route introduction |
| Zone04 | 90 | 1 Normal Mushroom | 2 Normal Mushroom, 1 Green Mushroom, 1 Red Mushroom | Mushroom woodland |
| Zone05 | 130 | 1 Normal Mushroom | 2 Bamboo Shoot, 1 Normal Mushroom | Deep bamboo transition |
| Zone06 | 180 | 1 Bamboo Shoot | 2 Bamboo Shoot, 1 BeeHome | Bamboo sanctuary |
| Zone07 | 240 | 1 BeeHome | 2 BeeHome, 1 Apple Tree | Main apiary meadow |
| Zone08 | 310 | 2 Tomato | 2 Tomato, 1 Pumpkin, 1 Eggplant | Reliable advanced farm |
| Zone09 | 390 | 2 Crystal | 1 Crystal, 1 Green Mushroom, 1 Normal Mushroom | Cave entrance |
| Zone10 | 480 | 1 Crystal, 2 Glowing Mushroom | Keep current set | Glowing cave |
| Zone11 | 580 | 1 Crystal, 2 Glowing Mushroom | 2 Crystal, 1 Glowing Mushroom | Crystal chamber |
| Zone12 | 700 | 1 Crystal, 1 Glowing Mushroom | Add 1 Purple Bush | Moon grotto |
| Zone13 | 850 | 1 Crystal, 2 Glowing Mushroom | 1 Crystal, 1 Glowing Mushroom, 1 BeeHome | Endgame sanctuary |

`Current interactive resources` describes the scene after adding
`Tomato_Map_Release` to Zone02. The recommended column is not fully implemented
yet and is intended for the next safe scene-placement pass.

## 5. Monster Coin Economy

| Monster group | Coin per tick | Tick interval | Maximum stored coin |
| --- | ---: | ---: | ---: |
| Leafy starter | 1 | 180s / 3m | 5 |
| Other 26 monsters | 1 | 240s / 4m | 8 |

This is a baseline economy. Before release, classify monsters into common,
uncommon, and rare economy tiers only after measuring actual player unlock
speed. Do not make rare monsters drastically faster coin printers; storage
capacity is the safer reward because it does not inflate active-play income.

## 6. Adjustment Rules

When changing this sheet and the game data, preserve these relationships:

1. A new zone must unlock at least one new recipe or materially improve access
   to an existing recipe.
2. A recipe ingredient that consumes two units should have at least two nodes
   available by the time the recipe is introduced.
3. Starter cooking must remain possible within the first five minutes.
4. A five-minute session should offer at least two useful actions.
5. Late resources may take longer, but the player should have other active
   harvest, coin, book, or monster interactions while waiting.
6. Unlock prices should be tuned from measured coin income, not by increasing
   both coin production and prices at the same time.
7. Test changes on a clean account and on an offline-return account before
   treating them as release values.

## 7. Project Sources

- Scene: `Assets/Scenes/GameplayScene.unity`
- Recipe assets: `Assets/ScriptableObjects/CookingRecipes/`
- Item assets: `Assets/ScriptableObjects/ItemData/`
- Monster assets: `Assets/ScriptableObjects/MonsterData/`
- Resource prefabs: `Assets/Prefabs/ResourcesNode/ResourcesNode_Map/`
- Balance audit: `Assets/docs/Release_Map_Zone_Economy_Audit.md`
