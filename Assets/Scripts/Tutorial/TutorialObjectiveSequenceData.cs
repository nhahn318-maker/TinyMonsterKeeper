using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "TutorialObjectiveSequence", menuName = "Tiny Monster Keeper/Tutorial/Objective Sequence")]
public sealed class TutorialObjectiveSequenceData : ScriptableObject
{
    [Serializable]
    public sealed class Objective
    {
        public TutorialAction action;

        [TextArea(2, 4)]
        public string displayText;

        [Min(0)]
        public int coinReward = 1;
    }

    [SerializeField] private List<Objective> objectives = new List<Objective>();

    public int Count => objectives.Count;

    public Objective Get(int index)
    {
        return index >= 0 && index < objectives.Count ? objectives[index] : null;
    }
}
