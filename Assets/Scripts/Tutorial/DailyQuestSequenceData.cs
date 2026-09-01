using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "DailyQuestSequence", menuName = "Tiny Monster Keeper/Tutorial/Daily Quest Sequence")]
public sealed class DailyQuestSequenceData : ScriptableObject
{
    [Serializable]
    public sealed class Goal
    {
        public TutorialAction action;
        [TextArea(1, 2)] public string displayText;
        [Min(1)] public int target = 1;
    }

    [Serializable]
    public sealed class Quest
    {
        public string title;
        [Min(0)] public int coinReward = 5;
        public List<Goal> goals = new List<Goal>();
    }

    [SerializeField] private List<Quest> quests = new List<Quest>();
    public int Count => quests.Count;
    public Quest Get(int index) => index >= 0 && index < quests.Count ? quests[index] : null;
}
