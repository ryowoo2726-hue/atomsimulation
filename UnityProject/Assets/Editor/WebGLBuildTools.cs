using System.IO;
using UnityEditor;
using UnityEngine;

public static class WebGLBuildTools
{
    private const string BuildPath = "../Builds/WebGL";

    [MenuItem("Build/Configure WebGL")]
    public static void ConfigureWebGL()
    {
        EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.WebGL, BuildTarget.WebGL);

        PlayerSettings.productName = "Atom Model Simulation";
        PlayerSettings.companyName = "Science Class";
        PlayerSettings.defaultScreenWidth = 1280;
        PlayerSettings.defaultScreenHeight = 720;
        PlayerSettings.runInBackground = true;

        PlayerSettings.WebGL.template = "APPLICATION:Default";
        PlayerSettings.WebGL.decompressionFallback = true;
        PlayerSettings.WebGL.dataCaching = true;
        PlayerSettings.WebGL.memorySize = 128;
        PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.None;

        Debug.Log("Configured WebGL settings for mobile/tablet web deployment.");
    }

    [MenuItem("Build/Build WebGL")]
    public static void BuildWebGL()
    {
        ConfigureWebGL();

        string absoluteBuildPath = Path.GetFullPath(Path.Combine(Application.dataPath, BuildPath));
        Directory.CreateDirectory(absoluteBuildPath);

        BuildPlayerOptions options = new()
        {
            scenes = new[] { "Assets/atom.unity" },
            locationPathName = absoluteBuildPath,
            target = BuildTarget.WebGL,
            options = BuildOptions.None
        };

        BuildPipeline.BuildPlayer(options);
    }
}
