using System;
using UnityEngine;

public static class TimedSaveUtility
{
    public static long NowUnix => DateTimeOffset.UtcNow.ToUnixTimeSeconds();

    public static long SecondsFromNow(float seconds)
    {
        return NowUnix + Mathf.Max(1, Mathf.CeilToInt(seconds));
    }

    public static string GetStableSceneKey(Component component, string kind)
    {
        if (component == null)
            return string.Empty;

        Transform current = component.transform;
        string path = current.name + "[" + current.GetSiblingIndex() + "]";
        while (current.parent != null)
        {
            current = current.parent;
            path = current.name + "[" + current.GetSiblingIndex() + "]/" + path;
        }

        return component.gameObject.scene.name + ":" + kind + ":" + path;
    }
}
