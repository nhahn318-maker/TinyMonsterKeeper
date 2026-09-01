# Tiny Monster Keeper - Store Release Readiness Audit

Audit date: 2026-08-31

This document distinguishes a buildable Android test from a complete store
release. The current APK builds, installs, launches, and contains the main loop:

`harvest -> cook -> summon monster -> interact/collect coins -> unlock zone`

Current content is adequate for a small first release: 14 zones, 13 ingredients,
9 recipes, and 27 monsters. The remaining work is primarily onboarding,
production validation, account UX, telemetry, and store packaging.

## 1. Current Implemented Gameplay

This section records functionality that currently exists in the project. It
does not include proposed combat, daily quests, achievements, decorations, or
other post-launch ideas.

### Player flow and scenes

- The boot flow is `SplashScene -> MainMenuScene -> LoadingScene -> GameplayScene`.
- The main menu offers guest entry and a settings panel. Google sign-in is still
  presented but is not a complete player-facing Google account flow.
- Loading uses a progress bar and the existing Leafy character presentation.
- The Android application has a configured icon, splash presentation, portrait
  orientation, and a buildable debug APK.

### Map and zone progression

- Gameplay takes place on one large top-down garden map with camera dragging.
- Zone00 is open initially. Zone01 through Zone13 are covered by fog and use
  increasing coin unlock costs from 15 to 850.
- Locked fog blocks monster navigation and resource interaction. Unlocking a
  zone removes its fog and persists the result.
- Unlock confirmation and feedback use the bottom safe area so controls remain
  visible above Android navigation UI.
- Map objects and monsters use Y-based sorting where appropriate, with explicit
  sorting exceptions for overlays, drops, fog, bubbles, and UI.

### Harvest and inventory

- The map currently supplies 13 ingredient types through bushes, trees,
  mushrooms, vegetable plants, bamboo, BeeHome, and crystal nodes.
- Resource nodes display ready/countdown states, spawn clickable item drops, and
  respawn from Unix timestamps, including elapsed time while the app is closed.
- Clicking a dropped item adds it to inventory and shows collection feedback.
- Inventory quantities are consumed by cooking and restored through the save
  system.

### Cooking and recipes

- The cooking panel accepts three ingredient units and matches them without
  depending on ingredient order.
- Nine valid recipes are configured. Each recipe has common, uncommon, and rare
  monster results with weights `3 / 2 / 1`.
- Cooking has animation, progress display, countdown, completion bubble, offline
  timestamp restoration, and a click-to-collect result step.
- An unmatched three-item mixture still cooks for three seconds, consumes its
  ingredients, then shows the failed bubble and localized failure notice.
- Collecting a successful result attracts and summons the selected monster into
  the garden. Duplicate summons increase the monster's collection/star state.

### Monsters and collection

- The project contains 27 collectible monsters with existing idle/movement
  presentation and garden roaming.
- The collection book displays locked/unlocked monster cards and collection
  progress across multiple pages.
- Summoned monsters remain in the garden and their positions are persisted.
- Clicking a monster opens its interaction panel. The player can feed it using
  berries or Play with it to increase friendship.
- Play has a persisted Unix-timestamp cooldown. Friendship and duplicate/star
  progression are included in saved state.

### Coin economy

- Monsters generate coins over time and store them until the player taps the
  monster/coin bubble to collect them.
- Leafy currently produces one coin every three minutes and stores five. Other
  monsters produce one every four minutes and store eight.
- Stored coin and the next production timestamp persist through offline time.
- Coins are spent to unlock fog zones, forming the current long-term progression
  loop.

### Save, account, and offline behavior

- Save version 2 covers inventory, monster collection, garden monsters,
  friendship, Play cooldown, coins, fog zones, cooking, resource timers,
  discovered recipes, and failed cooking state.
- Runtime supports local persistence and Firebase anonymous/cloud save plumbing.
  Production account recovery, Google linking, conflict UX, and account deletion
  are not complete release features yet.
- Cooking, resource respawn, Play cooldown, and coin production use Unix
  timestamps so elapsed offline time can be reconciled after reopening the game.

### Audio and feedback

- Background music and SFX are loaded by a persistent audio manager.
- Current effects cover UI/monster clicks, harvesting, item/coin collection,
  cooking completion, cooked-result collection, and monster star increases.
