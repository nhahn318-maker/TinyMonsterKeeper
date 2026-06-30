using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewCookingRecipeData", menuName = "TinyMonsterKeeper/Cooking Recipe Data")]
public class CookingRecipeData : ScriptableObject
{
    [System.Serializable]
    public struct IngredientRequirement
    {
        public ItemData itemData;
        public int count;
    }

    [Header("Recipe")]
    public string recipeId;
    public string resultName;
    public float cookDuration = 10f;
    public IngredientRequirement[] ingredients;

    [Header("Attraction")]
    public GameObject[] attractedMonsterPrefabs;
    public bool allowDuplicateMonsters;

    public int RequiredSlotCount
    {
        get
        {
            int total = 0;

            if (ingredients == null)
                return total;

            for (int i = 0; i < ingredients.Length; i++)
                total += Mathf.Max(0, ingredients[i].count);

            return total;
        }
    }

    public bool Matches(IReadOnlyList<ItemData> selectedIngredients)
    {
        if (selectedIngredients == null)
            return false;

        Dictionary<string, int> requiredAmounts = BuildRequiredAmounts();
        int requiredSlotCount = GetTotalCount(requiredAmounts);

        if (selectedIngredients.Count != requiredSlotCount)
            return false;

        Dictionary<string, int> selectedAmounts = BuildSelectedAmounts(selectedIngredients);

        if (selectedAmounts.Count != requiredAmounts.Count)
            return false;

        foreach (var requirement in requiredAmounts)
        {
            if (!selectedAmounts.TryGetValue(requirement.Key, out int selectedCount))
                return false;

            if (selectedCount != requirement.Value)
                return false;
        }

        return true;
    }

    public GameObject GetRandomAttractedMonsterPrefab()
    {
        if (attractedMonsterPrefabs == null || attractedMonsterPrefabs.Length == 0)
            return null;

        return attractedMonsterPrefabs[Random.Range(0, attractedMonsterPrefabs.Length)];
    }

    public string GetRequirementSummary()
    {
        Dictionary<string, int> requiredAmounts = BuildRequiredAmounts();
        List<string> parts = new List<string>();

        foreach (var pair in requiredAmounts)
            parts.Add($"{pair.Key} x{pair.Value}");

        return parts.Count > 0 ? string.Join(", ", parts) : "no ingredients";
    }

    private static Dictionary<string, int> BuildSelectedAmounts(IReadOnlyList<ItemData> selectedIngredients)
    {
        Dictionary<string, int> amounts = new Dictionary<string, int>();

        for (int i = 0; i < selectedIngredients.Count; i++)
        {
            ItemData item = selectedIngredients[i];
            if (item == null)
                continue;

            string itemId = GetNormalizedItemId(item);

            if (string.IsNullOrEmpty(itemId))
                continue;

            if (!amounts.ContainsKey(itemId))
                amounts[itemId] = 0;

            amounts[itemId]++;
        }

        return amounts;
    }

    private Dictionary<string, int> BuildRequiredAmounts()
    {
        Dictionary<string, int> amounts = new Dictionary<string, int>();

        if (ingredients == null)
            return amounts;

        for (int i = 0; i < ingredients.Length; i++)
        {
            IngredientRequirement requirement = ingredients[i];
            if (requirement.itemData == null || requirement.count <= 0)
                continue;

            string itemId = GetNormalizedItemId(requirement.itemData);

            if (string.IsNullOrEmpty(itemId))
                continue;

            if (!amounts.ContainsKey(itemId))
                amounts[itemId] = 0;

            amounts[itemId] += requirement.count;
        }

        return amounts;
    }

    private static int GetTotalCount(Dictionary<string, int> amounts)
    {
        int total = 0;

        foreach (var pair in amounts)
            total += pair.Value;

        return total;
    }

    private static string GetNormalizedItemId(ItemData item)
    {
        return item != null && item.itemId != null
            ? item.itemId.Trim().ToLowerInvariant()
            : string.Empty;
    }
}
