using System;
using System.Linq;
using System.Threading.Tasks;
using Eflatun.SceneReference;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;

namespace TCS.Bootstrapper {
    /// <summary>
    /// Manages loading and unloading of a single scene (Regular or Addressable).
    /// </summary>
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
        public async Task LoadScene(SceneReference sceneRef, IProgress<float> progress, bool reloadIfLoaded = false) {
            // Check if the scene is already loaded
            if (IsSceneLoaded(sceneRef)) {
                if (!reloadIfLoaded) {
                    // Scene is already loaded, do nothing
                    return;
                }

                // Unload current scene first
                await UnloadScene();
            }

            // Before loading a new scene, clear any leftover handles
            m_handleGroup.Handles.Clear();

            // We'll track progress by either using AsyncOperation (for regular scenes)
            // or AsyncOperationHandle (for addressable scenes).
            var operationGroup = new AsyncOperationGroup(1);

            if (sceneRef.State == SceneReferenceState.Regular) {
                // Built-in scene
                var loadOp = SceneManager.LoadSceneAsync(sceneRef.Path, LoadSceneMode.Additive);

                // (Optional) Simulate long loading time - remove if you don't need it:
                await Task.Delay(TimeSpan.FromSeconds(10f));

                if (loadOp != null) {
                    operationGroup.Operations.Add(loadOp);
                }
            }
            else if (sceneRef.State == SceneReferenceState.Addressable) {
                // Addressable scene
                AsyncOperationHandle<SceneInstance> handle = Addressables.LoadSceneAsync(sceneRef.Path, LoadSceneMode.Additive);
                m_handleGroup.Handles.Add(handle);
            }

            // Wait until both the regular and addressable operations are done (if they exist)
            while (!operationGroup.IsDone || !m_handleGroup.IsDone) {
                // Combine progress from built-in and addressable in a naive way
                float combined = (operationGroup.Progress + m_handleGroup.Progress) / 2f;
                progress?.Report(combined);

                await Task.Delay(100);
            }

            // Try setting the newly loaded scene as active
            var loadedScene = SceneManager.GetSceneByName(sceneRef.Path);
            if (loadedScene.IsValid()) {
                SceneManager.SetActiveScene(loadedScene);
            }

            m_activeSceneName = sceneRef.Path;
            OnSceneLoaded.Invoke(m_activeSceneName);
        }

        /// <summary>
        /// Unloads the currently active scene if it exists.
        /// </summary>
        public async Task UnloadScene() {
            if (string.IsNullOrEmpty(m_activeSceneName)) {
                // No active scene to unload
                return;
            }

            // Unload if it's a regular (non-addressable) scene
            var current = SceneManager.GetSceneByName(m_activeSceneName);
            if (current.IsValid() && current.isLoaded) {
                var unloadOp = SceneManager.UnloadSceneAsync(m_activeSceneName);
                if (unloadOp != null) {
                    var opGroup = new AsyncOperationGroup(1);
                    opGroup.Operations.Add(unloadOp);

                    OnSceneUnloaded.Invoke(m_activeSceneName);

                    // Wait until the unload operation is finished
                    while (!opGroup.IsDone) {
                        await Task.Delay(100);
                    }
                }
            }

            // Unload if it's an addressable scene
            foreach (AsyncOperationHandle<SceneInstance> handle in m_handleGroup.Handles.Where(h => h.IsValid())) {
                // Ensure we're unloading the correct scene instance
                if (handle.Result.Scene.name.Equals(m_activeSceneName, StringComparison.Ordinal)) {
                    Addressables.UnloadSceneAsync(handle);
                }
            }

            // Clear the handle group since we're done
            m_handleGroup.Handles.Clear();

            // Remove the reference
            m_activeSceneName = null;

            // (Optional) Unload unused assets:
            // await Resources.UnloadUnusedAssets();
        }

        bool IsSceneLoaded(SceneReference sceneRef) {
            var scene = SceneManager.GetSceneByName(sceneRef.Path);
            return scene.IsValid() && scene.isLoaded;
        }
    }
}