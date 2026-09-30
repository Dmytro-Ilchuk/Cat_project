using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace NineLives.Editor
{
    public static class MusicSetup
    {
        private const string ClipPath = "Assets/_Game/Audio/SillyCat.mp3";
        private static readonly string[] Scenes =
        {
            "Assets/_Game/Scenes/MainMenu.unity",
            "Assets/_Game/Scenes/Run.unity"
        };

        [MenuItem("NineLives/Audio/Install Music")]
        public static void Install()
        {
            var original = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            string originalPath = original.path;
            if (original.isDirty)
                throw new InvalidOperationException("Save the open scene before installing music.");
            AssetDatabase.ImportAsset(ClipPath, ImportAssetOptions.ForceSynchronousImport);
            var importer = AssetImporter.GetAtPath(ClipPath) as AudioImporter;
            if (importer == null) throw new InvalidOperationException("Music file is missing.");
            var settings = importer.defaultSampleSettings;
            settings.loadType = AudioClipLoadType.Streaming;
            settings.compressionFormat = AudioCompressionFormat.Vorbis;
            settings.quality = .7f;
            importer.defaultSampleSettings = settings;
            importer.SaveAndReimport();
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(ClipPath);
            if (clip == null) throw new InvalidOperationException("Music import failed.");

            foreach (string scenePath in Scenes)
            {
                var scene = EditorSceneManager.OpenScene(scenePath);
                var music = UnityEngine.Object.FindAnyObjectByType<GameMusic>();
                if (music == null)
                {
                    var host = new GameObject("Game Music", typeof(AudioSource), typeof(GameMusic));
                    music = host.GetComponent<GameMusic>();
                }
                var source = music.GetComponent<AudioSource>();
                source.volume = .28f;
                source.loop = true;
                source.playOnAwake = false;
                source.spatialBlend = 0f;
                var data = new SerializedObject(music);
                data.FindProperty("music").objectReferenceValue = clip;
                data.ApplyModifiedPropertiesWithoutUndo();
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
            if (!string.IsNullOrEmpty(originalPath))
                EditorSceneManager.OpenScene(originalPath);
        }
    }
}
