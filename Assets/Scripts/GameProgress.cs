using UnityEngine;

public static class GameProgress
{
    public static int CurrentLevelIndex = 0;
    public static LevelDatabase Database;

    public static int UnlockedLevel
    {
        get => PlayerPrefs.GetInt("UnlockedLevel", 0);
        set => PlayerPrefs.SetInt("UnlockedLevel", value);
    }
}