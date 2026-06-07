using System.IO;
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class SimulationProjectSetup
{
    public static void PrepareHeliumScene()
    {
        const string scenePath = "Assets/atom.unity";

        if (!File.Exists(scenePath))
        {
            Debug.LogError($"Scene not found: {scenePath}");
            return;
        }

        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

        EnsureCamera();
        EnsureDirectionalLight();
        EnsureHeliumController();
        RemoveColliderComponents();

        EditorSceneManager.SaveScene(scene);

        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(scenePath, true)
        };

        AssetDatabase.SaveAssets();
    }

    public static void CreateDemoScene()
    {
        const string scenePath = "Assets/Scenes/SimulationDemo.unity";
        Directory.CreateDirectory("Assets/Scenes");
        Directory.CreateDirectory("Assets/Materials");

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        GameObject cameraObject = new("Main Camera");
        var camera = cameraObject.AddComponent<Camera>();
        cameraObject.tag = "MainCamera";
        cameraObject.transform.position = new Vector3(0.0f, 4.0f, -10.0f);
        cameraObject.transform.rotation = Quaternion.Euler(20.0f, 0.0f, 0.0f);
        camera.clearFlags = CameraClearFlags.Skybox;

        GameObject lightObject = new("Directional Light");
        var light = lightObject.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.2f;
        lightObject.transform.rotation = Quaternion.Euler(50.0f, -30.0f, 0.0f);

        GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
        floor.name = "Floor";
        floor.transform.localScale = new Vector3(1.4f, 1.0f, 1.4f);

        Material particleMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Particle.mat");
        if (particleMaterial == null)
        {
            particleMaterial = new Material(Shader.Find("Standard"))
            {
                color = new Color(0.105f, 0.56f, 0.86f, 1.0f)
            };
            AssetDatabase.CreateAsset(particleMaterial, "Assets/Materials/Particle.mat");
        }

        GameObject controller = new("SimulationController");
        var simulationController = controller.AddComponent<SimulationController>();

        SerializedObject serializedController = new(simulationController);
        serializedController.FindProperty("particleCount").intValue = 24;
        serializedController.FindProperty("particleMaterial").objectReferenceValue = particleMaterial;
        serializedController.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.SaveScene(scene, scenePath);

        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(scenePath, true)
        };

        AssetDatabase.SaveAssets();
    }

    private static void EnsureCamera()
    {
        GameObject cameraObject = GameObject.Find("Main Camera");
        if (cameraObject == null)
        {
            cameraObject = new GameObject("Main Camera");
        }

        if (!cameraObject.TryGetComponent(out Camera camera))
        {
            camera = cameraObject.AddComponent<Camera>();
        }

        cameraObject.tag = "MainCamera";
        cameraObject.transform.position = new Vector3(0.0f, 4.2f, -10.0f);
        cameraObject.transform.rotation = Quaternion.Euler(22.0f, 0.0f, 0.0f);
        camera.fieldOfView = 50.0f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = Color.black;
    }

    private static void EnsureDirectionalLight()
    {
        GameObject lightObject = GameObject.Find("Directional Light");
        if (lightObject == null)
        {
            lightObject = new GameObject("Directional Light");
        }

        if (!lightObject.TryGetComponent(out Light light))
        {
            light = lightObject.AddComponent<Light>();
        }

        light.type = LightType.Directional;
        light.intensity = 1.4f;
        lightObject.transform.rotation = Quaternion.Euler(50.0f, -35.0f, 0.0f);
    }

    private static void EnsureHeliumController()
    {
        GameObject controllerObject = GameObject.Find("HeliumAtomSimulation");
        if (controllerObject == null)
        {
            controllerObject = new GameObject("HeliumAtomSimulation");
        }

        MonoScript simulationScript = AssetDatabase.LoadAssetAtPath<MonoScript>("Assets/Scripts/HeliumAtomSimulation.cs");
        Type simulationType = simulationScript != null ? simulationScript.GetClass() : null;
        if (simulationType == null)
        {
            Debug.LogWarning("HeliumAtomSimulation script is not compiled yet. It will bootstrap itself at Play time.");
            return;
        }

        if (controllerObject.GetComponent(simulationType) == null)
        {
            controllerObject.AddComponent(simulationType);
        }

        SerializedObject serializedController = new(controllerObject.GetComponent(simulationType));
        serializedController.FindProperty("cameraDistance").floatValue = 10.0f;
        serializedController.FindProperty("minCameraDistance").floatValue = 4.0f;
        serializedController.FindProperty("maxCameraDistance").floatValue = 18.0f;
        serializedController.FindProperty("orbitSensitivity").floatValue = 180.0f;
        serializedController.FindProperty("zoomSensitivity").floatValue = 3.0f;
        serializedController.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void RemoveColliderComponents()
    {
        foreach (GameObject sceneObject in UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
        {
            foreach (Component component in sceneObject.GetComponents<Component>())
            {
                if (component == null || !component.GetType().Name.Contains("Collider"))
                {
                    continue;
                }

                UnityEngine.Object.DestroyImmediate(component);
            }
        }
    }
}
