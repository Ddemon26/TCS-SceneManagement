using System;
using ImprovedTimers;
using UnityEngine;
namespace TCS.Bootstrapper {
    public class TrollLoadingMemes : MonoBehaviour {
        [SerializeField] float m_minTimePerCycle = 0.5f;
        [SerializeField] float m_maxTimePerCycle = 3f;
        
        [SerializeField] string[] m_loadingMemes;
        CountdownTimer m_timer;
        SceneLoader m_sceneLoader;

        void Awake() {
            m_sceneLoader = FindFirstObjectByType<SceneLoader>(FindObjectsInactive.Include);
            if (m_loadingMemes.Length == 0) {
                const string message = "Getting Help";
                m_loadingMemes = new[] { message };
                return;
            }
            m_timer = new CountdownTimer(0.1f);
            m_timer.Start();
            m_timer.OnTimerStop += SetNewCycle;
        }
        
        void OnDestroy() {
            m_timer.OnTimerStop -= SetNewCycle;
        }

        void Update() {
            if (m_timer.IsRunning) {
                m_timer.Tick();
            }
        }
        
        void SetNewCycle() {
            if (m_sceneLoader.IsFinishing) return;
            SetCycle();
            m_timer.Start();
        }
        
        void SetCycle() {
            float randomTime = UnityEngine.Random.Range(m_minTimePerCycle, m_maxTimePerCycle);
            m_timer.Stop();
            m_timer.Reset(randomTime);
            
            int randomIndex = UnityEngine.Random.Range(0, m_loadingMemes.Length);
            SceneEvents.OnLoadInfo.Invoke(m_loadingMemes[randomIndex]);
        }
    }
}