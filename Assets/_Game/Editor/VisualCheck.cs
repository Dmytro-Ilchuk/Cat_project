using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace NineLives.Editor
{
    public static class VisualCheck
    {
        [MenuItem("NineLives/Check Visuals")]
        public static void Validate()
        {
            var cat = AssetDatabase.LoadAllAssetsAtPath("Assets/_Game/Art/Characters/CatAnimations.png").OfType<Sprite>().ToArray();
            var rat = AssetDatabase.LoadAllAssetsAtPath("Assets/_Game/Art/Characters/RatAnimations.png").OfType<Sprite>().ToArray();
            Check(cat.Length == 48 && rat.Length == 24, "sprite sheet frame counts");
            foreach (var row in new[] { "Idle", "Walk", "Run", "Jump", "Attack", "Dash", "Hurt", "Death" })
                Check(cat.Count(s => s.name.StartsWith(row + "_")) == 6, "cat " + row);
            foreach (var row in new[] { "Idle", "Run", "Attack", "Death" })
                Check(rat.Count(s => s.name.StartsWith(row + "_")) == 6, "rat " + row);
            var letters = "АБВГҐДЕЄЖЗИІЇЙКЛМНОПРСТУФХЦЧШЩЬЮЯабвгґдеєжзиіїйклмнопрстуфхцчшщьюя";
            foreach (var name in new[] { "Tiny5-Regular", "PressStart2P-Regular" })
            {
                var font = AssetDatabase.LoadAssetAtPath<Font>("Assets/_Game/Art/Fonts/" + name + ".ttf");
                Check(font != null && letters.All(font.HasCharacter), name + " Ukrainian glyphs");
            }
            var fx = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Game/Resources/CombatFx.mat");
            Check(fx != null && fx.shader != null && fx.shader.isSupported, "combat shader");
            var background = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_Game/Art/Menu/MainMenuBackground.png");
            Check(background != null, "menu background");
            var menuScene = EditorSceneManager.OpenPreviewScene("Assets/_Game/Scenes/MainMenu.unity");
            try
            {
                var menu = menuScene.GetRootGameObjects().SelectMany(o => o.GetComponentsInChildren<MainMenu>(true)).Single();
                var rain = menu.GetComponentInChildren<MenuRain>(true);
                var image = menu.GetComponentsInChildren<RawImage>(true).Single(i => i.name == "Night city");
                Check(rain != null && rain.GetComponent<CanvasRenderer>() != null && image.texture == background, "menu art and rain");
                Check(menu.GetComponentsInChildren<Button>(true).Count(b => b.onClick.GetPersistentEventCount() > 0) >= 7, "menu buttons wired");
            }
            finally { EditorSceneManager.ClosePreviewScene(menuScene); }
            var arena = EditorSceneManager.OpenPreviewScene("Assets/_Game/Scenes/Tests/PlatformTest.unity");
            try
            {
                var roots = arena.GetRootGameObjects();
                Check(roots.SelectMany(o => o.GetComponentsInChildren<RatController>(true)).Count(r => r.enabled) == 2, "rat AI enabled");
                var player = roots.SelectMany(o => o.GetComponentsInChildren<PlayerController>(true)).Single();
                var visual = player.transform.Find("Visual");
                var animator = visual.GetComponent<CharacterSpriteAnimator>();
                Check(animator != null && animator.enabled && visual.GetComponent<SpriteRenderer>().sprite != null, "cat animated");
            }
            finally { EditorSceneManager.ClosePreviewScene(arena); }
            Debug.Log("Visuals PASS: animation frames, font glyphs, combat material, menu and arena.");
        }

        private static void Check(bool valid, string item)
        {
            if (!valid) throw new InvalidOperationException("Visuals: " + item);
        }
    }
}
