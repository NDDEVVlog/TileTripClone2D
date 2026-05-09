using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
using UnityEngine.SceneManagement;
using System.Linq;

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
    private BoardFactory _boardFactory;
    private bool _isGameActive;

    private void Start()
    {
        _boardFactory = new BoardFactory(_pool, _boardContainer, _iconSprites, _activeTiles);
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
        await _boardFactory.GenerateBoardAsync(_currentLevelData, HandleTileClick);
        _isGameActive = true;
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

            if (_activeTiles.Count == 0) HandleVictory();
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
        ShowEndGamePanelAsync(true, ProgressService.HasNextLevel()).Forget();
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
        if (isVictory) await _uiManager.ShowWinPanelAsync(hasNext);
        else await _uiManager.ShowLosePanelAsync();
    }

    private void HandleRetry() => ResetGameSequenceAsync().Forget();

    private async UniTaskVoid ResetGameSequenceAsync()
    {
        await _uiManager.HideAllPanelsAsync();
        await ClearBoardAsync();
        _trayController.ClearTray();

        await _trayController.DespawnGridAsync();
        InitializeGameSequenceAsync().Forget();
    }

    private void HandleReturnHome() => ReturnHomeSequenceAsync().Forget();

    private void HandleNextLevel()
    {
        if (ProgressService.HasNextLevel())
        {
            ProgressService.MoveToNextLevel();
            ResetGameSequenceAsync().Forget();
        }
        else ReturnHomeSequenceAsync().Forget();
    }

    public void HandleExitToHome() => ReturnHomeSequenceAsync().Forget();
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
        var despawnTasks = _activeTiles.Select(tile => 
        {
            tile.OnClicked -= HandleTileClick;
            return tile.DeSpawnAnimationAsync();
        });
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