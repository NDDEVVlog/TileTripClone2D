using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Cysharp.Threading.Tasks;
using DG.Tweening;

public class TrayController : MonoBehaviour
{   
    [SerializeField] private GameObject _gridSlotPrefab;
    [SerializeField] private Transform _startPoint;
    [SerializeField] private float _spacing = 1.2f;
    [SerializeField] private float _animationOffset = 1.5f;
    [SerializeField] private float _animationDuration = 0.4f;

    public event Action OnDefeat;
    public event Action OnMatched;

    private readonly List<TileView> _tiles = new();
    private readonly List<SpriteRenderer> _gridSlots = new();
    private int _capacity;
    private bool _isProcessingMatch;

    public void Initialize(int capacity)
    {
        _capacity = capacity;
        _tiles.Clear();
        _isProcessingMatch = false;
        ClearGridSlots();
    }

    public async UniTask SpawnGridAsync()
    {
        var tasks = new List<UniTask>();
        Vector2 start = _startPoint.position;

        for (int i = 0; i < _capacity; i++)
        {
            Vector2 targetPos = start + new Vector2(i * _spacing, 0);
            Vector2 startPos = targetPos + Vector2.down * _animationOffset;

            GameObject slotObj = Instantiate(_gridSlotPrefab, startPos, Quaternion.identity, _startPoint);
            slotObj.name = $"GridSlot_{i}";

            if (!slotObj.TryGetComponent<SpriteRenderer>(out var sr)) sr = slotObj.AddComponent<SpriteRenderer>();
            
            Color color = sr.color;
            color.a = 0;
            sr.color = color;
            _gridSlots.Add(sr);

            tasks.Add(AnimateSlotSpawnAsync(slotObj.transform, sr, targetPos, i * 0.05f));
        }

        await UniTask.WhenAll(tasks);
    }

    public async UniTask DespawnGridAsync()
    {
        var tasks = _gridSlots.Select((sr, i) => AnimateSlotDespawnAsync(sr, i * 0.05f));
        await UniTask.WhenAll(tasks);
        ClearGridSlots();
    }

    private async UniTask AnimateSlotSpawnAsync(Transform t, SpriteRenderer sr, Vector2 targetPos, float delay)
    {
        await UniTask.Delay(TimeSpan.FromSeconds(delay));
        var sequence = DOTween.Sequence();
        sequence.Join(t.DOMove(targetPos, _animationDuration).SetEase(Ease.OutBack));
        sequence.Join(sr.DOFade(1f, _animationDuration));
        await sequence.AsyncWaitForCompletion();
    }

    private async UniTask AnimateSlotDespawnAsync(SpriteRenderer sr, float delay)
    {
        await UniTask.Delay(TimeSpan.FromSeconds(delay));
        Vector2 targetPos = (Vector2)sr.transform.position + Vector2.up * _animationOffset;
        var sequence = DOTween.Sequence();
        sequence.Join(sr.transform.DOMove(targetPos, _animationDuration).SetEase(Ease.InBack));
        sequence.Join(sr.DOFade(0f, _animationDuration));
        await sequence.AsyncWaitForCompletion();
    }

    private void ClearGridSlots()
    {
        foreach (var slot in _gridSlots.Where(s => s != null)) Destroy(slot.gameObject);
        _gridSlots.Clear();
    }

    public bool TryAdd(TileView tile)
    {
        if (_tiles.Count >= _capacity || _isProcessingMatch) return false;

        int index = _tiles.FindLastIndex(t => t.Id == tile.Id);
        if (index >= 0) _tiles.Insert(index + 1, tile);
        else _tiles.Add(tile);
        
        var moveTasks = RepositionAsync();
        ProcessMatchesAsync(tile.Id, moveTasks).Forget();

        if (_tiles.Count >= _capacity && !HasPendingMatch(tile.Id)) OnDefeat?.Invoke();
        return true;
    }

    private List<UniTask> RepositionAsync()
    {
        Vector2 start = _startPoint.position;
        return _tiles.Select((t, i) => t.MoveToTrayAsync(start + new Vector2(i * _spacing, 0))).ToList();
    }

    private bool HasPendingMatch(string targetId) => _tiles.Count(t => t.Id == targetId) >= 3;

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