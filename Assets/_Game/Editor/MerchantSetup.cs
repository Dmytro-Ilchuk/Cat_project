using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace NineLives.Editor
{
    public static class MerchantSetup
    {
        private const string TexturePath = "Assets/_Game/Art/Characters/MerchantCat.png";
        private const string MaterialPath = "Assets/_Game/Art/Characters/MerchantCatLit.mat";
        private const string PrefabPath = "Assets/_Game/Prefabs/MerchantCat.prefab";
        private const string RunScene = "Assets/_Game/Scenes/Run.unity";

        [MenuItem("NineLives/Stage 5/Install Merchant Cat")]
        public static void Install()
        {
            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().path != RunScene)
                throw new InvalidOperationException("Open Run scene before installing the merchant.");

            AssetDatabase.ImportAsset(TexturePath, ImportAssetOptions.ForceSynchronousImport);
            var importer = AssetImporter.GetAtPath(TexturePath) as TextureImporter;
            if (importer == null) throw new InvalidOperationException("Merchant texture is missing.");
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 480f;
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteAlignment = (int)SpriteAlignment.Custom;
            settings.spritePivot = new Vector2(.5f, .088f);
            importer.SetTextureSettings(settings);
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = 2048;
            importer.SaveAndReimport();

            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(TexturePath);
            if (sprite == null) throw new InvalidOperationException("Merchant sprite import failed.");
            var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (material == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Lit-Default");
                if (shader == null) throw new InvalidOperationException("2D sprite lighting shader is missing.");
                material = new Material(shader) { name = "Merchant Cat Lit" };
                AssetDatabase.CreateAsset(material, MaterialPath);
            }

            var root = new GameObject("Merchant Cat", typeof(SpriteRenderer));
            try
            {
                var renderer = root.GetComponent<SpriteRenderer>();
                renderer.sprite = sprite;
                renderer.sharedMaterial = material;
                renderer.sortingOrder = 9;

                var lantern = new GameObject("Merchant Lantern", typeof(Light2D), typeof(WorldLightFlicker));
                lantern.transform.SetParent(root.transform, false);
                lantern.transform.localPosition = new Vector3(-.66f, 1.1f);
                var light = lantern.GetComponent<Light2D>();
                light.lightType = Light2D.LightType.Point;
                light.color = new Color(1f, .65f, .29f);
                light.intensity = .55f;
                light.pointLightInnerRadius = .35f;
                light.pointLightOuterRadius = 2.7f;

                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            var director = UnityEngine.Object.FindAnyObjectByType<RunDirector>();
            if (director == null || prefab == null)
                throw new InvalidOperationException("Run Director or merchant prefab is missing.");
            var data = new SerializedObject(director);
            data.FindProperty("merchantPrefab").objectReferenceValue = prefab;
            data.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();
        }
    }
}
