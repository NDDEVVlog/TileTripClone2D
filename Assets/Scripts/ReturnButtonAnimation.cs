using UnityEngine;
using DG.Tweening;

public class ReturnButtonAnimation : MonoBehaviour
{
    [SerializeField] private RectTransform _buttonRect;
    [SerializeField] private float _animationDuration = 0.3f;
    [SerializeField] private Vector3 _targetScale = Vector3.one * 1.2f;

    private Vector3 _originalScale;

    private void Start()
    {
        if (_buttonRect == null)
            _buttonRect = GetComponent<RectTransform>();

        _originalScale = _buttonRect.localScale;
        PopUpAnimation();
    }

    public void PopUpAnimation()
    {
        _buttonRect.localScale = _originalScale;
        _buttonRect.DOScale(_targetScale, _animationDuration).SetEase(Ease.OutBack);
    }
    public void PopDownAnimation()
    {
        _buttonRect.DOScale(Vector3.zero, _animationDuration).SetEase(Ease.InBack);   
    }
}
