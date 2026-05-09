using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Cysharp.Threading.Tasks;

public class TrayController : MonoBehaviour
{   
    [SerializeField] private Sprite baseGridSprite;
    [SerializeField] private int iconSize;

    [SerializeField] private Transform _startPoint;
    [SerializeField] private float _spacing = 1.2f;
    

    public event Action OnDefeat;
    public event Action OnMatched;

    private readonly List<TileView> _tiles = new();
    private int _capacity;
    private bool _isProcessingMatch;

    public void Initialize(int capacity)
    {
        _capacity = capacity;
        _tiles.Clear();
        _isProcessingMatch = false;
    }

    public bool TryAdd(TileView tile)
    {
        if (_tiles.Count >= _capacity || _isProcessingMatch) return false;

        InsertGrouped(tile);
        
        var moveTasks = RepositionAsync();
        ProcessMatchesAsync(tile.Id, moveTasks).Forget();

        if (_tiles.Count >= _capacity && !HasPendingMatch(tile.Id)) 
        {
            OnDefeat?.Invoke();
        }

        return true;
    }

    private void InsertGrouped(TileView tile)
    {
        int index = _tiles.FindLastIndex(t => t.Id == tile.Id);
        if (index >= 0) _tiles.Insert(index + 1, tile);
        else _tiles.Add(tile);
    }

    private List<UniTask> RepositionAsync()
    {
        var tasks = new List<UniTask>();
        Vector2 start = _startPoint.position;
        
        for (int i = 0; i < _tiles.Count; i++)
        {
            Vector2 targetPos = start + new Vector2(i * _spacing, 0);
            tasks.Add(_tiles[i].MoveToTrayAsync(targetPos));
        }
        
        return tasks;
    }

    private bool HasPendingMatch(string targetId)
    {
        return _tiles.Count(t => t.Id == targetId) >= 3;
    }

    private async UniTask ProcessMatchesAsync(string targetId, List<UniTask> moveTasks)
    {
        var matched = _tiles.Where(t => t.Id == targetId).ToList();

        if (matched.Count == 3)
        {
            _isProcessingMatch = true;
            foreach (var t in matched) _tiles.Remove(t);

            await UniTask.WhenAll(moveTasks);

            var mergeTasks = matched.Select(t => t.ExecuteMergeAnimationAsync());
            await UniTask.WhenAll(mergeTasks);
            
            await UniTask.WhenAll(RepositionAsync());
            
            _isProcessingMatch = false;
            OnMatched?.Invoke();
        }
    }
}