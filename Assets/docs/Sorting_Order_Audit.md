# Tiny Monster Keeper - Sorting Order Audit

The project uses one `Default` sorting layer. Rendering order is divided into
small numeric bands so runtime Y sorting remains readable in the Inspector.

| Content | Sorting order | Rule |
| --- | ---: | --- |
| Ground and tilemaps | 0-10 | Static map base |
| Cloud shadows | 60 | Above ground, below gameplay objects |
| Y-sorted world objects | normally 100-900 | `500 - worldY * 32`, plus intentional local offset |
| Fog tilemaps | 1000 | Covers all world objects |
| Fog unlock visuals | 1010 | Remains visible and clickable above fog |
| Pickable item drops | 1100 | Always visible above fog and world objects |
| Screen UI | Canvas overlay | Independent from world sorting order |

## Y-Sort Standard

- `worldBaseOrder`: 500
- `unitsToOrder`: 32, matching the project's 32 PPU pixel grid
- `minOrder`: -900
- `maxOrder`: 900
- Formula: `500 + baseOrder - round(worldY * 32)`

Two existing scene objects retain a converted `worldBaseOrder` of 433 because
they previously used 9792 instead of 10000. This preserves their intentional
relative offset in the normalized scale.

## Current Audit Result

- 127 Y-sort configurations use 32 orders per world unit.
- 125 use world base 500; 2 intentional exceptions use 433.
- Serialized `GameplayScene` renderer orders range from 0 to 1100.
- No old 10000 Y-sort base or 20000/32000 drop order remains.

Use `TinyMonsterKeeper > Automation > Normalize Sorting Orders` after importing
or creating batches of monsters and world resource prefabs.
