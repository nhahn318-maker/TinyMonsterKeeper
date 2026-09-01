# Tiny Monster Keeper - Audio Content Design Sheet

Audit date: 2026-08-31

## 1. Current State

- No runtime `.wav`, `.ogg`, `.mp3`, `.aiff`, or `.m4a` assets were found.
- No gameplay `AudioSource`, `AudioClip`, `AudioMixer`, or `PlayOneShot` usage was found.
- MainMenuScene and GameplayScene have AudioListeners only.
- Music and SFX settings currently change button sprites but do not control or
  persist any audio state.

The release target should be a small, cohesive sound set rather than one unique
clip for every monster or ingredient.

## 2. P0 Essential Sound List

These sounds are required for readable gameplay.

| ID | Sound | Trigger | Variants | Priority/notes |
| --- | --- | --- | ---: | --- |
| `ui_tap` | Soft wooden/leaf tap | Normal UI button | 2 | Quiet; never tiring |
| `ui_back` | Short soft close | Back/close panel | 1 | Lower pitch than tap |
| `ui_error` | Gentle blocked thunk | Insufficient coin/item, cooldown | 1 | Clear but not harsh |
| `ui_popup_open` | Paper/leaf unfold | Book, cooking, monster panel | 1 | Can share across panels |
| `item_harvest` | Leaf/pluck sound | Bush/tree/vegetable harvested | 3 | Randomize pitch slightly |
| `item_drop` | Tiny pop/bounce | Item appears on map | 2 | Separate from collection |
| `item_collect` | Bright pickup chime | Player taps dropped item | 3 | Most frequent reward sound |
| `coin_collect` | Small coin sparkle | Monster coin collected | 3 | Pitch can rise with amount |
| `fog_confirm` | Soft confirmation | Unlock dialog Yes | 1 | Before reveal starts |
| `fog_reveal` | Wide magical wind | Fog zone disappears | 1 | Important progression reward |
| `cook_add` | Ingredient plop | Item enters cooking slot | 3 | Short and tactile |
| `cook_remove` | Reverse plop | Item removed from slot | 1 | Optional if tap already clear |
| `cook_start` | Pot lid/bubble start | Cooking begins | 1 | Confirms ingredients consumed |
| `cook_loop` | Quiet simmer loop | Pot is actively cooking | 1 | Spatial, low volume, seamless |
| `cook_ready` | Warm three-note chime | Cooking timer completes | 1 | Must be recognizable off-camera |
| `cook_fail` | Small smoke/deflate | Invalid recipe finishes | 1 | Playful, not punitive |
| `summon_begin` | Magical aroma swell | Result collection begins | 1 | Leads into monster reveal |
| `summon_reveal` | Main success flourish | New monster appears | 1 | Strongest core-loop reward |
| `summon_duplicate` | Short reward sparkle | Existing monster gains star | 1 | Less grand than a new monster |
| `monster_tap` | Cute chirp | Monster selected | 3 | Shared pool for launch |
| `monster_feed` | Happy nibble/chirp | Feed succeeds | 2 | Pair with heart effect |
| `monster_play` | Happy bounce/chirp | Play succeeds | 2 | Distinct from feed |
| `friendship_star` | Rising sparkle | Star tier increases | 1 | Layer over duplicate result |

Minimum P0 delivery: 23 logical sounds, approximately 32 audio files including
variants. Several sounds can be synthesized from one source using pitch and
volume randomization.

## 3. Music and Ambience

| ID | Track | Scene/use | Recommended length |
| --- | --- | --- | ---: |
| `music_menu` | Gentle memorable theme | Main menu | 60-90s seamless loop |
| `music_garden_day` | Cozy light garden loop | Normal gameplay | 120-180s seamless loop |
| `music_cave_magic` | Softer mysterious variation | Zone09-13 camera region | 90-150s seamless loop |
| `amb_garden` | Birds, leaves, distant insects | Gameplay base ambience | 60-120s loop |
| `amb_cave` | Soft air, crystal shimmer | Cave zones | 60-120s loop |

For the first test build, `music_menu`, `music_garden_day`, and `amb_garden` are
enough. Cave music/ambience can follow after zone-based cross-fading exists.

## 4. P1 Polish Sounds

| Sound group | Suggested clips |
| --- | --- |
| Resource identity | crystal tap/shatter, mushroom poof, bamboo snap, honey pop |
| Monster life | idle chirp pool, sleep/snore, wake, footsteps or soft hops |
| Cooking detail | bubble variations, aroma whoosh, pot result collection |
| Book | page turn, card unlock, recipe discovered |
| Loading | Leafy/Dewli footstep loop or tiny running puffs |
| Environment | bee buzz near BeeHome, crystal hum, occasional wind gust |

