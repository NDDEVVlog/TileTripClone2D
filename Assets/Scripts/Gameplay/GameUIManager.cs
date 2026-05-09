using System;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using Cysharp.Threading.Tasks;

public class GameUIManager : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private CanvasGroup _winPanel;
    [SerializeField] private CanvasGroup _losePanel;

    [Header("Win Buttons")]
    [SerializeField] private Button _winNextButton;
    [SerializeField] private Button _winRetryButton;

    [Header("Lose Buttons")]
    [SerializeField] private Button _loseHomeButton;
    [SerializeField] private Button _loseRetryButton;

    [Header("Settings")]
    [SerializeField] private float _animationDuration = 0.3f;

    public event Action OnRetryClicked;
    public event Action OnHomeClicked;
    public event Action OnNextLevelClicked;

    private void Awake()
    {
        InitializePanels();
        RegisterButtons();
    }

    private void InitializePanels()
    {
        _winPanel.gameObject.SetActive(false);
        _losePanel.gameObject.SetActive(false);
        _winPanel.alpha = 0;
        _losePanel.alpha = 0;
    }

    private void RegisterButtons()
    {
        _winRetryButton.onClick.AddListener(() => OnRetryClicked?.Invoke());
        _loseRetryButton.onClick.AddListener(() => OnRetryClicked?.Invoke());
        
        _winNextButton.onClick.AddListener(() => OnNextLevelClicked?.Invoke());
        _loseHomeButton.onClick.AddListener(() => OnHomeClicked?.Invoke());
    }

    public async UniTask ShowWinPanelAsync(bool hasNextLevel)
    {   
        if (_winNextButton != null)
        {
            _winNextButton.gameObject.SetActive(hasNextLevel);
        }
        await AnimatePanelAsync(_winPanel, true);
    }

    public async UniTask ShowLosePanelAsync()
    {
        await AnimatePanelAsync(_losePanel, true);
    }

    public async UniTask HideAllPanelsAsync()
    {
        var tasks = new[]
        {
            AnimatePanelAsync(_winPanel, false),
            AnimatePanelAsync(_losePanel, false)
        };
        
        await UniTask.WhenAll(tasks);
    }

    private async UniTask AnimatePanelAsync(CanvasGroup panel, bool isShowing)
    {
        if (isShowing)
        {
            panel.gameObject.SetActive(true);
            panel.transform.localScale = Vector3.one * 0.8f;
            
            var sequence = DOTween.Sequence();
            sequence.Join(panel.DOFade(1f, _animationDuration));
            sequence.Join(panel.transform.DOScale(1f, _animationDuration).SetEase(Ease.OutBack));
            
            await sequence.AsyncWaitForCompletion();
            panel.interactable = true;
        }
        else
        {
            if (!panel.gameObject.activeSelf) return;

            var sequence = DOTween.Sequence();
            sequence.Join(panel.DOFade(0f, _animationDuration));
            sequence.Join(panel.transform.DOScale(0.8f, _animationDuration).SetEase(Ease.InBack));
            panel.interactable = false; 
            await sequence.AsyncWaitForCompletion();
            panel.gameObject.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        _winRetryButton.onClick.RemoveAllListeners();
        _loseRetryButton.onClick.RemoveAllListeners();
        _winNextButton.onClick.RemoveAllListeners();
        _loseHomeButton.onClick.RemoveAllListeners();
    }
}