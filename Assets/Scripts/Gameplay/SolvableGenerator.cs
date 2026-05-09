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
        if (levelData.LayoutCoordinates.Count % GameConstants.MATCHING_COUNT != 0)
            throw new ArgumentException($"Total tiles must be a multiple of {GameConstants.MATCHING_COUNT}.");

        for (int attempt = 0; attempt < GameConstants.GENERATOR_MAX_ATTEMPTS; attempt++)
        {
            var virtualTiles = levelData.LayoutCoordinates
                .Select(c => new VirtualTile { Position = c.Position, Layer = c.Layer, Size = GameConstants.TILE_SIZE })
                .ToList();

            BuildVirtualGraph(virtualTiles);

            if (TryAssignIds(virtualTiles, levelData.AllowedIconIds))
            {
                ShuffleTilePositions(virtualTiles);
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
        var idBag = new List<string>(allowedIds);
        ShuffleList(idBag);
        int bagIndex = 0;

        while (unassigned.Count > 0)
        {
            var selectedGroup = new List<VirtualTile>();

            for (int i = 0; i < GameConstants.MATCHING_COUNT; i++)
            {
                var available = unassigned.Where(t => t.BlockedBy.Count == 0).ToList();
                if (available.Count == 0) return false; 

                var pickedTile = available[Random.Range(0, available.Count)];
                selectedGroup.Add(pickedTile);
                unassigned.Remove(pickedTile);

                foreach (var blocked in pickedTile.Blocking) blocked.BlockedBy.Remove(pickedTile);
            }

            string selectedId = idBag[bagIndex];
            bagIndex++;

            if (bagIndex >= idBag.Count)
            {
                bagIndex = 0;
                ShuffleList(idBag);
            }

            foreach (var tile in selectedGroup) tile.AssignedId = selectedId;
        }

        return true;
    }

    private void ShuffleList<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int swapIndex = Random.Range(0, i + 1);
            (list[i], list[swapIndex]) = (list[swapIndex], list[i]);
        }
    }

    private void ShuffleTilePositions(List<VirtualTile> tiles)
    {
        var groupedByState = tiles.GroupBy(t => t.BlockedBy.Count).ToList();
        
        foreach (var group in groupedByState)
        {
            var idList = group.Select(t => t.AssignedId).ToList();
            ShuffleList(idList);

            int index = 0;
            foreach (var tile in group)
            {
                tile.AssignedId = idList[index];
                index++;
            }
        }
    }

    private bool IsOverlapping(VirtualTile a, VirtualTile b)
    {
        return a.Position.x < b.Position.x + b.Size &&
               a.Position.x + a.Size > b.Position.x &&
               a.Position.y < b.Position.y + b.Size &&
               a.Position.y + a.Size > b.Position.y;
    }
}