using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TinyMonsterKeeper.EditorAutomation
{
    public static class CookingRecipeSetupTool
    {
        private const string RecipeFolder = "Assets/ScriptableObjects/CookingRecipes";

        [MenuItem("TinyMonsterKeeper/Automation/Setup All Cooking Recipes")]
        public static void SetupAllCookingRecipes()
        {
            CookingRecipeData[] recipes = LoadRecipes();
            if (!ValidateRecipes(recipes))
                return;

            CookingPotController pot = FindActiveScenePot();
            if (pot == null)
            {
                Debug.LogError("CookingPotController was not found in the active scene. Open GameplayScene and run this tool again.");
                return;
            }

            SerializedObject serializedPot = new SerializedObject(pot);
            SerializedProperty recipeProperty = serializedPot.FindProperty("recipes");
            recipeProperty.arraySize = recipes.Length;

            for (int i = 0; i < recipes.Length; i++)
                recipeProperty.GetArrayElementAtIndex(i).objectReferenceValue = recipes[i];

            serializedPot.ApplyModifiedProperties();
            EditorUtility.SetDirty(pot);
            EditorSceneManager.MarkSceneDirty(pot.gameObject.scene);
            Selection.activeGameObject = pot.gameObject;

            Debug.Log($"Assigned {recipes.Length} recipes covering 27 monster results to {pot.name}. Save GameplayScene to keep the references.");
        }

        [MenuItem("TinyMonsterKeeper/Tools/Validate Cooking Recipes")]
        public static void ValidateCookingRecipes()
        {
            CookingRecipeData[] recipes = LoadRecipes();
            if (ValidateRecipes(recipes))
                Debug.Log($"Cooking recipe validation passed: {recipes.Length} unique recipes and 27 unique monster results.");
        }

        private static CookingRecipeData[] LoadRecipes()
        {
            return AssetDatabase.FindAssets("t:CookingRecipeData", new[] { RecipeFolder })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<CookingRecipeData>)
                .Where(recipe => recipe != null)
                .OrderBy(recipe => GetRecipeOrder(recipe.recipeId))
                .ThenBy(recipe => recipe.recipeId, StringComparer.Ordinal)
                .ToArray();
        }

        private static bool ValidateRecipes(IReadOnlyList<CookingRecipeData> recipes)
        {
            bool valid = true;
            HashSet<string> recipeIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            HashSet<string> ingredientSignatures = new HashSet<string>(StringComparer.Ordinal);
            HashSet<GameObject> monsterPrefabs = new HashSet<GameObject>();

            for (int i = 0; i < recipes.Count; i++)
            {
                CookingRecipeData recipe = recipes[i];
                if (string.IsNullOrWhiteSpace(recipe.recipeId) || !recipeIds.Add(recipe.recipeId.Trim()))
                {
                    Debug.LogError($"Recipe has a missing or duplicate ID: {recipe.name}", recipe);
                    valid = false;
                }

                if (recipe.RequiredSlotCount != 3)
                {
                    Debug.LogError($"Recipe {recipe.name} requires {recipe.RequiredSlotCount} slots instead of 3.", recipe);
                    valid = false;
                }

                string signature = BuildIngredientSignature(recipe);
                if (!ingredientSignatures.Add(signature))
                {
                    Debug.LogError($"Recipe {recipe.name} duplicates ingredient combination {signature}.", recipe);
                    valid = false;
                }

                if (recipe.monsterResultOptions == null || recipe.monsterResultOptions.Length == 0)
                {
                    Debug.LogError($"Recipe {recipe.name} has no monster results.", recipe);
                    valid = false;
                    continue;
                }

                for (int resultIndex = 0; resultIndex < recipe.monsterResultOptions.Length; resultIndex++)
                {
                    GameObject prefab = recipe.monsterResultOptions[resultIndex].monsterPrefab;
                    if (prefab == null)
                    {
                        Debug.LogError($"Recipe {recipe.name} has an empty monster result.", recipe);
                        valid = false;
                    }
                    else if (!monsterPrefabs.Add(prefab))
                    {
                        Debug.LogError($"Monster prefab {prefab.name} appears in more than one recipe.", recipe);
                        valid = false;
                    }
                }
            }

            if (monsterPrefabs.Count != 27)
            {
                Debug.LogError($"Recipes cover {monsterPrefabs.Count} unique monster prefabs instead of 27.");
                valid = false;
            }

            return valid;
        }

        private static string BuildIngredientSignature(CookingRecipeData recipe)
        {
            Dictionary<string, int> counts = new Dictionary<string, int>(StringComparer.Ordinal);
            if (recipe.ingredients != null)
            {
                for (int i = 0; i < recipe.ingredients.Length; i++)
                {
                    CookingRecipeData.IngredientRequirement requirement = recipe.ingredients[i];
                    if (requirement.itemData == null || requirement.count <= 0)
                        continue;

                    string itemId = requirement.itemData.itemId.Trim().ToLowerInvariant();
                    counts[itemId] = counts.TryGetValue(itemId, out int count)
                        ? count + requirement.count
                        : requirement.count;
                }
            }

            return string.Join("+", counts.OrderBy(pair => pair.Key).Select(pair => $"{pair.Key}:{pair.Value}"));
        }

        private static int GetRecipeOrder(string recipeId)
        {
            return int.TryParse(recipeId, out int numericId) ? numericId : int.MaxValue;
        }

        private static CookingPotController FindActiveScenePot()
        {
            return Resources.FindObjectsOfTypeAll<CookingPotController>()
                .FirstOrDefault(candidate => candidate != null
                    && candidate.gameObject.scene.IsValid()
                    && candidate.gameObject.scene == UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        }
    }
}
