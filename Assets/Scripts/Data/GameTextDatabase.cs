using System;
using System.Collections.Generic;
using UnityEngine;

#pragma warning disable 0649

public enum GameTextKey
{
    FogUnlockConfirm,
    NotEnoughCoin,
    MonsterJoined,
    MonsterAlreadyInGarden,
    MonsterStarIncreased
}

[CreateAssetMenu(fileName = "GameTextDatabase", menuName = "Tiny Monster Keeper/Game Text Database")]
public class GameTextDatabase : ScriptableObject
{
    [Serializable]
    private class Entry
    {
        public GameTextKey key;
        [TextArea(2, 4)] public string text;
    }

    [SerializeField] private List<Entry> entries = new List<Entry>();

    public string Get(GameTextKey key, string fallback, params object[] args)
    {
        string template = fallback;

        for (int i = 0; i < entries.Count; i++)
        {
            if (entries[i] != null && entries[i].key == key && !string.IsNullOrWhiteSpace(entries[i].text))
            {
                template = entries[i].text;
                break;
            }
        }

        if (args == null || args.Length == 0)
            return template;

        try
        {
            return string.Format(template, args);
        }
        catch (FormatException)
        {
            Debug.LogWarning($"Game text format is invalid for key: {key}");
            return template;
        }
    }
}

#pragma warning restore 0649
