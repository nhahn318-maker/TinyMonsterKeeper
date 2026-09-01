using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TinyMonsterKeeper.EditorAutomation
{
    public static class BeeHomeSetupTool
    {
        private const string BeeArtFolder = "Assets/Arts/ResourcesNode/Enviroment/bee";
        private const string BeeHomeSpritePath = BeeArtFolder + "/beehome.png";
        private const string BeeAnimationPath = BeeArtFolder + "/bee_animation.png";
        private const string HoneyButterSpritePath = BeeArtFolder + "/honey_butter.png";
        private const string HoneyButterClickAnimationPath = BeeArtFolder + "/honey_butter_animationclick.png";
        private const string ItemDataPath = "Assets/ScriptableObjects/ItemData/HoneyButter_ItemData.asset";
        private const string DropPrefabPath = "Assets/Prefabs/ResourcesNode/HoneyButterDrop.prefab";
        private const string MapPrefabPath = "Assets/Prefabs/ResourcesNode/ResourcesNode_Map/BeeHome_Map.prefab";
        private const string AnimationFolder = "Assets/Animations/ResourcesNode/Bee";
        private const string ProductionClipPath = AnimationFolder + "/BeeHome_Producing.anim";
        private const string AnimatorPath = "Assets/Animators/ResourcesNode/BeeHomeAnimator.controller";
        private const string HoneyButterPickupClipPath = AnimationFolder + "/HoneyButter_Pickup.anim";
        private const string HoneyButterPickupAnimatorPath = "Assets/Animators/ResourcesNode/HoneyButterDropAnimator.controller";
        private const string BubbleSpritePath = "Assets/Arts/UI/Bubble_32x28.png";

        [MenuItem("TinyMonsterKeeper/Automation/Setup Selected BeeHome")]
        public static void SetupSelectedBeeHome()
        {
            GameObject selected = Selection.activeGameObject;
            if (selected == null || !selected.scene.IsValid())
            {
                selected = Resources.FindObjectsOfTypeAll<GameObject>()
                    .FirstOrDefault(candidate => candidate.scene.IsValid()
                        && candidate.name.Replace("_", string.Empty).Replace(" ", string.Empty)
                            .ToLowerInvariant().Contains("beehome"));
            }

            if (selected == null)
            {
                Debug.LogError("BeeHome was not found in the active scene. Add the beehome sprite to the scene, then run the setup again.");
                return;
            }

            Sprite beeHomeSprite = AssetDatabase.LoadAssetAtPath<Sprite>(BeeHomeSpritePath);
            Sprite honeyButterSprite = AssetDatabase.LoadAssetAtPath<Sprite>(HoneyButterSpritePath);
            Sprite bubbleSprite = AssetDatabase.LoadAssetAtPath<Sprite>(BubbleSpritePath);
            if (beeHomeSprite == null || honeyButterSprite == null || bubbleSprite == null)
            {
                Debug.LogError("BeeHome setup failed because beehome, honey_butter, or bubble sprite is missing.");
                return;
            }

            CenterBeeAnimationPivots();
            Sprite[] productionFrames = AssetDatabase.LoadAllAssetsAtPath(BeeAnimationPath)
                .OfType<Sprite>()
                .OrderBy(sprite => sprite.name)
                .ToArray();
            if (productionFrames.Length == 0)
            {
                Debug.LogError("BeeHome setup failed: bee_animation.png has no sliced sprites.");
                return;
            }

            EnsureFolder("Assets/Animations/ResourcesNode", "Bee");
            ItemData itemData = CreateOrUpdateItemData(honeyButterSprite);
            AnimationClip productionClip = CreateOrUpdateProductionClip(productionFrames);
            AnimatorController animatorController = CreateOrUpdateAnimator(productionClip);
            GameObject dropPrefab = CreateOrUpdateDropPrefab(itemData, honeyButterSprite);
            SetupHoneyButterPickupAnimationInternal();

            ConfigureBeeHome(selected, beeHomeSprite, itemData, dropPrefab, bubbleSprite, animatorController);
            AddItemToSaveDatabase(itemData);

            if (!PrefabUtility.IsPartOfPrefabInstance(selected))
            {
                PrefabUtility.SaveAsPrefabAssetAndConnect(selected, MapPrefabPath, InteractionMode.UserAction);
            }
            else
            {
                PrefabUtility.ApplyPrefabInstance(selected, InteractionMode.UserAction);
            }

            EditorUtility.SetDirty(selected);
            EditorSceneManager.MarkSceneDirty(selected.scene);
            AssetDatabase.SaveAssets();
            Selection.activeGameObject = selected;
            Debug.Log("BeeHome setup finished. Honey Butter production defaults to 60 seconds. Verify it in Play Mode, then save the scene.");
        }

        private static void CenterBeeAnimationPivots()
        {
            CenterAnimationPivots(BeeAnimationPath);
        }

        private static void CenterAnimationPivots(string texturePath)
        {
            TextureImporter importer = AssetImporter.GetAtPath(texturePath) as TextureImporter;
            if (importer == null || importer.spriteImportMode != SpriteImportMode.Multiple)
                return;

#pragma warning disable 618
            SpriteMetaData[] sprites = importer.spritesheet;
            bool changed = false;
            for (int i = 0; i < sprites.Length; i++)
            {
                if (sprites[i].alignment == (int)SpriteAlignment.Center && sprites[i].pivot == new Vector2(0.5f, 0.5f))
                    continue;

                sprites[i].alignment = (int)SpriteAlignment.Center;
                sprites[i].pivot = new Vector2(0.5f, 0.5f);
                changed = true;
            }

            if (changed)
            {
                importer.spritesheet = sprites;
                importer.SaveAndReimport();
            }
#pragma warning restore 618
        }

        private static ItemData CreateOrUpdateItemData(Sprite icon)
        {
            ItemData itemData = AssetDatabase.LoadAssetAtPath<ItemData>(ItemDataPath);
            if (itemData == null)
            {
                itemData = ScriptableObject.CreateInstance<ItemData>();
                AssetDatabase.CreateAsset(itemData, ItemDataPath);
            }

            itemData.name = "HoneyButter_ItemData";
            itemData.itemId = "honey_butter";
            itemData.itemName = "Honey Butter";
            itemData.icon = icon;
            itemData.cookingIconSize = new Vector2(100f, 100f);
            itemData.friendshipValue = 1;
            EditorUtility.SetDirty(itemData);
            return itemData;
        }

        private static AnimationClip CreateOrUpdateProductionClip(Sprite[] frames)
        {
            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(ProductionClipPath);
            if (clip == null)
            {
                clip = new AnimationClip();
                AssetDatabase.CreateAsset(clip, ProductionClipPath);
            }

            clip.name = "BeeHome_Producing";
            clip.frameRate = 8f;
            ObjectReferenceKeyframe[] keys = new ObjectReferenceKeyframe[frames.Length];
            for (int i = 0; i < frames.Length; i++)
            {
                keys[i] = new ObjectReferenceKeyframe
                {
                    time = i / clip.frameRate,
                    value = frames[i]
                };
            }

            EditorCurveBinding binding = new EditorCurveBinding
            {
                path = string.Empty,
                type = typeof(SpriteRenderer),
                propertyName = "m_Sprite"
            };
            AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);

            SerializedObject serializedClip = new SerializedObject(clip);
            SerializedProperty loopTime = serializedClip.FindProperty("m_AnimationClipSettings.m_LoopTime");
            if (loopTime != null)
                loopTime.boolValue = true;
            serializedClip.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(clip);
            return clip;
        }

        private static AnimatorController CreateOrUpdateAnimator(AnimationClip clip)
        {
            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(AnimatorPath);
            if (controller == null)
                controller = AnimatorController.CreateAnimatorControllerAtPath(AnimatorPath);

            AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
            AnimatorState state = stateMachine.states
                .Select(child => child.state)
                .FirstOrDefault(candidate => candidate.name == "BeeHome_Producing");
            if (state == null)
                state = stateMachine.AddState("BeeHome_Producing");

            state.motion = clip;
            stateMachine.defaultState = state;
            EditorUtility.SetDirty(controller);
            return controller;
        }

        private static GameObject CreateOrUpdateDropPrefab(ItemData itemData, Sprite icon)
        {
            GameObject root = AssetDatabase.LoadAssetAtPath<GameObject>(DropPrefabPath) != null
                ? PrefabUtility.LoadPrefabContents(DropPrefabPath)
                : new GameObject("HoneyButterDrop");

            root.name = "HoneyButterDrop";
            root.layer = 3;
            root.transform.localScale = Vector3.one;

            SpriteRenderer renderer = root.GetComponent<SpriteRenderer>();
            if (renderer == null)
                renderer = root.AddComponent<SpriteRenderer>();
            renderer.sprite = icon;
            renderer.sortingOrder = 5;

            GameObject crystalDrop = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/ResourcesNode/CrystalDrop.prefab");
            SpriteRenderer crystalRenderer = crystalDrop != null ? crystalDrop.GetComponent<SpriteRenderer>() : null;
            if (crystalRenderer != null)
                renderer.sharedMaterial = crystalRenderer.sharedMaterial;

            BoxCollider2D collider = root.GetComponent<BoxCollider2D>();
            if (collider == null)
                collider = root.AddComponent<BoxCollider2D>();
            collider.size = icon.bounds.size * 0.85f;

            Animator pickupAnimator = root.GetComponent<Animator>();

            BerryDropController pickup = root.GetComponent<BerryDropController>();
            if (pickup == null)
                pickup = root.AddComponent<BerryDropController>();
            SerializedObject serializedPickup = new SerializedObject(pickup);
            serializedPickup.FindProperty("itemData").objectReferenceValue = itemData;
            serializedPickup.FindProperty("amount").intValue = 1;
            serializedPickup.FindProperty("jumpHeight").floatValue = 0.25f;
            serializedPickup.FindProperty("duration").floatValue = 0.3f;
            serializedPickup.FindProperty("pickupCollider").objectReferenceValue = collider;
            serializedPickup.FindProperty("spriteRenderer").objectReferenceValue = renderer;
            serializedPickup.FindProperty("mainCamera").objectReferenceValue = null;
            serializedPickup.FindProperty("animator").objectReferenceValue = pickupAnimator;
            serializedPickup.FindProperty("pickupAnim").stringValue = pickupAnimator != null
                ? "HoneyButter_Pickup"
                : string.Empty;
            serializedPickup.FindProperty("pickupClickSprite").objectReferenceValue = null;
            serializedPickup.FindProperty("pickupAnimDuration").floatValue = 0.1f;
            serializedPickup.FindProperty("addToInventoryBeforeAnimation").boolValue = true;
            serializedPickup.FindProperty("ignoreUIWhileTesting").boolValue = true;
            serializedPickup.ApplyModifiedPropertiesWithoutUndo();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, DropPrefabPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        [MenuItem("TinyMonsterKeeper/Automation/Setup Honey Butter Pickup Animation")]
        public static void SetupHoneyButterPickupAnimation()
        {
            if (!SetupHoneyButterPickupAnimationInternal())
                return;

            AssetDatabase.SaveAssets();
            Debug.Log("Honey Butter pickup animation setup finished.");
        }

        private static bool SetupHoneyButterPickupAnimationInternal()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(DropPrefabPath) == null)
            {
                Debug.LogError("HoneyButterDrop prefab is missing. Run Setup Selected BeeHome first.");
                return false;
            }

            CenterAnimationPivots(HoneyButterClickAnimationPath);
            Sprite[] frames = AssetDatabase.LoadAllAssetsAtPath(HoneyButterClickAnimationPath)
                .OfType<Sprite>()
                .OrderBy(sprite => sprite.name)
                .ToArray();
            if (frames.Length == 0)
            {
                Debug.LogError("Honey Butter pickup setup failed: the click animation has no sliced frames.");
                return false;
            }

            AnimationClip clip = CreateOrUpdateSpriteClip(
                HoneyButterPickupClipPath,
                "HoneyButter_Pickup",
                frames,
                8f,
                false);
            AnimatorController controller = CreateOrUpdateSingleStateAnimator(
                HoneyButterPickupAnimatorPath,
                "HoneyButter_Pickup",
                clip);

            GameObject root = PrefabUtility.LoadPrefabContents(DropPrefabPath);
            Animator animator = root.GetComponent<Animator>();
            if (animator == null)
                animator = root.AddComponent<Animator>();
            animator.runtimeAnimatorController = controller;
            animator.enabled = false;

            BerryDropController pickup = root.GetComponent<BerryDropController>();
            if (pickup == null)
            {
                PrefabUtility.UnloadPrefabContents(root);
                Debug.LogError("HoneyButterDrop is missing BerryDropController.");
                return false;
            }

            SerializedObject serializedPickup = new SerializedObject(pickup);
            serializedPickup.FindProperty("animator").objectReferenceValue = animator;
            serializedPickup.FindProperty("pickupAnim").stringValue = "HoneyButter_Pickup";
            serializedPickup.FindProperty("pickupClickSprite").objectReferenceValue = null;
            serializedPickup.FindProperty("pickupAnimDuration").floatValue = frames.Length / 8f;
            serializedPickup.FindProperty("addToInventoryBeforeAnimation").boolValue = true;
            serializedPickup.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(root, DropPrefabPath);
            PrefabUtility.UnloadPrefabContents(root);
            return true;
        }

        private static AnimationClip CreateOrUpdateSpriteClip(
            string clipPath,
            string clipName,
            Sprite[] frames,
            float frameRate,
            bool loop)
        {
            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
            if (clip == null)
            {
                clip = new AnimationClip();
                AssetDatabase.CreateAsset(clip, clipPath);
            }

            clip.name = clipName;
            clip.frameRate = frameRate;
            ObjectReferenceKeyframe[] keys = new ObjectReferenceKeyframe[frames.Length];
            for (int i = 0; i < frames.Length; i++)
            {
                keys[i] = new ObjectReferenceKeyframe
                {
                    time = i / frameRate,
                    value = frames[i]
                };
            }

            EditorCurveBinding binding = new EditorCurveBinding
            {
                path = string.Empty,
                type = typeof(SpriteRenderer),
                propertyName = "m_Sprite"
            };
            AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);

            SerializedObject serializedClip = new SerializedObject(clip);
            SerializedProperty loopTime = serializedClip.FindProperty("m_AnimationClipSettings.m_LoopTime");
            if (loopTime != null)
                loopTime.boolValue = loop;
            serializedClip.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(clip);
            return clip;
        }

        private static AnimatorController CreateOrUpdateSingleStateAnimator(
            string controllerPath,
            string stateName,
            AnimationClip clip)
        {
            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
            if (controller == null)
                controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);

            AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
            AnimatorState state = stateMachine.states
                .Select(child => child.state)
                .FirstOrDefault(candidate => candidate.name == stateName);
            if (state == null)
                state = stateMachine.AddState(stateName);

            state.motion = clip;
            stateMachine.defaultState = state;
            EditorUtility.SetDirty(controller);
            return controller;
        }

        private static void ConfigureBeeHome(
            GameObject target,
            Sprite idleSprite,
            ItemData itemData,
            GameObject dropPrefab,
            Sprite bubbleSprite,
            RuntimeAnimatorController animatorController)
        {
            Undo.RecordObject(target, "Setup BeeHome");
            target.name = "BeeHome_Map";

            SpriteRenderer renderer = target.GetComponent<SpriteRenderer>();
            if (renderer == null)
                renderer = Undo.AddComponent<SpriteRenderer>(target);
            renderer.sprite = idleSprite;

            BoxCollider2D collider = target.GetComponent<BoxCollider2D>();
            if (collider == null)
            {
                collider = Undo.AddComponent<BoxCollider2D>(target);
                collider.size = idleSprite.bounds.size * 0.75f;
            }

            Animator animator = target.GetComponent<Animator>();
            if (animator == null)
                animator = Undo.AddComponent<Animator>(target);
            animator.runtimeAnimatorController = animatorController;

            StaticTimedHarvestNodeController node = target.GetComponent<StaticTimedHarvestNodeController>();
            if (node == null)
                node = Undo.AddComponent<StaticTimedHarvestNodeController>(target);

            TMP_Text existingCountdown = target.GetComponentsInChildren<TMP_Text>(true)
                .FirstOrDefault(text => text.name == "GrowthTimerText");
            Transform existingBubble = target.GetComponentsInChildren<Transform>(true)
                .FirstOrDefault(child => child != target.transform && child.name == "ReadyBubble");

            SerializedObject serializedNode = new SerializedObject(node);
            serializedNode.FindProperty("respawnDuration").floatValue = 60f;
            serializedNode.FindProperty("countdownFormat").stringValue = "{0}s";
            serializedNode.FindProperty("dropPrefab").objectReferenceValue = dropPrefab;
            serializedNode.FindProperty("dropPoint").objectReferenceValue = null;
            serializedNode.FindProperty("fallbackDropOffset").vector2Value = new Vector2(0f, -0.3f);
            serializedNode.FindProperty("itemData").objectReferenceValue = itemData;
            serializedNode.FindProperty("amount").intValue = 1;
            serializedNode.FindProperty("harvestCollider").objectReferenceValue = collider;
            serializedNode.FindProperty("mainCamera").objectReferenceValue = null;
            serializedNode.FindProperty("spriteRenderer").objectReferenceValue = renderer;
            serializedNode.FindProperty("growthCountdownText").objectReferenceValue = existingCountdown;
            serializedNode.FindProperty("createCountdownTextIfMissing").boolValue = false;
            serializedNode.FindProperty("countdownLocalOffset").vector3Value = new Vector3(0f, 0.7f, 0f);
            serializedNode.FindProperty("readyBubbleObject").objectReferenceValue = existingBubble != null
                ? existingBubble.gameObject
                : null;
            serializedNode.FindProperty("readyBubbleSprite").objectReferenceValue = bubbleSprite;
            serializedNode.FindProperty("createReadyBubbleIfMissing").boolValue = false;
            serializedNode.FindProperty("readyBubbleLocalOffset").vector3Value = new Vector3(0f, 0.7f, 0f);
            serializedNode.FindProperty("readyBubbleIconScale").floatValue = 0.7f;
            serializedNode.FindProperty("pickupLayer").intValue = 1 << 3;
            serializedNode.FindProperty("animateWhileGrowing").boolValue = true;
            serializedNode.FindProperty("productionAnimator").objectReferenceValue = animator;
            serializedNode.FindProperty("idleSprite").objectReferenceValue = idleSprite;
            serializedNode.ApplyModifiedPropertiesWithoutUndo();

            YSortByPosition ySort = target.GetComponent<YSortByPosition>();
            if (ySort == null)
                ySort = Undo.AddComponent<YSortByPosition>(target);
            SerializedObject serializedYSort = new SerializedObject(ySort);
            serializedYSort.FindProperty("sortPoint").objectReferenceValue = target.transform;
            serializedYSort.FindProperty("sortYOffset").floatValue = -0.25f;
            serializedYSort.FindProperty("worldBaseOrder").intValue = 500;
            serializedYSort.FindProperty("baseOrder").intValue = 0;
            serializedYSort.FindProperty("unitsToOrder").floatValue = 100f;
            serializedYSort.FindProperty("preferSortingGroup").boolValue = false;
            serializedYSort.FindProperty("sortingGroup").objectReferenceValue = null;
            SerializedProperty renderers = serializedYSort.FindProperty("spriteRenderers");
            renderers.arraySize = 1;
            renderers.GetArrayElementAtIndex(0).objectReferenceValue = renderer;
            serializedYSort.ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(renderer);
            EditorUtility.SetDirty(collider);
            EditorUtility.SetDirty(animator);
            EditorUtility.SetDirty(node);
            EditorUtility.SetDirty(ySort);
        }

        private static void AddItemToSaveDatabase(ItemData itemData)
        {
            SaveGameRuntimeBinder binder = Object.FindObjectOfType<SaveGameRuntimeBinder>();
            if (binder == null)
            {
                Debug.LogWarning("SaveGameRuntimeBinder was not found. Add HoneyButter_ItemData to its Item Database manually.");
                return;
            }

            SerializedObject serializedBinder = new SerializedObject(binder);
            SerializedProperty database = serializedBinder.FindProperty("itemDatabase");
            for (int i = 0; i < database.arraySize; i++)
            {
                if (database.GetArrayElementAtIndex(i).objectReferenceValue == itemData)
                    return;
            }

            database.InsertArrayElementAtIndex(database.arraySize);
            database.GetArrayElementAtIndex(database.arraySize - 1).objectReferenceValue = itemData;
            serializedBinder.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(binder);
        }

        private static void EnsureFolder(string parentPath, string folderName)
        {
            string path = parentPath + "/" + folderName;
            if (!AssetDatabase.IsValidFolder(path))
                AssetDatabase.CreateFolder(parentPath, folderName);
        }
    }
}
