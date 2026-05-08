using System.Collections.Generic;
using UnityEngine;

public class IconData : ScriptableObject
{
    public List<IconInfo> Icons = new List<IconInfo>();

    public Sprite GetSpriteById(string id)
    {
        var icon = Icons.Find(i => i.Id == id);
        return icon.Sprite;
    }
}

public struct IconInfo
{
    public string Id;
    public Sprite Sprite;
}