- Music and SFX settings now control playback and persist through `PlayerPrefs`.
- The Alerts toggle remains visual only because local notifications are not yet
  implemented.

### Current gameplay boundary

- There is currently no battle system, enemy progression, equipment, quests,
  achievements, decoration placement, advertising, or in-app purchase loop.
- The playable loop ends after unlocking all zones, discovering recipes, and
  collecting/upgrading monsters; a clearer end-of-content goal is still needed.

## 2. P0 - Must Fix Before Store Submission

### Gameplay and UX

- Add a first-session tutorial that points to one ready red bush, inventory,
  cooking pot, result collection, monster coin collection, and Zone01 unlock.
  The first monster must be obtainable in under five minutes without guessing.
- Add clear feedback for insufficient coins, full/empty ingredient slots,
  unavailable harvests, cooking in progress, failed recipes, full monster coin
  storage, and blocked fog areas. Verify every message on a real phone.
- Define an end-of-content state after Zone13/all monsters. The game needs a
  collection completion message or continuing goal instead of silently ending.
- Test the complete clean-account progression. Current balance values are
  serialized, but they have not been validated by measured full-run playtests.
- Test touch priority across camera dragging, fog buttons, monsters, harvest
  nodes, drops, cooking UI, and book UI on at least a small and tall phone.
- Implement Android Back behavior for dialogs/panels and an intentional exit
  path. No Back/Escape handler is currently present.

### Broken or misleading UI

- `ButtonGoogle` is active and interactable but has no click listener. Either
  implement real Google account linking/sign-in or hide the button for launch.
  Anonymous Firebase auth is not equivalent to Google sign-in.
- Main menu Music and SFX toggles now control persistent playback. The Alerts
  toggle still changes only its sprite; connect it to notification permission
  and scheduling, or hide it for launch.
- Add a player-facing save/account screen showing local/cloud status, account
  type, retry state, and destructive reset/delete confirmation. The existing
  reset component is a developer tool, not release account UX.

### Save reliability

- Run a save matrix covering fresh install, app force-stop, background/foreground,
  device reboot, no network, reconnect, corrupted local JSON, cloud failure,
  app update, and device clock changes.
- Verify all version-2 fields end-to-end: inventory, collection, garden monster
  positions, friendship, Play cooldown, stored/next coin timestamps, fog zones,
  cooking, resource timers, discovered recipes, and failed mixes.
- Lock Firestore production rules to the authenticated user path and test denied
  cross-user access. Never release with Firestore test-mode rules.
- If account creation/linking is offered, implement in-app account and associated
  cloud-data deletion. A local `PlayerPrefs.DeleteAll()` reset alone is not
  account deletion.

### Android release build

- Replace `DefaultCompany` with the final studio/developer name.
- Create a release upload keystore, back it up securely, and never commit its
  password. Keep package ID `com.nhahn.tinymonsterkeeper` only if it is final.
- Build a non-Development Android App Bundle (`.aab`), not the debug APK.
- Use IL2CPP and include ARM64. The current architecture mask includes ARMv7 and
  ARM64, but scripting backend is still the project default rather than an
  explicit Android IL2CPP release configuration.
- Increase `versionCode` for every uploaded build and choose a deliberate
  player-facing version name. Current values are `1` and `1.0`.
- Confirm target API 36 for submissions from 2026-08-31 onward. The current
  automatic target produced API 36 in the test APK, but release CI/build must
  verify it rather than assume it.
- Upload the AAB to Play internal testing and resolve every pre-launch report,
  native-library, permission, obsolete API, and device compatibility warning.

### Privacy and store policy

- Publish a privacy policy covering Firebase anonymous authentication,
  Firestore save data, analytics (if added), retention, deletion, and contact.
- Complete Play Console Data safety accurately, including Firebase SDK behavior.
- Decide whether the app is directed at children. Complete target audience,
  content rating, ads declaration, and Families requirements accordingly.
- Inventory licenses for all art, fonts, music, SFX, plugins, and Firebase/Unity
  SDKs. Keep proof that every release asset permits commercial distribution.
- Prepare final store listing: app name, short/full description, icon, feature
  graphic, phone screenshots from the real build, support email, category, and
  privacy-policy URL.

