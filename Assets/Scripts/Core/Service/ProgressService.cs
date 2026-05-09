using UnityEngine;

public static class ProgressService
{
    public static LevelDatabase Database { get; private set; }
    public static int CurrentLevelIndex { get; set; }

    public static int UnlockedLevel
    {
        get => PlayerPrefs.GetInt(GameConstants.PREFS_UNLOCKED_LEVEL, 0);
        set => PlayerPrefs.SetInt(GameConstants.PREFS_UNLOCKED_LEVEL, value);
    }

    public static void Initialize(LevelDatabase database)
    {
        Database = database;
    }

    public static LevelData GetCurrentLevelData()
    {
        if (Database == null || Database.Levels.Length == 0) return null;
        int safeIndex = Mathf.Clamp(CurrentLevelIndex, 0, Database.Levels.Length - 1);
        return Database.Levels[safeIndex];
    }

    public static void UnlockNextLevel()
    {
        int nextLevel = CurrentLevelIndex + 1;
        if (nextLevel > UnlockedLevel)
        {
            UnlockedLevel = nextLevel;
        }
    }

    public static void MoveToNextLevel()
    {
        CurrentLevelIndex++;
        
        if (Database != null && CurrentLevelIndex >= Database.Levels.Length)
        {
            CurrentLevelIndex = 0; 
        }
    }

    public static bool HasNextLevel()
    {
        if (Database == null) return false;
        return CurrentLevelIndex < Database.Levels.Length - 1;
    }
}