using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraFitter : MonoBehaviour
{
    [SerializeField] private float _portraitWidth = 8f; 
    [SerializeField] private float _portraitHeight = 12f;
    [SerializeField] private float _landscapeWidth = 16f;
    [SerializeField] private float _landscapeHeight = 8f;
    [SerializeField] private float _padding = 1f;

    private Camera _camera;
    private int _lastScreenWidth;
    private int _lastScreenHeight;

    private void Awake()
    {
        _camera = GetComponent<Camera>();
        FitToScreen();
    }

    private void Update()
    {
        if (Screen.width != _lastScreenWidth || Screen.height != _lastScreenHeight)
        {
            FitToScreen();
        }
    }

    private void FitToScreen()
    {
        _lastScreenWidth = Screen.width;
        _lastScreenHeight = Screen.height;

        bool isPortrait = Screen.width < Screen.height;
        float targetWidth = isPortrait ? _portraitWidth : _landscapeWidth;
        float targetHeight = isPortrait ? _portraitHeight : _landscapeHeight;

        float screenRatio = (float)Screen.width / Screen.height;
        float targetRatio = targetWidth / targetHeight;

        if (screenRatio >= targetRatio)
        {
            _camera.orthographicSize = (targetHeight / 2f) + _padding;
        }
        else
        {
            float differenceInSize = targetRatio / screenRatio;
            _camera.orthographicSize = ((targetHeight / 2f) + _padding) * differenceInSize;
        }
    }
}