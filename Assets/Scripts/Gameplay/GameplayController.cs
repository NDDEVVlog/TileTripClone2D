using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine.SceneManagement;

public class GameplayController : MonoBehaviour
{
    [SerializeField] private ObjectPool _pool;
    [SerializeField] private TrayController _trayController;
    [SerializeField] private ComboManager _comboManager;
    [SerializeField] private GameUIManager _uiManager;
    [SerializeField] private Transform _boardContainer;
    [SerializeField] private Sprite[] _iconSprites;
    
    [Space]
    [SerializeField] private AudioClip _tapSfx;
    [SerializeField] private AudioClip _matchSfx;
    [SerializeField] private AudioClip _winSfx;
    [SerializeField] private AudioClip _loseSfx;

    private LevelData _currentLevelData;
    private readonly HashSet<TileView> _activeTiles = new();
    private bool _isGameActive;

    private void Start()
    {
        BindEvents();
        InitializeGameSequenceAsync().Forget();
    }

    private void BindEvents()
    {
        _trayController.OnDefeat += HandleDefeat;
        _trayController.OnMatched += HandleMatch;

        _uiManager.OnRetryClicked += HandleRetry;
        _uiManager.OnHomeClicked += HandleReturnHome;
        _uiManager.OnNextLevelClicked += HandleNextLevel;
    }

    private async UniTaskVoid InitializeGameSequenceAsync()
    {
        _currentLevelData = ProgressService.GetCurrentLevelData();
        if (_currentLevelData == null)
        {   
            SceneManager.LoadScene(GameConstants.SCENE_HOME);
            return;
        }
        
        _pool.Initialize();
        _trayController.Initialize(_currentLevelData.RackCapacity);

        await _trayController.SpawnGridAsync();
        await GenerateBoardAsync();
    }

    private async UniTask GenerateBoardAsync()
    {
        var generator = new SolvableGenerator();
        var tilesData = generator.Generate(_currentLevelData);
        var instances = new List<TileView>();

        var groupedByLayer = tilesData.GroupBy(t => t.Layer).OrderBy(g => g.Key).ToList();

        foreach (var layerGroup in groupedByLayer)
        {
            var spawnTasks = new List<UniTask>();

            foreach (var data in layerGroup)
            {
                var tile = _pool.Get<TileView>(GameConstants.POOL_KEY_TILE);
                tile.transform.SetPositionAndRotation(data.Position, Quaternion.identity);
                tile.transform.SetParent(_boardContainer);

                var sprite = _iconSprites.First(s => s.name == data.Id);
                
                tile.Setup(data.Id, data.Layer, sprite, t => _pool.Return(t.gameObject));
                tile.OnClicked += HandleTileClick;
                
                instances.Add(tile);
                _activeTiles.Add(tile);
                
                spawnTasks.Add(tile.SpawnPopupAnimationAsync());
            }

            await UniTask.WhenAll(spawnTasks);
            await UniTask.Delay(100);
        }

        BuildDependencies(instances);
        _isGameActive = true;
    }

    private void BuildDependencies(IReadOnlyList<TileView> tiles)
    {
        int count = tiles.Count;
        for (int i = 0; i < count; i++)
        {
            for (int j = i + 1; j < count; j++)
            {
                var a = tiles[i];
                var b = tiles[j];

                if (a.Layer == b.Layer || !IsOverlapping(a, b)) continue;

                if (a.Layer < b.Layer) a.AddBlocker(b);
                else b.AddBlocker(a);
            }
        }
    }

    private bool IsOverlapping(TileView a, TileView b)
    {
        return a.transform.position.x < b.transform.position.x + b.Size &&
               a.transform.position.x + a.Size > b.transform.position.x &&
               a.transform.position.y < b.transform.position.y + b.Size &&
               a.transform.position.y + a.Size > b.transform.position.y;
    }

    private void HandleTileClick(TileView tile)
    {
        if (!_activeTiles.Contains(tile) || !_isGameActive) return;

        if (_trayController.TryAdd(tile))
        {
            AudioService.Instance?.PlaySFX(_tapSfx);
            _comboManager?.HandleTap(tile.Id);
            
            _activeTiles.Remove(tile);
            tile.DetachForTray();

            if (_activeTiles.Count == 0) 
            {
                HandleVictory();
            }
        }
    }

    private void HandleMatch()
    {
        AudioService.Instance?.PlaySFX(_matchSfx);
        _comboManager?.HandleMatch();
    }

    private void HandleVictory()
    {
        _isGameActive = false;
        //AudioService.Instance?.PlaySFX(_winSfx);
        
        ProgressService.UnlockNextLevel();
        bool hasNext = ProgressService.HasNextLevel();
        
        ShowEndGamePanelAsync(true, hasNext).Forget();
    }

    private void HandleDefeat()
    {
        _isGameActive = false;
        //AudioService.Instance?.PlaySFX(_loseSfx);
        
        ShowEndGamePanelAsync(false).Forget();
    }

    private async UniTaskVoid ShowEndGamePanelAsync(bool isVictory, bool hasNext = false)
    {
        await UniTask.Delay(1000); 

        if (isVictory)
            await _uiManager.ShowWinPanelAsync(hasNext);
        else
            await _uiManager.ShowLosePanelAsync();
    }

    private void HandleRetry()
    {
        ResetGameSequenceAsync().Forget();
    }

    private async UniTaskVoid ResetGameSequenceAsync()
    {
        await _uiManager.HideAllPanelsAsync();
        await ClearBoardAsync();
        await _trayController.DespawnGridAsync();
        
        InitializeGameSequenceAsync().Forget();
    }

    private void HandleReturnHome()
    {
        ReturnHomeSequenceAsync().Forget();
    }

    private void HandleNextLevel()
    {
        if (ProgressService.HasNextLevel())
        {
            ProgressService.MoveToNextLevel();
            ResetGameSequenceAsync().Forget();
        }
        else
        {
            ReturnHomeSequenceAsync().Forget();
        }
    }

    private async UniTaskVoid ReturnHomeSequenceAsync()
    {
        _isGameActive = false;
        await _uiManager.HideAllPanelsAsync();
        await ClearBoardAsync();
        await _trayController.DespawnGridAsync();
        
        SceneManager.LoadScene(GameConstants.SCENE_HOME);
    }

    private async UniTask ClearBoardAsync()
    {
        var despawnTasks = new List<UniTask>();

        foreach (var tile in _activeTiles)
        {
            tile.OnClicked -= HandleTileClick;
            despawnTasks.Add(tile.DeSpawnAnimationAsync());
        }

        await UniTask.WhenAll(despawnTasks);
        _activeTiles.Clear();
    }

    private void OnDestroy()
    {
        _trayController.OnDefeat -= HandleDefeat;
        _trayController.OnMatched -= HandleMatch;

        _uiManager.OnRetryClicked -= HandleRetry;
        _uiManager.OnHomeClicked -= HandleReturnHome;
        _uiManager.OnNextLevelClicked -= HandleNextLevel;
    }
}