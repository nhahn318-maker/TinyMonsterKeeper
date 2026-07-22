# Firebase Account Save Setup

This folder is reserved for the account-based save system.

## Folder roles

- `Data`: serializable save models, for example `GameSaveData`, `InventorySaveData`, `MonsterSaveData`.
- `Services`: interfaces and shared save orchestration, for example `ISaveService`, `SaveManager`.
- `Local`: local autosave implementation used offline and before Firebase is ready.
- `Firebase`: Firebase Auth and Cloud Save implementation.
- `Runtime`: scene-facing MonoBehaviours that bootstrap save/load.

## Firebase Console Setup

1. Create a Firebase project at `https://console.firebase.google.com`.
2. Add an Android app.
3. Set Android package name to the same value as Unity `Project Settings > Player > Android > Package Name`.
4. Download `google-services.json`.
5. Put `google-services.json` in `Assets/`.
6. Add an iOS app later when building iOS.
7. Enable `Authentication`.
8. Enable `Anonymous` sign-in provider first.
9. Later enable `Google` and `Apple` sign-in if account recovery across devices is required.
10. Enable `Cloud Firestore`.
11. Start in test mode only while developing, then replace with locked rules before store submission.

## Unity Machine Setup

1. Download Firebase Unity SDK from the official Firebase site.
2. Import these packages:
   - `FirebaseAuth.unitypackage`
   - `FirebaseFirestore.unitypackage`
3. Let Unity resolve dependencies via External Dependency Manager.
4. Confirm there are no Firebase dependency errors in Console.
5. In `Project Settings > Player > Android > Other Settings > Configuration`, set `Api Compatibility Level` to `.NET Framework` if Firebase reports missing framework/reference errors.
6. Create a boot object in the first scene, for example `SaveSystem`.
7. Add `SaveSystemBootstrap` from `Assets/Scripts/Save/Runtime`.
8. Press Play and check Console for `Firebase signed in. UID:` and `Save system ready (cloud).`

## Firestore Rules For Development

If Unity logs `Missing or insufficient permissions`, Firestore rules are blocking the save document.

Use this rule while developing:

```text
rules_version = '2';
service cloud.firestore {
  match /databases/{database}/documents {
    match /users/{userId}/{document=**} {
      allow read, write: if request.auth != null && request.auth.uid == userId;
    }
  }
}
```

This allows each signed-in anonymous player to read/write only their own document under `users/{uid}`.

## Current Bootstrap Status

Implemented:

- `GameSaveData`: one shared save payload for coin, inventory, monster collection, garden monsters, fog zones, recipes, and failed mixes.
- `LocalSaveService`: offline/local cache using `PlayerPrefs`.
- `FirebaseCloudSaveService`: anonymous-auth cloud save at `users/{uid}/save/main`.
- `SaveManager`: chooses the newest local/cloud save and writes both local cache and cloud save.
- `SaveSystemBootstrap`: scene-facing component for first runtime test.
- `SaveGameRuntimeBinder`: routes coin, inventory, and monster collection counts into the shared save.

Not wired yet:

- Garden monster presence on map and fog unlock state still need to be routed into `SaveManager`.

## Scene Setup For Gameplay Sync

On the same `SaveSystem` object:

1. Keep `SaveSystemBootstrap`.
2. Add `SaveGameRuntimeBinder`.
3. Fill `Item Database` with every collectible/cookable item data asset, for example berry, apple, red mushroom, pumpkin, eggplant, green mushroom.
4. Fill `Monster Database` with every `MonsterData` asset, including Leafy.
5. Keep `Load Save On Start` enabled.
6. Keep `Autosave On Change` enabled.

Current synced systems:

- Coin changes.
- Inventory item amounts.
- Monster book unlock counts/stars/badges.

## Planned Save Document

Firestore path:

```text
users/{uid}/save/main
```

Suggested payload:

```json
{
  "version": 1,
  "coin": 0,
  "inventory": {},
  "monsterCollection": {},
  "gardenMonsters": [],
  "unlockedFogZones": [],
  "discoveredRecipes": [],
  "failedMixes": [],
  "lastSavedAtUnix": 0
}
```

## Implementation Order

1. Build `GameSaveData`.
2. Build `ISaveService`.
3. Build `LocalSaveService`.
4. Route current managers through `SaveManager`.
5. Add Firebase anonymous auth.
6. Add Firebase cloud save.
7. Add conflict resolution between local and cloud.
8. Add account linking.
