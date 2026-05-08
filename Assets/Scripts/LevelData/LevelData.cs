using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct TileCoordinate
{
    public Vector2 Position;
    public int Layer;
}

[CreateAssetMenu(fileName = "NewLevel", menuName = "TileTrip/LevelData")]
public class LevelData : ScriptableObject
{
    public int RackCapacity = 7;
    //public float TimeLimit = 60f; 
    public List<TileCoordinate> LayoutCoordinates = new List<TileCoordinate>();
    public List<string> AllowedIconIds = new List<string>();
}