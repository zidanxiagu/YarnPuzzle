using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace YarnScapes.Editor
{
    public static class ProjectBuilder
    {
        [MenuItem("YarnScapes/Build Starter Scene")]
        public static void Build()
        {
            PlayerSettings.companyName = "YarnScapes Reconstruction";
            PlayerSettings.productName = "YarnScapes Offline Core";
            PlayerSettings.defaultScreenWidth = 540;
            PlayerSettings.defaultScreenHeight = 960;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.resizableWindow = true;
            PlayerSettings.runInBackground = true;
            PlayerSettings.allowedAutorotateToPortrait = true;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false;
            PlayerSettings.allowedAutorotateToLandscapeRight = false;
            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, "com.reconstruction.yarnscapes.offline");

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = new GameObject("YarnScapesRuntime");
            root.AddComponent<global::YarnScapesRuntime>();
            SceneManager.SetActiveScene(scene);
            if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
            {
                AssetDatabase.CreateFolder("Assets", "Scenes");
            }
            EditorSceneManager.SaveScene(scene, "Assets/Scenes/Bootstrap.unity");

            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene("Assets/Scenes/Bootstrap.unity", true)
            };
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("YarnScapes starter scene built.");
        }

        [MenuItem("YarnScapes/Build Windows Player")]
        public static void BuildWindowsPlayer()
        {
            Build();
            var output = "Builds/YarnScapesOffline.exe";
            var report = BuildPipeline.BuildPlayer(
                EditorBuildSettings.scenes,
                output,
                BuildTarget.StandaloneWindows64,
                BuildOptions.None);
            Debug.Log("YarnScapes Windows player result: " + report.summary.result + ", errors=" + report.summary.totalErrors);
            if (report.summary.totalErrors > 0)
            {
                throw new System.Exception("Windows player build failed with " + report.summary.totalErrors + " errors.");
            }
        }
    }
}
