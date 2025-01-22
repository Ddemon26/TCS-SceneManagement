using ImprovedTimers;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
//using DG.Tweening;

namespace TCS.Bootstrapper {
    public class LoadingScreen : MonoBehaviour {
        [SerializeField] Image m_backgroundOne;
        [SerializeField] Image m_backgroundTwo;
        [SerializeField] CanvasGroup m_canvasGroupOne;
        [SerializeField] CanvasGroup m_canvasGroupTwo;
        [SerializeField] TextMeshProUGUI m_tipTextTitle;
        [SerializeField] TextMeshProUGUI m_tipTextInfo;
        [SerializeField] float m_timePerCycle = 5f;
        [SerializeField] float m_tipTextCycleTime = 3f; // New field for tip text cycle time
        [SerializeField] LoadingScreenSetup m_loadingScreenSetup;
        [SerializeField] float m_fadeDuration = 1f;

        CountdownTimer m_backgroundTimer;
        CountdownTimer m_tipTextTimer; // New timer for tip text
        TipJsonData m_tipText;
        bool m_isFirstBackgroundActive = true;

        void Awake() {
            m_loadingScreenSetup.SetTipsArray();
            m_backgroundTimer = new CountdownTimer(m_timePerCycle);
            m_tipTextTimer = new CountdownTimer(m_tipTextCycleTime); // Initialize the new timer
            m_backgroundTimer.OnTimerStop += SetNewBackgroundCycle;
            m_tipTextTimer.OnTimerStop += SetNewTipTextCycle; // Set event handler for the new timer
        }

        void OnDestroy() {
            m_backgroundTimer.OnTimerStop -= SetNewBackgroundCycle;
            m_tipTextTimer.OnTimerStop -= SetNewTipTextCycle; // Remove event handler for the new timer
        }

        void Start() {
            SetNewBackgroundCycle();
            SetNewTipTextCycle(); // Start the tip text cycle
        }

        void SetNewBackgroundCycle() {
            SetBackgroundCycle();
            m_backgroundTimer.Start();
        }

        void SetNewTipTextCycle() {
            SetTipTextCycle();
            m_tipTextTimer.Start();
        }

        void SetBackgroundCycle() {
            if (m_isFirstBackgroundActive) {
                m_backgroundTwo.sprite = m_loadingScreenSetup.GetRandomBackground();
                FadeTransition(m_canvasGroupOne, m_canvasGroupTwo);
            } else {
                m_backgroundOne.sprite = m_loadingScreenSetup.GetRandomBackground();
                FadeTransition(m_canvasGroupTwo, m_canvasGroupOne);
            }

            m_isFirstBackgroundActive = !m_isFirstBackgroundActive;
        }

        void SetTipTextCycle() {
            m_tipText = m_loadingScreenSetup.GetRandomLoadingText();

            if (m_tipText == null) {
                m_tipTextTitle.text = "Default Title";
                m_tipTextInfo.text =
                    "a bunch of random info text should go here" +
                    "for some reason you did not provide any tips";
            } else {
                m_tipTextTitle.text = m_tipText.title;
                m_tipTextInfo.text = m_tipText.info;
            }
        }

        void FadeTransition(CanvasGroup from, CanvasGroup to) {
            //from.DOFade(0f, m_fadeDuration);
            //to.DOFade(1f, m_fadeDuration);
        }

        void Update() {
            if (m_backgroundTimer.IsRunning) {
                m_backgroundTimer.Tick();
            }
            if (m_tipTextTimer.IsRunning) {
                m_tipTextTimer.Tick();
            }
        }
    }
}