Do not add continuous audio to all 27 monsters. It would become noisy when many
monsters share the garden. Use rare, distance-limited idle chirps with a global
cooldown and maximum simultaneous voice count.

## 5. Audio Mix Rules

- Mixer groups: `Master`, `Music`, `SFX`, `Ambience`, and `UI`.
- Music target: roughly -18 to -14 dB under gameplay.
- Ambience target: roughly -24 to -18 dB and never mask reward cues.
- UI/SFX peaks should remain controlled; avoid clipping when coin and popup
  sounds play together.
- Limit ordinary SFX to 6-8 simultaneous voices and monster idle voices to 1-2.
- Randomize frequent sounds by about +/-3% pitch and small volume variation.
- Cooking loop and environmental emitters should use spatial attenuation;
  critical UI/error/ready sounds should be non-spatial.
- Fade music between scenes instead of restarting it abruptly.
- Pause/mute gameplay audio correctly when the app loses focus.

## 6. Mobile Asset Format

- Keep editable masters as 44.1 or 48 kHz, 24-bit WAV outside or inside a
  dedicated source folder.
- Import short SFX as mono where stereo is unnecessary.
- Use Vorbis-compressed `.ogg`/Unity compressed clips for music and ambience.
- Use Decompress On Load for very short frequent UI SFX, Compressed In Memory
  for medium clips, and Streaming for long music.
- Every asset must have commercial-use license evidence or be original work.

## 7. Implementation Order

1. Create one persistent AudioManager and AudioMixer with saved Master/Music/SFX
   settings.
2. Make the existing Music and SFX toggles control real mixer groups and persist.
3. Add UI tap/back/error, harvest/drop/collect, and coin sounds.
4. Add the complete cooking and summon sound sequence.
5. Add menu/gameplay music and garden ambience with cross-fade.
6. Add P1 biome/resource detail only after testing the mix with many monsters.

## 8. Audio Acceptance Test

1. Music and SFX settings persist after app restart.
2. Muting SFX does not mute music; muting music does not mute UI/SFX.
3. No clipping occurs during summon, coin pickup, or multiple item collection.
4. Cook-ready feedback can be heard while the camera is elsewhere.
5. Twenty minutes of play does not make repeated pickup/UI sounds irritating.
6. Background/foreground transitions do not duplicate music or AudioListeners.
7. Low-end Android playback produces no obvious latency or frame spikes.

## 9. Researched CC0 Candidate Sources

Only candidates whose asset page explicitly states CC0 are included here.
Final selection still requires listening and mix testing in the game.

| Source pack | Candidate use | License | Source |
| --- | --- | --- | --- |
| Kenney Interface Sounds | UI tap, back, popup, confirm, error | CC0 | `https://kenney.nl/assets/interface-sounds` |
| Kenney RPG Audio | harvest, item handling, impacts, footsteps | CC0 | `https://kenney.nl/assets/rpg-audio` |
| OpenGameArt 7 Assorted UI SFX | menu confirm/navigate/error, pickup, star gain | CC0 | `https://opengameart.org/content/7-assorted-sound-effects-menu-level-up` |
| OpenGameArt Boiling Water Loops | quiet cooking-pot loop | CC0 | `https://opengameart.org/content/boiling-water-loops` |
| OpenGameArt Magic Spell SFX | fog reveal, summon begin/reveal | CC0 | `https://opengameart.org/content/magic-spell-sfx` |
| OpenGameArt Gem Collect SFX | crystal pickup or rare result | CC0 | `https://opengameart.org/content/gem-collect-sfx` |

Recommended first prototype mapping:

| Game sound ID | Candidate source |
| --- | --- |
| `ui_tap`, `ui_back`, `ui_popup_open`, `ui_error` | Kenney Interface Sounds |
| `item_harvest`, `item_drop` | Kenney RPG Audio |
| `item_collect` | Assorted UI SFX item pickup, with 2 pitch variants |
| `coin_collect` | Kenney Interface/RPG bright impact or pickup |
| `cook_loop` | `cooking_with_cover_01.ogg`, trimmed and softened |
| `cook_ready`, `friendship_star` | Assorted UI SFX ability/level-up variants |
| `fog_reveal`, `summon_begin`, `summon_reveal` | Magic Spell SFX, layered or pitch-adjusted |
| `crystal_collect` | `gem-gather-stereo.wav` or a mono conversion |

Avoid mixing Pixabay and Freesound clips into the first pass unless a required
sound cannot be covered by the CC0 packs above. Their per-file provenance and
license evidence take more time to audit, while these candidates have explicit
asset-page license declarations.

For every downloaded source, store a small provenance record containing the
original filename, source URL, creator, license, download date, edits made, and
the final in-game sound ID.
