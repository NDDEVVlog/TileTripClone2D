using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using DG.Tweening;
using Cysharp.Threading.Tasks;

public class HomeController : MonoBehaviour
{
    [Header("Main UI Elements")]
    [SerializeField] private Button _playButton;
    [SerializeField] private TextMeshProUGUI _playButtonText;
    [SerializeField] private RectTransform _mainGameIcon;
    [SerializeField] private Button _selectLevelButton;

    [Header("Transitions")]
    [SerializeField] private Image _transitionImage;
    [SerializeField] private float _transitionDuration = 0.5f;
    [SerializeField] private float _animDuration = 0.4f;

    [Header("Dynamic Level Selector")]
    [SerializeField] private GameObject _levelButtonPrefab;
    [SerializeField] private RectTransform _levelGridContainer;
    [SerializeField] private RectTransform _levelScrollView;

    private static bool _isFirstLoad = true;
    private readonly List<RectTransform> _spawnedLevelButtons = new();
    
    private bool _isTransitioning;
    private bool _isLevelTableShowing;

    private void Start()
    {
        InitializeDatabase();
        SetupMainPlayButton();
        SetupSelectLevelButton();
        SetupDynamicLevelSelector();

        _levelScrollView.localScale = Vector3.zero;
        _levelScrollView.gameObject.SetActive(false);

        ExecuteEnterAnimationsAsync().Forget();
    }

    private void InitializeDatabase()
    {
        if (ProgressService.Database != null) return;
        var db = Resources.Load<LevelDatabase>(GameConstants.DB_RESOURCE_PATH);
        ProgressService.Initialize(db);
    }

    private void SetupMainPlayButton()
    {
        if (_playButton == null) return;

        int targetLevelIndex = ProgressService.UnlockedLevel;
        int maxLevelIndex = ProgressService.Database.Levels.Length - 1;

        if (targetLevelIndex > maxLevelIndex)
        {
            targetLevelIndex = maxLevelIndex;
        }

        if (_playButtonText != null)
        {
            _playButtonText.text = $"PLAY LEVEL {targetLevelIndex + 1}";
        }
        
        _playButton.onClick.AddListener(() => ExecuteExitAndLoadGameplayAsync(targetLevelIndex).Forget());
    }

    private void SetupSelectLevelButton()
    {
        if (_selectLevelButton == null) return;
        _selectLevelButton.onClick.AddListener(() => ToggleLevelTableAsync().Forget());
    }

    private void SetupDynamicLevelSelector()
    {
        if (_levelButtonPrefab == null || _levelGridContainer == null) return;

        foreach (Transform child in _levelGridContainer) Destroy(child.gameObject);
        _spawnedLevelButtons.Clear();

        int totalLevels = ProgressService.Database.Levels.Length;

        for (int i = 0; i < totalLevels; i++)
        {
            int levelIndex = i; 
            GameObject btnObj = Instantiate(_levelButtonPrefab, _levelGridContainer);
            Button btn = btnObj.GetComponent<Button>();
            TextMeshProUGUI txt = btnObj.GetComponentInChildren<TextMeshProUGUI>();

            bool isUnlocked = levelIndex <= ProgressService.UnlockedLevel;
            btn.interactable = isUnlocked;

            if (txt != null) txt.text = (levelIndex + 1).ToString();

            if (isUnlocked)
            {
                btn.onClick.AddListener(() => ExecuteExitAndLoadGameplayAsync(levelIndex).Forget());
            }

            RectTransform btnRect = btnObj.GetComponent<RectTransform>();
            btnRect.localScale = Vector3.zero;
            _spawnedLevelButtons.Add(btnRect);
        }
    }

    private async UniTaskVoid ToggleLevelTableAsync()
    {
        if (_isTransitioning) return;
        _isTransitioning = true;
        ToggleUIInteractability(false);

        _isLevelTableShowing = !_isLevelTableShowing;

        if (_isLevelTableShowing)
        {
            await ShowLevelTableAsync();
        }
        else
        {
            await HideLevelTableAsync();
        }

        ToggleUIInteractability(true);
        _isTransitioning = false;
    }

    private async UniTask ShowLevelTableAsync()
    {
        _levelScrollView.gameObject.SetActive(true);
        
        await _levelScrollView.DOScale(1f, _animDuration)
            .SetEase(Ease.OutBack)
            .SetLink(_levelScrollView.gameObject)
            .AsyncWaitForCompletion();

        var cascadeTasks = new List<UniTask>();
        for (int i = 0; i < _spawnedLevelButtons.Count; i++)
        {
            var task = _spawnedLevelButtons[i].DOScale(1f, 0.3f)
                .SetEase(Ease.OutBack)
                .SetDelay(i * 0.03f)
                .SetLink(_spawnedLevelButtons[i].gameObject)
                .AsyncWaitForCompletion().AsUniTask();
            cascadeTasks.Add(task);
        }

        await UniTask.WhenAll(cascadeTasks);
    }

