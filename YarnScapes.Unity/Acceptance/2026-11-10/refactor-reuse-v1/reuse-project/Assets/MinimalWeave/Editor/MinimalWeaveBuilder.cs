using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace WeaveKit.Samples.Editor
{
    public static class MinimalWeaveBuilder
    {
        [MenuItem("WeaveKit/Create Sample Scene")]
        public static void CreateScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            new GameObject("MinimalWeaveDemo", typeof(MinimalWeaveDemo));
            EditorSceneManager.SaveScene(scene, "Assets/MinimalWeave.unity");
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene("Assets/MinimalWeave.unity", true) };
        }
        public static void Build()
        {
            CreateScene();
            PlayerSettings.companyName = "WeaveKit"; PlayerSettings.productName = "WeaveKit Independent Sample";
            PlayerSettings.defaultScreenWidth = 640; PlayerSettings.defaultScreenHeight = 800;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed; PlayerSettings.runInBackground = true;
            var report = BuildPipeline.BuildPlayer(EditorBuildSettings.scenes, "Builds/WeaveKitSample.exe", BuildTarget.StandaloneWindows64, BuildOptions.None);
            if (report.summary.result != BuildResult.Succeeded) throw new Exception("Standalone sample build failed.");
            Debug.Log("WEAVEKIT_STANDALONE_BUILD PASS errors=" + report.summary.totalErrors);
        }
    }
}
