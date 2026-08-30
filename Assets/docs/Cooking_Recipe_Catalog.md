# Cooking Recipe Catalog

All recipes use three cooking slots. Each recipe attracts one monster from its weighted pool. The common result has weight 3, uncommon has weight 2, and rare has weight 1.

| Recipe | Ingredients | Monster pool (common -> rare) | Cook time |
| --- | --- | --- | --- |
| 3 Red Berries | Berry x3 | Cooconi, Leafy, Cotty | 10s, 12s, 15s |
| 3 Purple Berries | Purple Berry x3 | Mushy, Dewli, Moolo | 10s, 12s, 15s |
| Woodland Forage | Apple + Berry + Mushroom | Pebby, Woody, Rooty | 14s, 17s, 20s |
| Mushroom Medley | Mushroom + Red Mushroom + Green Mushroom | MushRibbit, Molli, Moss | 16s, 19s, 22s |
| Harvest Stew | Pumpkin + Eggplant + Tomato | Kabuto, Antie, Arcant | 18s, 21s, 24s |
| Sweet Orchard Blend | Apple + Honey Butter + Berry | Strawli, Leafbag, Bolla | 18s, 21s, 24s |
| Bamboo Feast | Bamboo Shoot x2 + Honey Butter | Bambat, Bambam, Bamurtle | 22s, 25s, 28s |
| Crystal Glow Soup | Crystal + Glowing Mushroom + Green Mushroom | Wispbo, Beo, Cacu | 26s, 29s, 32s |
| Moon Garden Elixir | Purple Berry + Glowing Mushroom + Crystal | Lotus, LilyPadle, Pipcher | 28s, 31s, 34s |

## Design notes

- Leafy remains unlocked by default; obtaining Leafy from cooking raises its collection star instead of adding a permanent duplicate.
- Existing recipes keep their original monster as the highest-weight result: Cooconi for red berries and Mushy for purple berries.
- Later-biome ingredients produce longer cooking times and rarer monster pools.
- `allowDuplicateMonsters` remains disabled. Duplicate results are handled by the collection star/badge flow.
- Run `TinyMonsterKeeper > Tools > Validate Cooking Recipes` after changing any recipe.
- Run `TinyMonsterKeeper > Automation > Setup All Cooking Recipes` in `GameplayScene` after adding or removing recipe assets.
