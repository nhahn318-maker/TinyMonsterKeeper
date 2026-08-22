# Current Gameplay Map Layout

Source scene: `Assets/Scenes/GameplayScene.unity`.

This is a read-only layout record made from the scene Tilemaps and placed
objects. Coordinates are world-space approximations intended for map planning,
not a replacement for the scene itself.

## Overall Shape

```text
                              NORTH

                         [ Zone05: Cave ]
                         crystal / cave moss
                                |
                    Zone04      |      Zone02
                 bamboo / moss  |  pumpkin / eggplant
                       |         |         |
                    Zone03 ---- [ Zone00 ] ---- Zone01
                   bamboo shoot   cooking pot   purple berries
                       |             |
                       +------ Summon path -----+
                              arrival gate

                              SOUTH
```

The current map is a hub-and-branches layout. `Zone00_Start` is the central
garden. The eastern branch is the berry and vegetable route, the western branch
is the bamboo route, and the northern route ends in the cave. The separate
summon path enters the hub from the south.

## Zone00_Start: Central Hub

- Ground Tilemap cell bounds: `x -10..8`, `y -6..4` (`88` painted cells).
- Cooking Pot: approximately `(-0.10, 0.16)`.
- Arrival gate: approximately `(0.13, -4.16)`.
- Garden arrival spawn point: approximately `(0.16, -4.73)`.
- Red berry bushes: approximately `(-2.13, -1.39)`, `(-0.80, -1.40)`,
  `(-1.46, -1.93)`, `(1.92, 1.71)`.
- Apple tree: approximately `(1.42, -1.62)`.
- Supporting decor: red mushrooms, logs, small/big stone, grass clumps.

Role: first playable area, cooking hub, starter berries and apple collection,
and arrival point for cooked monsters.

## Zone01: East Berry Grove

- Ground Tilemap cell bounds: `x -10..8`, `y -6..4` (`36` painted cells).
- Fog Tilemap cell bounds: `x -3..8`, `y -6..4` (`36` painted cells).
- Unlock button: approximately `(6.66, -3.11)`.
- Purple berry bushes: approximately `(4.40, -2.41)` and `(5.24, -2.41)`.
- Green mushroom: approximately `(5.66, -2.92)`.
- Supporting decor: white flowers, fallen log, grass clumps.

Role: first locked east expansion and the source of purple berries.

## Zone02: East Harvest Garden

- Ground Tilemap cell bounds: `x -10..8`, `y -6..4` (`30` painted cells).
- Fog Tilemap cell bounds: `x -3..8`, `y -6..4` (`30` painted cells).
- Unlock button: approximately `(6.58, 2.42)`.
- Pumpkins: approximately `(6.74, 1.46)`, `(7.54, 1.46)`, `(7.12, 0.94)`.
- Eggplant: approximately `(5.30, 2.02)`.
- Supporting decor: pink flower and grass clumps.

Role: second east expansion, vegetable harvest area.

## Zone03: West Bamboo Clearing

- Ground Tilemap cell bounds: `x -10..8`, `y -6..4` (`30` painted cells).
- Fog Tilemap cell bounds: `x -10..8`, `y -6..4` (`35` painted cells).
- Unlock button: approximately `(-6.93, -2.87)`.
- Big bamboo: approximately `(-8.27, -3.47)`.
- Bamboo shoot: approximately `(-5.50, -3.15)`.
- Supporting decor: grass bamboo, small bamboo, Japanese-style stone.

Role: first locked west expansion and source of bamboo shoot.

## Zone04: West Mossy Bamboo Area

- Ground Tilemap cell bounds: `x -10..8`, `y -6..4` (`36` painted cells).
- Fog Tilemap cell bounds: `x -10..8`, `y -6..4` (`42` painted cells).
- Unlock button: approximately `(-7.07, 1.12)`.
- Big bamboo: approximately `(-5.61, 0.26)`, `(-4.26, 2.64)`,
  `(-8.17, 2.72)`.
- Mushroom: approximately `(-4.59, 0.52)`.

Role: second west expansion, bamboo-heavy transition toward the cave.

## Zone05: Northern Crystal Cave

- Ground Tilemap cell bounds: `x -10..8`, `y -6..11` (`56` painted cells).
- Fog Tilemap cell bounds: `x -10..8`, `y -6..11` (`56` painted cells).
- Unlock button: approximately `(-0.23, 6.49)`.
- Crystal cluster: approximately `(-0.59, 6.58)`.
- Glowing mushroom: approximately `(0.73, 5.77)`.
- Cave decor: cave grass `(1.02, 6.21)`, big moss stone `(0.74, 6.75)`,
  single stalagmite `(0.15, 7.26)`, moss stone `(-0.86, 7.56)`, and
  stalagmite cluster `(1.63, 7.78)`.

Role: final currently placed biome, crystal and glowing mushroom collection.

## Summon Path Area

- Parent offset: approximately `(0, -2.26)`.
- Start point: approximately `(0.97, -11.35)`.
- End point: approximately `(1.04, -7.38)`.
- Visual content: gate, path ground, bushes and trees.

Role: cinematic entry lane for a newly attracted monster. It should visually
connect to the Zone00 arrival gate rather than act as an isolated map island.

## Current Layout Issues To Solve In A Future Prototype

1. The painted ground zones are visibly separated by large empty space.
2. Zone05 is north of the hub, but it does not yet have a strong visible path
   from either side branch.
3. Zone01/Zone02 and Zone03/Zone04 read as disconnected rectangles before fog
   is removed.
4. The summon path is visually strong but needs a continuous transition into
   the central hub.
5. Environment density is currently low outside the placed resource nodes;
   this should be solved with sprite-only decor layers before moving gameplay
   resources or changing navigation.
