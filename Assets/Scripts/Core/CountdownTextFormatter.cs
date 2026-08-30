using UnityEngine;

public static class CountdownTextFormatter
{
    public static string Format(int totalSeconds)
    {
        int seconds = Mathf.Max(0, totalSeconds);
        if (seconds < 60)
            return $"{seconds}s";

        int minutes = seconds / 60;
        int remainingSeconds = seconds % 60;
        return $"{minutes}:{remainingSeconds:00}";
    }
}
