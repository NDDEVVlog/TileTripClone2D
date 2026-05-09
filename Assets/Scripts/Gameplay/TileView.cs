using System;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using Cysharp.Threading.Tasks;
using UnityEngine.EventSystems;

[RequireComponent(typeof(BoxCollider2D))]
public class TileView : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private SpriteRenderer _baseRenderer;
    [SerializeField] private SpriteRenderer _iconRenderer;

    public string Id { get; private set; }
    public int Layer { get; private set; }
    public float Size => GameConstants.TILE_SIZE;
    public bool IsClickable => _blockedBy.Count == 0;

    public event Action<TileView> OnClicked;
    
    private Action<TileView> _onReturnToPool;
    private readonly HashSet<TileView> _blockedBy = new();
    private readonly HashSet<TileView> _blocking = new();
    private BoxCollider2D _collider;
    private bool _isInTray;

    private void Awake()
    {
        _collider = GetComponent<BoxCollider2D>();
    }

    public void Setup(string id, int layer, Sprite iconSprite, Action<TileView> onReturnToPool)
    {
        Id = id;
        Layer = layer;
        _iconRenderer.sprite = iconSprite;  
        _onReturnToPool = onReturnToPool;
        _isInTray = false;

        ConfigureSortingOrder(layer);
        AdjustIconScale(iconSprite);
    }

    private void ConfigureSortingOrder(int layer)
    {
        int ySortingOffset = Mathf.RoundToInt(-transform.position.y * 100);
        int baseSortingOrder = (layer * GameConstants.SORTING_BASE_OFFSET) + ySortingOffset;

        _baseRenderer.sortingOrder = baseSortingOrder;
        _iconRenderer.sortingOrder = baseSortingOrder + GameConstants.SORTING_ICON_OFFSET;
    }

    private void AdjustIconScale(Sprite iconSprite)
    {
        if (_baseRenderer.sprite != null && iconSprite != null)
        {
            Vector2 baseSize = _baseRenderer.sprite.bounds.size;
            Vector2 iconSize = iconSprite.bounds.size;

            float targetSize = Mathf.Min(baseSize.x, baseSize.y) * 0.75f; 
            float scaleFactor = targetSize / Mathf.Max(iconSize.x, iconSize.y);

            _iconRenderer.transform.localScale = new Vector3(scaleFactor, scaleFactor, 1f);
        }
    }

    public async UniTask DeSpawnAnimationAsync()
    {
        transform.DOKill();
        await transform.DOScale(Vector3.zero, GameConstants.ANIMATION_DURATION_FAST)
            .SetEase(Ease.InBack)
            .SetLink(gameObject)
            .AsyncWaitForCompletion();

        ResetForPool();
        _onReturnToPool?.Invoke(this);
    }

    public async UniTask SpawnPopupAnimationAsync()
    {
        transform.localScale = Vector3.zero;
        UpdateVisuals();
        
        await transform.DOScale(GameConstants.TILE_SPAWN_SCALE, GameConstants.ANIMATION_DURATION_SLOW)
            .SetEase(Ease.OutBack)
            .SetLink(gameObject)
            .AsyncWaitForCompletion();
    }

    public void AddBlocker(TileView tileAbove)
    {
        _blockedBy.Add(tileAbove);
        tileAbove._blocking.Add(this);
        UpdateVisuals();
    }

    public void RemoveBlocker(TileView tileAbove)
    {
        _blockedBy.Remove(tileAbove);
        UpdateVisuals();
    }

    public void DetachForTray()
    {
        _isInTray = true;
        foreach (var tileBelow in _blocking) tileBelow.RemoveBlocker(this);
        
        _blocking.Clear();
        _blockedBy.Clear();
        _collider.enabled = false;
        
        _baseRenderer.sortingOrder = GameConstants.SORTING_DETACHED_BASE;
        _iconRenderer.sortingOrder = GameConstants.SORTING_DETACHED_ICON;
        _baseRenderer.color = Color.white;
        _iconRenderer.color = Color.white;
    }

    private void UpdateVisuals()
    {
        Color color = IsClickable ? Color.white : GameConstants.TILE_LOCKED_COLOR;
        _baseRenderer.DOColor(color, GameConstants.ANIMATION_DURATION_COLOR).SetLink(gameObject);
        _iconRenderer.DOColor(color, GameConstants.ANIMATION_DURATION_COLOR).SetLink(gameObject);
        _collider.enabled = IsClickable;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (IsClickable && !_isInTray) 
        {
            OnClicked?.Invoke(this);
        }
    }

    public async UniTask MoveToTrayAsync(Vector2 targetPosition)
    {
        transform.DOKill(); 
        await transform.DOMove(targetPosition, GameConstants.ANIMATION_DURATION_NORMAL)
            .SetEase(Ease.OutQuad)
            .SetLink(gameObject)
            .AsyncWaitForCompletion();
    }

    public async UniTask ExecuteMergeAnimationAsync()
    {
        transform.DOKill();
        
        await transform.DOMove(transform.position + Vector3.up * 0.5f, GameConstants.ANIMATION_DURATION_NORMAL)
            .SetEase(Ease.InQuad)
            .SetLink(gameObject)
            .AsyncWaitForCompletion();

        await transform.DOScale(Vector3.zero, GameConstants.ANIMATION_DURATION_FAST)
            .SetLink(gameObject)
            .AsyncWaitForCompletion();

        ResetForPool();
        _onReturnToPool?.Invoke(this);
    }

    private void ResetForPool()
    {
        transform.DOKill();
        _baseRenderer.DOKill();
        _iconRenderer.DOKill();
        _blockedBy.Clear();
        _blocking.Clear();
        OnClicked = null;
    }
}