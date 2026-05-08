using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

public struct GeneratedTile
{
    public Vector2 Position;
    public int Layer;
    public string Id;
}

public class VirtualTile
{
    // Đã thay đổi từ 'init' sang 'set' để sửa lỗi CS0518
    public Vector2 Position { get; set; }
    public int Layer { get; set; }
    public float Size { get; set; }
    public string AssignedId { get; set; }

    public HashSet<VirtualTile> BlockedBy { get; } = new();
    public HashSet<VirtualTile> Blocking { get; } = new();
}

public class SolvableGenerator
{
    public IReadOnlyList<GeneratedTile> Generate(LevelData levelData)
    {
        if (levelData.LayoutCoordinates.Count % 3 != 0)
            throw new ArgumentException("Total tiles must be a multiple of 3");

        for (int attempt = 0; attempt < GameConstants.GENERATOR_MAX_ATTEMPTS; attempt++)
        {
            var virtualTiles = levelData.LayoutCoordinates
                .Select(c => new VirtualTile { Position = c.Position, Layer = c.Layer, Size = GameConstants.TILE_SIZE })
                .ToList();

            BuildVirtualGraph(virtualTiles);

            if (TryAssignIds(virtualTiles, levelData.AllowedIconIds))
            {
                return virtualTiles.Select(v => new GeneratedTile
                {
                    Position = v.Position,
                    Layer = v.Layer,
                    Id = v.AssignedId
                }).ToList();
            }
        }

        throw new Exception("Failed to generate a solvable board.");
    }

    private void BuildVirtualGraph(List<VirtualTile> tiles)
    {
        int count = tiles.Count;
        for (int i = 0; i < count; i++)
        {
            for (int j = i + 1; j < count; j++)
            {
                var a = tiles[i];
                var b = tiles[j];

                if (a.Layer == b.Layer || !IsOverlapping(a, b)) continue;

                if (a.Layer < b.Layer)
                {
                    a.BlockedBy.Add(b);
                    b.Blocking.Add(a);
                }
                else
                {
                    b.BlockedBy.Add(a);
                    a.Blocking.Add(b);
                }
            }
        }
    }

    private bool TryAssignIds(List<VirtualTile> tiles, IReadOnlyList<string> allowedIds)
    {
        var unassigned = new List<VirtualTile>(tiles);

        while (unassigned.Count > 0)
        {
            var available = unassigned.Where(t => t.BlockedBy.Count == 0).ToList();
            if (available.Count < 3) return false; 

            var selectedGroup = available.OrderBy(_ => Random.value).Take(3).ToList();
            string selectedId = allowedIds[Random.Range(0, allowedIds.Count)];

            foreach (var tile in selectedGroup)
            {
                tile.AssignedId = selectedId;
                unassigned.Remove(tile);

                foreach (var blocked in tile.Blocking)
                {
                    blocked.BlockedBy.Remove(tile);
                }
            }
        }
        return true;
    }

    private bool IsOverlapping(VirtualTile a, VirtualTile b)
    {
        return a.Position.x < b.Position.x + b.Size &&
               a.Position.x + a.Size > b.Position.x &&
               a.Position.y < b.Position.y + b.Size &&
               a.Position.y + a.Size > b.Position.y;
    }
}