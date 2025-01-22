using System.Collections;
using System.Threading;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
namespace TCS.Bootstrapper {
    public class SceneLoader : MonoBehaviour {
        [SerializeField] Image m_loadingBar;
        [SerializeField] TextMeshProUGUI m_loadingText;
        [SerializeField] float m_fillSpeed = 0.5f;
        [SerializeField] CanvasGroup m_canvasGroup;
        [SerializeField] Canvas m_canvas;
        [SerializeField] SceneGroup[] m_sceneGroups;

        float m_targetProgress;
        bool m_isLoading;
        public bool IsFinishing { get; private set; }

        public readonly SceneGroupManager Manager = new();

        void Awake() {
            // TODO can remove
            //Manager.OnSceneLoaded += sceneName => Debug.Log("Loaded: " + sceneName);
            //Manager.OnSceneUnloaded += sceneName => Debug.Log("Unloaded: " + sceneName);
            Manager.OnSceneGroupLoaded += OnSceneGroupCleanup;
            SceneEvents.OnLoadInfo += SetLoadingText;
        }
        void OnDestroy() {
            SceneEvents.OnLoadInfo -= SetLoadingText;
        }
        async void Start() {
            await LoadSceneGroup(0);
        }
        void Update() {
            if (!m_isLoading) return;

            float currentFillAmount = m_loadingBar.fillAmount;
            float progressDifference = Mathf.Abs(currentFillAmount - m_targetProgress);

            float dynamicFillSpeed = progressDifference * m_fillSpeed;

            m_loadingBar.fillAmount = Mathf.Lerp(currentFillAmount, m_targetProgress, Time.deltaTime * dynamicFillSpeed);
        }
        async Task FillLastOfLoadingBar(float targetProgress) {
            while (m_loadingBar.fillAmount < targetProgress) {
                m_loadingBar.fillAmount += Time.deltaTime;
                SetLoadingText("Finishing");
                await Task.Yield();
            }
            
            IsFinishing = true;
        }
        #region Scene Loading
        public async Task LoadSceneGroup(int index) {
            m_loadingBar.fillAmount = 0f;
            m_targetProgress = 1f;

            if (index < 0 || index >= m_sceneGroups.Length) {
#if PROJECT_DEBUG
                Debug.LogError("Invalid scene group index: " + index);
#endif
                return;
            }

            var progress = new LoadingProgress();
            progress.Progressed += target => m_targetProgress = Mathf.Max(target, m_targetProgress);

            EnableLoadingCanvas();
            
            await Manager.LoadScenes(m_sceneGroups[index], progress);
            await FillLastOfLoadingBar(1f);

            DisableLoadingCanvas();
        }
        void CloseLoadingCanvas() {
            EnableLoadingCanvas(false);
        }
        void EnableLoadingCanvas(bool enable = true) {
            m_isLoading = enable;
            m_canvas.gameObject.SetActive(enable);

            if (enable) {
                m_canvasGroup.alpha = 1f;
            }
        }
        void DisableLoadingCanvas() {
            SetLoadingText("Cleaning up");
            StartCoroutine(FadeOutCanvas());
        }
        IEnumerator FadeOutCanvas() {
            const float fadeOutTime = 0.85f;
            while (m_canvasGroup.alpha > 0) {
                m_canvasGroup.alpha -= Time.deltaTime * fadeOutTime;
                yield return null;
            }

            CloseLoadingCanvas();
        }
        #endregion
        
        CancellationTokenSource m_cancellationTokenSource;
        void SetLoadingText(string text) {
            if (!m_isLoading) return;
            m_loadingText.text = text;

            // Cancel the previous coroutine if it is still running
            m_cancellationTokenSource?.Cancel();
            m_cancellationTokenSource = new CancellationTokenSource();

            StartCoroutine(AnimateLoadingText(text, m_cancellationTokenSource.Token));
        }
        IEnumerator AnimateLoadingText(string text, CancellationToken token) {
            if (!m_isLoading) yield break;

            const float speed = 0.5f;
            const int maxDots = 5;
            var currentDots = 1;

            while (m_isLoading) {
                if (token.IsCancellationRequested) yield break;

                m_loadingText.text = text + new string('.', currentDots);

                currentDots++;
                if (currentDots > maxDots) {
                    currentDots = 1;
                }

                yield return new WaitForSeconds(speed);
            }
        }
        void OnSceneGroupCleanup() { 
            #if PROJECT_DEBUG
            Debug.Log("Scene group clean up started.");
            #endif
            UnloadBootstrapper();
        }
        static async void UnloadBootstrapper() {
            if (!SceneManager.GetSceneByName("Bootstrapper").isLoaded) return;
            await SceneManager.UnloadSceneAsync("Bootstrapper");
            #if PROJECT_DEBUG
            Debug.Log("Bootstrapper scene unloaded.");
            #endif
        }
    }
}