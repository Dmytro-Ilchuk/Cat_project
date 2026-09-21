using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Profile;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace NineLives.Editor
{
    public static class StageOneValidation
    {
        [MenuItem("NineLives/Validate Stage 1")]
        public static void Validate()
        {
            Require(Application.unityVersion == "6000.6.1f1", "Use Unity 6000.6.1f1.");
            Require(PlayerSettings.productName == "NineLives", "Product name changed.");
            Require(EditorSettings.serializationMode == SerializationMode.ForceText, "Enable Force Text.");
            Require(VersionControlSettings.mode == "Visible Meta Files", "Enable Visible Meta Files.");
            Require(PlayerSettings.GetScriptingBackend(NamedBuildTarget.Standalone) == ScriptingImplementation.Mono2x, "Expected Mono backend.");
            var pipeline = GraphicsSettings.defaultRenderPipeline as UniversalRenderPipelineAsset;
            Require(pipeline != null && pipeline.scriptableRenderer.GetType().Name == "Renderer2D", "Expected URP 2D Renderer.");
            string[] scenes = { "Assets/_Game/Scenes/Bootstrap.unity", "Assets/_Game/Scenes/MainMenu.unity" };
            Require(EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).SequenceEqual(scenes), "Build scenes/order changed.");
            var profile = BuildProfile.GetAllBuildProfiles().Single(p => p.name == "Windows Development");
            Require(profile.GetScenesForBuild().Where(s => s.enabled).Select(s => s.path).SequenceEqual(scenes), "Profile scenes/order changed.");
            var input = AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/_Game/Settings/NineLives.inputactions");
            Require(input != null && InputSystem.actions == input, "Project input asset is not NineLives.");
            foreach (var action in new[] { "Move", "Jump", "Attack", "Dash", "Hiss", "Interact", "Pause" })
                Require(input.FindAction("Gameplay/" + action) != null, "Missing gameplay action: " + action);
            foreach (var action in new[] { "Navigate", "Submit", "Cancel", "Point", "Click" })
                Require(input.FindAction("UI/" + action) != null, "Missing UI action: " + action);

            string[] layers = { "Ground", "Player", "Enemy", "PlayerHitbox", "EnemyHitbox", "Interactable" };
            for (int i = 0; i < layers.Length; i++)
                Require(LayerMask.NameToLayer(layers[i]) == i + 8, "Incorrect layer: " + layers[i]);
            for (int a = 8; a <= 13; a++)
            for (int b = 0; b < 32; b++)
            {
                int low = Math.Min(a, b), high = Math.Max(a, b);
                bool collide = (low == 8 && (high == 9 || high == 10)) ||
                    (low == 9 && (high == 12 || high == 13)) || (low == 10 && high == 11);
                Require(Physics2D.GetIgnoreLayerCollision(a, b) != collide, "Incorrect 2D collision pair: " + a + "/" + b);
            }

            var menuScene = UnityEngine.SceneManagement.SceneManager.GetSceneByPath(scenes[1]);
            bool opened = !menuScene.isLoaded;
            if (opened)
            {
                Require(!EditorApplication.isPlaying, "Run from MainMenu in Play Mode, or stop Play Mode.");
                menuScene = EditorSceneManager.OpenScene(scenes[1], OpenSceneMode.Additive);
            }
            try
            {
                var roots = menuScene.GetRootGameObjects();
                var button = roots.SelectMany(r => r.GetComponentsInChildren<Button>(true)).Single();
                Require(button.onClick.GetPersistentEventCount() == 1 &&
                    button.onClick.GetPersistentMethodName(0) == nameof(MainMenu.Quit) &&
                    button.onClick.GetPersistentTarget(0) is MainMenu, "Quit button is not wired.");
                var events = roots.SelectMany(r => r.GetComponentsInChildren<EventSystem>(true)).Single();
                var module = events.GetComponent<InputSystemUIInputModule>();
                Require(module != null && module.actionsAsset == input && module.submit.action != null && module.leftClick.action != null,
                    "UI input is not wired.");
            }
            finally
            {
                if (opened) EditorSceneManager.CloseScene(menuScene, true);
            }
            Debug.Log("NineLives: Stage 1 validation passed.");
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
