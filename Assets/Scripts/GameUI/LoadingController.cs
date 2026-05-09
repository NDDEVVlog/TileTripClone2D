using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Cysharp.Threading.Tasks;
using DG.Tweening;

public class LoadingController : MonoBehaviour
{
    [SerializeField] private Slider _progressBar;
    [SerializeField] private AudioClip _mainBgm;
    [SerializeField] private Image _transitionImage;
    [SerializeField] private float _minimumLoadingTime = 1.5f;

    private void Start()
    {
        _progressBar.value = 0f;
        ExecuteLoadingFlowAsync().Forget();
    }

    private async UniTaskVoid ExecuteLoadingFlowAsync()
    {
        float targetProgress = 0f;
        float fillSpeed = 1f / _minimumLoadingTime;

        var resourceRequest = Resources.LoadAsync<LevelDatabase>(GameConstants.DB_RESOURCE_PATH);
        
        while (!resourceRequest.isDone)
        {
            targetProgress = resourceRequest.progress * 0.3f; 
            _progressBar.value = Mathf.MoveTowards(_progressBar.value, targetProgress, Time.deltaTime * fillSpeed);
            await UniTask.Yield();
        }

        ProgressService.Initialize(resourceRequest.asset as LevelDatabase);

        if (_mainBgm != null && AudioService.Instance != null)
        {
            AudioService.Instance.PlayBGM(_mainBgm);
        }

        var sceneOp = SceneManager.LoadSceneAsync(GameConstants.SCENE_HOME);
        sceneOp.allowSceneActivation = false;

        while (sceneOp.progress < 0.9f || _progressBar.value < 1f)
        {
            float normalizedSceneProgress = sceneOp.progress / 0.9f;
            targetProgress = 0.3f + (normalizedSceneProgress * 0.7f);

            _progressBar.value = Mathf.MoveTowards(_progressBar.value, targetProgress, Time.deltaTime * fillSpeed);
            await UniTask.Yield();
        }

        _progressBar.value = 1f;

        await _transitionImage.DOFade(1f, 0.5f).AsyncWaitForCompletion();
        
        sceneOp.allowSceneActivation = true;
    }
}