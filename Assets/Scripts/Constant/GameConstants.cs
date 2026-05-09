using UnityEngine;

public static class GameConstants
{
    public const string SCENE_LOADING = "LoadingScene";
    public const string SCENE_HOME = "HomeScene";
    public const string SCENE_GAMEPLAY = "GameplayScene";
    
    public const string DB_RESOURCE_PATH = "LevelDatabase";
    public const string POOL_KEY_TILE = "Tile";
    public const string POOL_KEY_MATCH_EFFECT = "MatchEffect";

    public const string PREFS_UNLOCKED_LEVEL = "UnlockedLevel";

    public const float TILE_SIZE = 1f;
    public const int GENERATOR_MAX_ATTEMPTS = 50;

    public const int SORTING_BASE_OFFSET = 1000;
    public const int SORTING_ICON_OFFSET = 1;
    public const int SORTING_DETACHED_BASE = 1000;
    public const int SORTING_DETACHED_ICON = 1001;

    public static readonly Vector3 TILE_SPAWN_SCALE = new(0.68f, 0.68f, 0.68f);
    public const float ANIMATION_DURATION_FAST = 0.1f;
    public const float ANIMATION_DURATION_NORMAL = 0.15f;
    public const float ANIMATION_DURATION_SLOW = 0.3f;
    public const float ANIMATION_DURATION_COLOR = 0.5f;

    public static readonly Color TILE_LOCKED_COLOR = new(0.4f, 0.4f, 0.4f, 1f);

    public const int COMBO_MAX_SIZE = 5;
}