    private async UniTask HideLevelTableAsync()
    {
        var cascadeTasks = new List<UniTask>();
        for (int i = _spawnedLevelButtons.Count - 1; i >= 0; i--)
        {
            var task = _spawnedLevelButtons[i].DOScale(0f, 0.2f)
                .SetEase(Ease.InBack)
                .SetDelay((_spawnedLevelButtons.Count - 1 - i) * 0.02f)
                .SetLink(_spawnedLevelButtons[i].gameObject)
                .AsyncWaitForCompletion().AsUniTask();
            cascadeTasks.Add(task);
        }

        await UniTask.WhenAll(cascadeTasks);

        await _levelScrollView.DOScale(0f, _animDuration)
            .SetEase(Ease.InBack)
            .SetLink(_levelScrollView.gameObject)
            .AsyncWaitForCompletion();

        _levelScrollView.gameObject.SetActive(false);
    }

    private async UniTaskVoid ExecuteEnterAnimationsAsync()
    {
        _isTransitioning = true;
        ToggleUIInteractability(false);

        if (_isFirstLoad)
        {
            _isFirstLoad = false;
            _transitionImage.gameObject.SetActive(true);
            _transitionImage.color = new Color(0, 0, 0, 1f);
            
            await _transitionImage.DOFade(0f, _transitionDuration)
                .SetEase(Ease.Linear)
                .SetLink(gameObject)
                .AsyncWaitForCompletion();
                
            _transitionImage.gameObject.SetActive(false);
        }
        else
        {
            _transitionImage.gameObject.SetActive(false);
            _mainGameIcon.localScale = Vector3.zero;
            _playButton.GetComponent<RectTransform>().localScale = Vector3.zero;
            
            if (_selectLevelButton != null) 
                _selectLevelButton.GetComponent<RectTransform>().localScale = Vector3.zero;

            var tasks = new List<UniTask>
            {
                _mainGameIcon.DOScale(1f, _animDuration).SetEase(Ease.OutBack).SetLink(gameObject).AsyncWaitForCompletion().AsUniTask(),
                _playButton.GetComponent<RectTransform>().DOScale(1f, _animDuration).SetEase(Ease.OutBack).SetDelay(0.15f).SetLink(gameObject).AsyncWaitForCompletion().AsUniTask()
            };

            if (_selectLevelButton != null)
            {
                tasks.Add(_selectLevelButton.GetComponent<RectTransform>().DOScale(1f, _animDuration)
                    .SetEase(Ease.OutBack).SetDelay(0.1f).SetLink(gameObject).AsyncWaitForCompletion().AsUniTask());
            }

            await UniTask.WhenAll(tasks);
        }

        ToggleUIInteractability(true);
        _isTransitioning = false;
    }

    private async UniTaskVoid ExecuteExitAndLoadGameplayAsync(int levelIndex)
    {
        if (_isTransitioning) return;
        _isTransitioning = true;
        ToggleUIInteractability(false);

        var tasks = new List<UniTask>();

        if (_isLevelTableShowing)
        {
            tasks.Add(HideLevelTableAsync());
        }

        tasks.Add(_mainGameIcon.DOScale(0f, _animDuration).SetEase(Ease.InBack).SetLink(gameObject).AsyncWaitForCompletion().AsUniTask());
        tasks.Add(_playButton.GetComponent<RectTransform>().DOScale(0f, _animDuration).SetEase(Ease.InBack).SetLink(gameObject).AsyncWaitForCompletion().AsUniTask());

        if (_selectLevelButton != null)
        {
            tasks.Add(_selectLevelButton.GetComponent<RectTransform>().DOScale(0f, _animDuration).SetEase(Ease.InBack).SetLink(gameObject).AsyncWaitForCompletion().AsUniTask());
        }

        await UniTask.WhenAll(tasks);

        ProgressService.CurrentLevelIndex = levelIndex;
        SceneManager.LoadScene(GameConstants.SCENE_GAMEPLAY);
    }

    private void ToggleUIInteractability(bool state)
    {
        if (_playButton != null) _playButton.interactable = state;
        if (_selectLevelButton != null) _selectLevelButton.interactable = state;
        
        foreach (var rt in _spawnedLevelButtons)
        {
            var btn = rt.GetComponent<Button>();
            if (btn != null)
            {
                int btnIndex = _spawnedLevelButtons.IndexOf(rt);
                btn.interactable = state && (btnIndex <= ProgressService.UnlockedLevel);
            }
        }
    }
}