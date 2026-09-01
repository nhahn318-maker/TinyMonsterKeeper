using System;

public enum TutorialAction
{
    HarvestResource,
    CollectDrop,
    OpenCooking,
    StartCooking,
    CollectCookedResult,
    SummonMonster,
    InteractMonster,
    CollectCoin,
    UnlockZone01
}

public static class TutorialSignal
{
    public static event Action<TutorialAction> Raised;
    public static void Raise(TutorialAction action) => Raised?.Invoke(action);
}
