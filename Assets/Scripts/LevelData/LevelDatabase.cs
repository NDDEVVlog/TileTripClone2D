using UnityEngine;

[CreateAssetMenu(fileName = "LevelDatabase", menuName = "TileTrip/LevelDatabase")]
public class LevelDatabase : ScriptableObject
{
    public LevelData[] Levels;
}