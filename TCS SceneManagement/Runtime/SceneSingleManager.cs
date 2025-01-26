using System;
using System.Linq;
using System.Threading.Tasks;
using Eflatun.SceneReference;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;

namespace TCS.SceneManagement {
    public class SceneSingleManager {
        public event Action<string> OnSceneLoaded = delegate { };
        public event Action<string> OnSceneUnloaded = delegate { };

        // Use this group to track addressable scene handles (in case you need to unload).
        readonly AsyncOperationHandleGroup m_handleGroup = new(1);

        string m_activeSceneName;

        /// <summary>
        /// Loads a single scene (Regular or Addressable).
        /// If it's already loaded and <paramref name="reloadIfLoaded"/> is false, it skips reloading.
        /// Otherwise, it unloads the current scene before loading again.
        /// </summary>
        /// <param name="sceneRef">Scene reference to load.</param>
        /// <param name="progress">Progress object for reporting loading progress (0..1).</param>
        /// <param name="reloadIfLoaded">Whether to unload/reload if this scene is already loaded.</param>
        public async Task LoadSceneAsync(SceneReference sceneRef, IProgress<float> progress, bool reloadIfLoaded = false) {
            // Check if the scene is already loaded
            if (IsSceneLoaded(sceneRef)) {
                if (!reloadIfLoaded) {
                    // Scene is already loaded, do nothing
                    return;
                }
            }

            // Before loading a new scene, clear any leftover handles
            m_handleGroup.Handles.Clear();

            var operationGroup = new AsyncOperationGroup(1);

            if (sceneRef.State == SceneReferenceState.Regular) {
                // Built-in scene
                var loadOp = SceneManager.LoadSceneAsync(sceneRef.Name, LoadSceneMode.Additive);

                // (Optional) Simulate long loading time
                await Task.Delay(TimeSpan.FromSeconds(1f));

                if (loadOp != null) {
                    operationGroup.Operations.Add(loadOp);
                }
            }
            else if (sceneRef.State == SceneReferenceState.Addressable) {
                // Addressable scene
                AsyncOperationHandle<SceneInstance> handle =
                    Addressables.LoadSceneAsync(sceneRef.Path, LoadSceneMode.Additive);
                m_handleGroup.Handles.Add(handle);
            }

            // Wait until both the regular and addressable operations are done (if they exist)
            while (!operationGroup.IsDone || !m_handleGroup.IsDone) {
                float combined = (operationGroup.Progress + m_handleGroup.Progress) / 2f;
                progress?.Report(combined);

                await Task.Delay(100);
            }

            // Try setting the newly loaded scene as active
            var loadedScene = SceneManager.GetSceneByName(sceneRef.Name);
            if (loadedScene.IsValid()) {
                SceneManager.SetActiveScene(loadedScene);
            }

            m_activeSceneName = sceneRef.Name;
            OnSceneLoaded.Invoke(m_activeSceneName);
        }

        /// <summary>
        /// Unloads the given scene (Regular or Addressable) using its SceneReference.
        /// </summary>
        /// <param name="sceneRef">Reference to the scene to unload.</param>
        public async Task UnloadSceneAsync(SceneReference sceneRef) {
            // Check if the target scene is valid and loaded
            var scene = SceneManager.GetSceneByName(sceneRef.Name);
            if (!scene.IsValid() || !scene.isLoaded) {
                Debug.LogWarning($"Scene '{sceneRef.Name}' is not loaded.");
                // Scene not loaded, do nothing or optionally log a warning
                return;
            }

            if (sceneRef.State == SceneReferenceState.Regular) {
                // Unload a built-in scene
                var unloadOp = SceneManager.UnloadSceneAsync(sceneRef.Name);
                if (unloadOp != null) {
                    var opGroup = new AsyncOperationGroup(1);
                    opGroup.Operations.Add(unloadOp);

                    // Wait for unload to finish
                    while (!opGroup.IsDone) {
                        await Task.Delay(100);
                    }
                }
            }
            else if (sceneRef.State == SceneReferenceState.Addressable) {
                // Unload an addressable scene
                foreach (AsyncOperationHandle<SceneInstance> handle in m_handleGroup.Handles.Where(h => h.IsValid())) {
                    if (handle.Result.Scene.name.Equals(sceneRef.Name, StringComparison.Ordinal)) {
                        // Wait for the scene to unload properly
                        await Addressables.UnloadSceneAsync(handle).Task;
                        break;
                    }
                }
            }

            // Fire the unload event
            OnSceneUnloaded.Invoke(sceneRef.Name);

            // Clear the handle group since we're done with this scene
            m_handleGroup.Handles.Clear();

            // If the scene we unloaded was also stored as "active" in the manager, clear it
            if (m_activeSceneName == sceneRef.Name) {
                m_activeSceneName = null;
            }

            // (Optional) Unload unused assets to free memory
            // await Resources.UnloadUnusedAssets();
        }

        static bool IsSceneLoaded(SceneReference sceneRef) {
            var scene = SceneManager.GetSceneByName(sceneRef.Name);
            return scene.IsValid() && scene.isLoaded;
        }
    }
}