## 3. P1 - Required For A Credible Launch

- Add gameplay analytics events for tutorial steps, harvest, cook start/result,
  failed mix, monster result/duplicate, feed/play, coin collection, zone unlock,
  session duration, and progression time. Economy should be tuned from these
  events, not intuition.
- Add crash and non-fatal error reporting. Firebase packages are present, but no
  production Crashlytics integration was found.
- Add a lightweight goal layer: next recipe hint, collection progress, or zone
  objective. The core loop exists, but new players currently have little guidance
  about what to pursue next.
- Add recipe discovery presentation in the book and explain common/uncommon/rare
  outcomes without exposing confusing raw weights.
- Add duplicate-monster value so an unlucky duplicate remains rewarding, for
  example stars, friendship capacity, cosmetics, or a small guaranteed resource.
- Complete the remaining audio pass for failed cooking, summon rarity, fog
  unlock, ambience balancing, and independent music/SFX volume levels.
- Verify text size, contrast, safe areas, notch/cutout handling, touch target size,
  pixel filtering, and landscape/portrait policy on representative resolutions.
- Establish performance budgets and profile GameplayScene on low-end Android:
  frame time, memory, GC allocations, overdraw, loading time, battery use, and
  Firebase behavior offline.

## 4. P2 - Post-Launch Retention Improvements

- Optional local notifications for completed cooking/resources, only after
  permission UX and settings are implemented.
- Daily/weekly goals, rotating requests, cosmetic decoration, achievements, or
  additional late-game sinks after the base economy is stable.
- Localization. Current user-facing database is English-only; do not advertise
  Vietnamese or other languages until all runtime text is externalized.
- Cloud account linking/recovery across devices after anonymous-save migration
  and conflict resolution are proven.
- Remote-configured economy values only after deterministic local defaults and
  rollback behavior exist.

## 5. Release Test Gates

The store candidate is ready only when all gates pass:

1. Zero compile errors and zero missing scripts/references in all build scenes.
2. Clean install completes tutorial and first summon without developer help.
3. Zone01 unlocks in the intended 10-15 active minutes.
4. Full Zone01-13 progression is completable with no missing ingredient path.
5. Save matrix passes locally and with production Firestore rules.
6. A 24-hour offline return restores timers without duplication or negative time.
7. Twenty repeated cooking/summon cycles produce no stuck pot or lost inventory.
8. All panels support Android Back and no UI blocks camera/gameplay permanently.
9. Thirty-minute low-end-device run has no crash, runaway memory, or severe heat.
10. Release AAB passes Play internal test and pre-launch report.
11. Privacy policy, Data safety, content rating, licenses, and listing are complete.
12. Final smoke test is performed from the exact signed AAB delivered by Play.

## 6. Recommended Implementation Order

1. Hide or implement Google login; make settings real; add Android Back handling.
2. Build the first-session tutorial and next-goal guidance.
3. Run clean-account and offline save tests; fix every blocker found.
4. Add analytics and crash reporting, then conduct economy playtests.
5. Complete privacy/account deletion and production Firebase rules.
6. Configure final identity, IL2CPP/ARM64, keystore, versioning, and release AAB.
7. Complete listing assets and Play Console declarations.
8. Use internal testing, closed testing, and staged production rollout.

## 7. Current Verified Strengths

- Android debug APK builds, installs, launches, and reaches Unity activity.
- Build scene order is Splash, MainMenu, Loading, Gameplay.
- Fog costs are serialized as 15 through 850 for Zone01-13.
- Save version 2 contains offline Unix timestamps for cooking, resources, Play
  cooldown, and next monster coin production.
- Fog unlock and garden monster state are connected to the runtime save binder.
- Android minimum SDK is 23 and the latest test artifact targeted API 36.
- Both ARMv7 and ARM64 are selected by the current architecture mask.

## 8. Known Documentation Debt

- `Release_Map_Zone_Economy_Audit.md` still describes old debug timers and zero
  fog costs, while the scene and balance sheet contain newer release values.
- `README_Firebase_Save_Setup.md` says fog and garden monsters are not wired, but
  current `SaveGameRuntimeBinder` does wire them.
- `todo_list.md` is stale and has broken text encoding. Replace or archive it so
  completed systems are not mistaken for unfinished work.
