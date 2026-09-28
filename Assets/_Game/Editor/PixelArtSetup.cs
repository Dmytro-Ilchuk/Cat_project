using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace NineLives.Editor
{
    public static class PixelArtSetup
    {
        private const string Art = "Assets/_Game/Art/Pixel/";

        [MenuItem("NineLives/Art/Apply pixel-art budget")]
        public static void Apply()
        {
            Bake("GroundPropsSimple.png", "GroundProps32.png", 64, 64);
            Bake("ModernUtilityPolesSource.png", "ModernUtilityPoles128.png", 128, 128);
            Bake("WetFloorSimple.png", "WetFloor64.png", 64, 64);
            Bake("NightSkySimple.png", "NightSky256.png", 256, 128);
            Bake("StreetFoundationSimple.png", "StreetFoundation64.png", 64, 64);
            Bake("StreetLitterSimple.png", "StreetLitter64.png", 64, 64);
            Bake("UpperPropsSimple.png", "UpperProps64.png", 64, 64);
            Bake("NeonLampsGraphite.png", "NeonLamps128.png", 128, 128);
            Bake("SupportedCatwalkSimple.png", "SupportedCatwalk128.png", 128, 43);
            Bake("HangingLedgeSimple.png", "HangingLedge64.png", 64, 21);
            Bake("SpikeStripSimple.png", "SpikeStrip64.png", 64, 21);
            Bake("CityNearSimple.png", "CityNear512.png", 512, 171);
            Bake("CityMidSimple.png", "CityMid256.png", 256, 86);
            Bake("CityMidAlternateSimple.png", "CityMidAlternate256.png", 256, 85);
            Bake("CityFarSimple.png", "CityFar128.png", 128, 43);
            Import("GroundProps32.png", 64, 100, true, false);
            Import("ModernUtilityPoles128.png", 128, 100, true, false);
            Import("WetFloor64.png", 64, 16, false, true);
            Import("NightSky256.png", 256, 16, false, true);
            Import("StreetFoundation64.png", 64, 16, false, true);
            Import("StreetLitter64.png", 64, 100, true, false);
            Import("NeonLamps128.png", 128, 100, true, false);
            Import("WindDebris64.png", 64, 100, true, false);
            Import("UpperProps64.png", 64, 100, true, false);
            Import("SupportedCatwalk128.png", 128, 128f / 21.72f, false, true);
            Import("HangingLedge64.png", 64, 100, true, false);
            Import("SpikeStrip64.png", 64, 100, true, false);
            Import("CityNear512.png", 512, 512f / 21.72f, false, true);
            Import("CityMid256.png", 256, 256f / 21.72f, false, true);
            Import("CityMidAlternate256.png", 256, 256f / 21.72f, false, true);
            Import("CityFar128.png", 128, 128f / 21.72f, false, true);
            Import("CityUtilityProps128.png", 128, 100, true, false);
            Import("CityFrontShadow128.png", 128, 16, false, true, FilterMode.Bilinear);

            foreach (string path in new[] { "Assets/_Game/Scenes/Run.unity", "Assets/_Game/Scenes/Tests/RoomLab.unity" })
            {
                var scene = EditorSceneManager.OpenScene(path);
                var art = Object.FindAnyObjectByType<RoomEnvironmentArt>();
                var data = new SerializedObject(art);
                data.FindProperty("wetStreetFloor").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>(Art + "WetFloor64.png");
                data.FindProperty("streetFoundation").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>(Art + "StreetFoundation64.png");
                data.FindProperty("streetLitter").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Texture2D>(Art + "StreetLitter64.png");
                data.FindProperty("streetProps").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Texture2D>(Art + "GroundProps32.png");
                data.FindProperty("warmLamps").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Texture2D>(Art + "ModernUtilityPoles128.png");
                data.FindProperty("neonLamps").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Texture2D>(Art + "ModernUtilityPoles128.png");
                data.FindProperty("windDebris").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Texture2D>(Art + "WindDebris64.png");
                data.FindProperty("overheadProps").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Texture2D>(Art + "UpperProps64.png");
                data.FindProperty("infrastructureProps").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Texture2D>(Art + "CityUtilityProps128.png");
                data.FindProperty("supportedPlatform").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>(Art + "SupportedCatwalk128.png");
                data.FindProperty("hangingPlatform").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Texture2D>(Art + "HangingLedge64.png");
                data.FindProperty("spikeStrip").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Texture2D>(Art + "SpikeStrip64.png");
                data.ApplyModifiedPropertiesWithoutUndo();
                var parallax = Object.FindAnyObjectByType<RoomParallax>();
                var layers = new SerializedObject(parallax);
                layers.FindProperty("distantCity").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>(Art + "CityFar128.png");
                layers.FindProperty("buildings").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>(Art + "CityMid256.png");
                layers.FindProperty("alternateBuildings").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>(Art + "CityMidAlternate256.png");
                layers.FindProperty("foreground").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>(Art + "CityNear512.png");
                layers.FindProperty("nightSky").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>(Art + "NightSky256.png");
                layers.FindProperty("lowerForeground").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>(Art + "CityFrontShadow128.png");
                layers.ApplyModifiedPropertiesWithoutUndo();
                if (parallax.GetComponent<RoomAtmosphere>() == null)
                    parallax.gameObject.AddComponent<RoomAtmosphere>();
                EditorSceneManager.SaveScene(scene);
            }
            Check();
        }

        [MenuItem("NineLives/Art/Check pixel-art budget")]
        public static void Check()
        {
            foreach (var (name, width, height) in new[] {
                ("GroundProps32.png", 64, 64), ("ModernUtilityPoles128.png", 128, 128),
                ("WetFloor64.png", 64, 64), ("NeonLamps128.png", 128, 128),
                ("WindDebris64.png", 64, 64),
                ("NightSky256.png", 256, 128), ("StreetFoundation64.png", 64, 64),
                ("StreetLitter64.png", 64, 64),
                ("UpperProps64.png", 64, 64), ("SupportedCatwalk128.png", 128, 43),
                ("HangingLedge64.png", 64, 21), ("SpikeStrip64.png", 64, 21),
                ("CityNear512.png", 512, 171), ("CityMid256.png", 256, 86),
                ("CityMidAlternate256.png", 256, 85), ("CityFar128.png", 128, 43),
                ("CityUtilityProps128.png", 128, 128), ("CityFrontShadow128.png", 128, 64) })
            {
                var source = new Texture2D(2, 2);
                source.LoadImage(File.ReadAllBytes(Art + name));
                if (source.width != width || source.height != height)
                    throw new System.InvalidOperationException(name + " exceeds source PNG pixel budget.");
                Object.DestroyImmediate(source);
                var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(Art + name);
                FilterMode expectedFilter = name == "CityFrontShadow128.png" ? FilterMode.Bilinear : FilterMode.Point;
                if (texture == null || texture.filterMode != expectedFilter ||
                    texture.width != width || texture.height != height)
                    throw new System.InvalidOperationException(name + " has invalid Unity import settings.");
            }
            Debug.Log("Pixel-art environment budget PASS.");
        }

        internal static void Bake(string sourceName, string outputName, int width, int height)
        {
            var source = new Texture2D(2, 2);
            source.LoadImage(File.ReadAllBytes(Art + sourceName));
            var pixels = source.GetPixels32();
            var reduced = new Color32[width * height];
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                {
                    int sx = Mathf.FloorToInt((x + .5f) * source.width / width);
                    int sy = Mathf.FloorToInt((y + .5f) * source.height / height);
                    var color = pixels[sy * source.width + sx];
                    color.a = color.a < 128 ? (byte)0 : (byte)255;
                    reduced[y * width + x] = color;
                }
            var output = new Texture2D(width, height, TextureFormat.RGBA32, false);
            output.SetPixels32(reduced);
            output.Apply();
            File.WriteAllBytes(Art + outputName, output.EncodeToPNG());
            Object.DestroyImmediate(source);
            Object.DestroyImmediate(output);
        }

        internal static void Import(string name, int limit, float ppu, bool readable, bool sprite,
            FilterMode filter = FilterMode.Point)
        {
            string path = Art + name;
            AssetDatabase.ImportAsset(path);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = sprite ? TextureImporterType.Sprite : TextureImporterType.Default;
            importer.filterMode = filter;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = limit;
            importer.isReadable = readable;
            if (sprite)
            {
                importer.spritePixelsPerUnit = ppu;
                var settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);
                settings.spriteMeshType = SpriteMeshType.FullRect;
                importer.SetTextureSettings(settings);
            }
            importer.SaveAndReimport();
        }
    }
}
