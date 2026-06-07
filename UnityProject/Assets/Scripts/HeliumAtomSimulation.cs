using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class HeliumAtomSimulation : MonoBehaviour
{
    private const string RuntimeRootName = "Atom Model Runtime";

    private static readonly ElementData[] Elements =
    {
        new(1, "H", "수소", 1, 1, 0),
        new(2, "He", "헬륨", 2, 2, 2),
        new(3, "Li", "리튬", 3, 3, 4),
        new(4, "Be", "베릴륨", 4, 4, 5),
        new(5, "B", "붕소", 5, 5, 6),
        new(6, "C", "탄소", 6, 6, 6),
        new(7, "N", "질소", 7, 7, 7),
        new(8, "O", "산소", 8, 8, 8),
        new(9, "F", "플루오린", 9, 9, 10),
        new(10, "Ne", "네온", 10, 10, 10),
        new(11, "Na", "나트륨", 11, 11, 12),
        new(12, "Mg", "마그네슘", 12, 12, 12),
        new(13, "Al", "알루미늄", 13, 13, 14),
        new(14, "Si", "규소", 14, 14, 14),
        new(15, "P", "인", 15, 15, 16),
        new(16, "S", "황", 16, 16, 16),
        new(17, "Cl", "염소", 17, 17, 18),
        new(18, "Ar", "아르곤", 18, 18, 22),
        new(19, "K", "칼륨", 19, 19, 20),
        new(20, "Ca", "칼슘", 20, 20, 20),
    };

    [Header("Templates")]
    [SerializeField] private GameObject protonTemplate;
    [SerializeField] private GameObject neutronTemplate;
    [SerializeField] private GameObject electronTemplate;

    [Header("Motion")]
    [SerializeField] private float firstShellRadius = 2.7f;
    [SerializeField] private float shellSpacing = 1.45f;
    [SerializeField] private float electronOrbitSpeed = 90.0f;
    [SerializeField] private float nucleusSpinSpeed = 18.0f;
    [SerializeField] private float nucleusHideRadius = 0.7f;

    [Header("Camera")]
    [SerializeField] private float cameraDistance = 10.0f;
    [SerializeField] private float minCameraDistance = 4.0f;
    [SerializeField] private float maxCameraDistance = 18.0f;
    [SerializeField] private float orbitSensitivity = 180.0f;
    [SerializeField] private float zoomSensitivity = 3.0f;
    [SerializeField] private float touchOrbitSensitivity = 0.22f;
    [SerializeField] private float touchZoomSensitivity = 0.02f;

    private Transform runtimeRoot;
    private Transform nucleusRoot;
    private Transform electronRoot;
    private Transform[] electrons = Array.Empty<Transform>();
    private ElectronMotion[] electronMotions = Array.Empty<ElectronMotion>();
    private Camera orbitCamera;
    private float cameraYaw;
    private float cameraPitch = 22.0f;
    private float orbitAngle;
    private int selectedElementIndex = 1;
    private int pendingElementIndex = -1;
    private GUIStyle panelStyle;
    private GUIStyle selectedButtonStyle;
    private float previousPinchDistance;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.name.Equals("atom", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (FindFirstObjectByType<HeliumAtomSimulation>() != null)
        {
            return;
        }

        GameObject controller = new("HeliumAtomSimulation");
        controller.AddComponent<HeliumAtomSimulation>();
    }

    private void Awake()
    {
        protonTemplate = protonTemplate != null ? protonTemplate : GameObject.Find("nucleus");
        neutronTemplate = neutronTemplate != null ? neutronTemplate : GameObject.Find("neutron");
        electronTemplate = electronTemplate != null ? electronTemplate : GameObject.Find("electron");

        if (protonTemplate == null || neutronTemplate == null || electronTemplate == null)
        {
            Debug.LogError("Atom simulation needs objects named nucleus, neutron, and electron.");
            enabled = false;
            return;
        }

        EnsureCameraAndLight();
        BuildAtom(Elements[selectedElementIndex]);
        PositionCamera();
    }

    private void Update()
    {
        ApplyPendingElementSelection();

        if (nucleusRoot == null)
        {
            return;
        }

        nucleusRoot.Rotate(Vector3.up, nucleusSpinSpeed * Time.deltaTime, Space.World);

        orbitAngle += electronOrbitSpeed * Time.deltaTime;
        UpdateElectronPositions();
        UpdateCameraControls();
        UpdateTouchControls();
    }

    private void ApplyPendingElementSelection()
    {
        if (pendingElementIndex < 0)
        {
            return;
        }

        int nextElementIndex = pendingElementIndex;
        pendingElementIndex = -1;

        if (nextElementIndex == selectedElementIndex)
        {
            return;
        }

        selectedElementIndex = nextElementIndex;
        BuildAtom(Elements[selectedElementIndex]);
    }

    private void OnGUI()
    {
        InitializeGuiStyles();

        ElementData selected = Elements[selectedElementIndex];
        float scale = Mathf.Clamp(Mathf.Min(Screen.width / 900.0f, Screen.height / 650.0f), 0.78f, 1.2f);
        Matrix4x4 previousMatrix = GUI.matrix;
        GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(scale, scale, 1.0f));
        float scaledWidth = Screen.width / scale;
        float scaledHeight = Screen.height / scale;
        float panelWidth = scaledWidth < 520.0f ? scaledWidth - 24.0f : 230.0f;
        float panelHeight = Mathf.Min(scaledHeight - 24.0f, 430.0f);

        GUI.Box(new Rect(12.0f, 12.0f, panelWidth, panelHeight), GUIContent.none, panelStyle);
        float x = 24.0f;
        float y = 24.0f;
        float contentWidth = panelWidth - 24.0f;

        GUI.Label(new Rect(x, y, contentWidth, 24.0f), $"{selected.AtomicNumber}. {selected.Symbol}  {selected.Name}");
        y += 30.0f;
        GUI.Label(new Rect(x, y, contentWidth, 22.0f), $"양성자: {selected.Protons}");
        y += 22.0f;
        GUI.Label(new Rect(x, y, contentWidth, 22.0f), $"중성자: {selected.Neutrons}");
        y += 22.0f;
        GUI.Label(new Rect(x, y, contentWidth, 22.0f), $"전자: {selected.Electrons}");
        y += 22.0f;
        GUI.Label(new Rect(x, y, contentWidth, 22.0f), $"전자 배치: {FormatShells(GetShellCounts(selected.Electrons))}");
        y += 34.0f;

        DrawElementButtons(x, y, contentWidth);

        GUI.matrix = previousMatrix;
    }

    private void DrawElementButtons(float x, float y, float contentWidth)
    {
        float gap = 6.0f;
        float buttonWidth = (contentWidth - gap) * 0.5f;
        float buttonHeight = 28.0f;

        for (int row = 0; row < 10; row++)
        {
            for (int column = 0; column < 2; column++)
            {
                int index = row * 2 + column;
                ElementData element = Elements[index];
                Rect rect = new(
                    x + column * (buttonWidth + gap),
                    y + row * (buttonHeight + gap),
                    buttonWidth,
                    buttonHeight);

                GUIStyle style = index == selectedElementIndex ? selectedButtonStyle : GUI.skin.button;
                if (GUI.Button(rect, $"{element.AtomicNumber} {element.Symbol}", style))
                {
                    pendingElementIndex = index;
                }
            }
        }
    }

    private void BuildAtom(ElementData element)
    {
        GameObject previousRuntimeRoot = GameObject.Find(RuntimeRootName);
        if (previousRuntimeRoot != null)
        {
            Destroy(previousRuntimeRoot);
        }

        runtimeRoot = new GameObject(RuntimeRootName).transform;
        runtimeRoot.position = Vector3.zero;

        nucleusRoot = new GameObject($"Nucleus - {element.Symbol}").transform;
        nucleusRoot.SetParent(runtimeRoot, false);

        protonTemplate.SetActive(false);
        neutronTemplate.SetActive(false);
        electronTemplate.SetActive(false);

        BuildNucleus(element);
        BuildElectronShells(element);
    }

    private void BuildNucleus(ElementData element)
    {
        int totalNucleons = element.Protons + element.Neutrons;
        for (int i = 0; i < element.Protons; i++)
        {
            CreateParticle(protonTemplate, $"Proton {i + 1}", nucleusRoot, GetNucleonPosition(i, totalNucleons), 0.52f);
        }

        for (int i = 0; i < element.Neutrons; i++)
        {
            CreateParticle(neutronTemplate, $"Neutron {i + 1}", nucleusRoot, GetNucleonPosition(element.Protons + i, totalNucleons), 0.52f);
        }
    }

    private void BuildElectronShells(ElementData element)
    {
        electronRoot = new GameObject("Electrons").transform;
        electronRoot.SetParent(runtimeRoot, false);

        electrons = new Transform[element.Electrons];
        electronMotions = new ElectronMotion[element.Electrons];

        for (int i = 0; i < element.Electrons; i++)
        {
            Transform electron = CreateParticle(electronTemplate, $"Electron {i + 1}", electronRoot, Vector3.zero, 0.32f).transform;
            electrons[i] = electron;
            electronMotions[i] = CreateElectronMotion(i);
        }

        UpdateElectronPositions();
    }

    private GameObject CreateParticle(GameObject template, string objectName, Transform parent, Vector3 localPosition, float scale)
    {
        GameObject particle = Instantiate(template, parent);
        particle.name = objectName;
        particle.SetActive(true);
        particle.transform.localPosition = localPosition;
        particle.transform.localRotation = Quaternion.identity;
        particle.transform.localScale = Vector3.one * scale;
        return particle;
    }

    private void UpdateElectronPositions()
    {
        for (int i = 0; i < electrons.Length; i++)
        {
            ElectronMotion motion = electronMotions[i];
            UpdateVisibilityCycle(ref motion);

            Vector3 position = motion.Shape == OrbitalShape.Sphere
                ? GetSphericalOrbitalPosition(motion)
                : GetDumbbellOrbitalPosition(motion);

            bool hiddenAtNucleus = position.magnitude < nucleusHideRadius;
            electrons[i].position = position;
            electrons[i].gameObject.SetActive(!motion.Hidden && !hiddenAtNucleus);
            electronMotions[i] = motion;
        }
    }

    private void EnsureCameraAndLight()
    {
        if (Camera.main == null)
        {
            GameObject cameraObject = new("Main Camera");
            cameraObject.tag = "MainCamera";
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.transform.position = new Vector3(0.0f, 4.2f, -10.0f);
            camera.transform.rotation = Quaternion.Euler(22.0f, 0.0f, 0.0f);
            camera.fieldOfView = 50.0f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
            orbitCamera = camera;
        }
        else
        {
            orbitCamera = Camera.main;
            orbitCamera.clearFlags = CameraClearFlags.SolidColor;
            orbitCamera.backgroundColor = Color.black;
        }

        if (FindFirstObjectByType<Light>() != null)
        {
            return;
        }

        GameObject lightObject = new("Directional Light");
        Light light = lightObject.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.4f;
        lightObject.transform.rotation = Quaternion.Euler(50.0f, -35.0f, 0.0f);
    }

    private void UpdateCameraControls()
    {
        if (orbitCamera == null)
        {
            return;
        }

        if (Input.GetMouseButton(0))
        {
            cameraYaw += Input.GetAxis("Mouse X") * orbitSensitivity * Time.deltaTime;
            cameraPitch -= Input.GetAxis("Mouse Y") * orbitSensitivity * Time.deltaTime;
            cameraPitch = Mathf.Clamp(cameraPitch, -75.0f, 75.0f);
        }

        float scroll = Input.mouseScrollDelta.y;
        if (Mathf.Abs(scroll) > 0.001f)
        {
            cameraDistance = Mathf.Clamp(cameraDistance - scroll * zoomSensitivity, minCameraDistance, maxCameraDistance);
        }

        PositionCamera();
    }

    private void UpdateTouchControls()
    {
        if (orbitCamera == null)
        {
            return;
        }

        if (Input.touchCount == 1)
        {
            Touch touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Moved)
            {
                cameraYaw += touch.deltaPosition.x * touchOrbitSensitivity;
                cameraPitch -= touch.deltaPosition.y * touchOrbitSensitivity;
                cameraPitch = Mathf.Clamp(cameraPitch, -75.0f, 75.0f);
                PositionCamera();
            }
        }
        else if (Input.touchCount >= 2)
        {
            Touch first = Input.GetTouch(0);
            Touch second = Input.GetTouch(1);
            float pinchDistance = Vector2.Distance(first.position, second.position);

            if (previousPinchDistance > 0.0f)
            {
                float delta = pinchDistance - previousPinchDistance;
                cameraDistance = Mathf.Clamp(cameraDistance - delta * touchZoomSensitivity, minCameraDistance, maxCameraDistance);
                PositionCamera();
            }

            previousPinchDistance = pinchDistance;
        }
        else
        {
            previousPinchDistance = 0.0f;
        }
    }

    private void PositionCamera()
    {
        if (orbitCamera == null)
        {
            return;
        }

        Quaternion rotation = Quaternion.Euler(cameraPitch, cameraYaw, 0.0f);
        Vector3 offset = rotation * new Vector3(0.0f, 0.0f, -cameraDistance);
        orbitCamera.transform.position = Vector3.zero + offset;
        orbitCamera.transform.rotation = rotation;
    }

    private static int[] GetShellCounts(int electronCount)
    {
        int[] capacities = { 2, 8, 8, 2 };
        int[] shellCounts = new int[capacities.Length];
        int remaining = electronCount;

        for (int i = 0; i < capacities.Length && remaining > 0; i++)
        {
            shellCounts[i] = Mathf.Min(capacities[i], remaining);
            remaining -= shellCounts[i];
        }

        return shellCounts;
    }

    private static string FormatShells(int[] shellCounts)
    {
        string result = string.Empty;
        for (int i = 0; i < shellCounts.Length; i++)
        {
            if (shellCounts[i] == 0)
            {
                continue;
            }

            result += result.Length == 0 ? shellCounts[i].ToString() : $"-{shellCounts[i]}";
        }

        return result;
    }

    private float GetShellRadius(int shell)
    {
        return firstShellRadius + shell * shellSpacing;
    }

    private ElectronMotion CreateElectronMotion(int zeroBasedElectronIndex)
    {
        int electronNumber = zeroBasedElectronIndex + 1;
        ElectronMotion motion = new()
        {
            Phase = zeroBasedElectronIndex * 73.0f,
            SpeedMultiplier = UnityEngine.Random.Range(0.82f, 1.18f),
            MoveUntil = Time.time + UnityEngine.Random.Range(1.0f, 5.0f),
            HiddenUntil = -1.0f
        };

        if (electronNumber <= 2)
        {
            motion.Shape = OrbitalShape.Sphere;
            motion.Radius = firstShellRadius * 0.82f;
            motion.Tilt = Quaternion.Euler(20.0f, electronNumber * 57.0f, 0.0f);
            return motion;
        }

        if (electronNumber <= 4)
        {
            motion.Shape = OrbitalShape.Sphere;
            motion.Radius = firstShellRadius + shellSpacing * 0.55f;
            motion.Tilt = Quaternion.Euler(38.0f, electronNumber * 47.0f, 18.0f);
            return motion;
        }

        if (electronNumber <= 10)
        {
            motion.Shape = OrbitalShape.Dumbbell;
            motion.Axis = GetAxisForDumbbellElectron(electronNumber - 5);
            motion.Radius = firstShellRadius + shellSpacing * 1.15f;
            motion.Width = 0.8f;
            return motion;
        }

        if (electronNumber <= 12)
        {
            motion.Shape = OrbitalShape.Sphere;
            motion.Radius = firstShellRadius + shellSpacing * 1.85f;
            motion.Tilt = Quaternion.Euler(52.0f, electronNumber * 31.0f, 26.0f);
            return motion;
        }

        if (electronNumber <= 18)
        {
            motion.Shape = OrbitalShape.Dumbbell;
            motion.Axis = GetAxisForDumbbellElectron(electronNumber - 13);
            motion.Radius = firstShellRadius + shellSpacing * 2.35f;
            motion.Width = 0.95f;
            return motion;
        }

        motion.Shape = OrbitalShape.Sphere;
        motion.Radius = firstShellRadius + shellSpacing * 3.0f;
        motion.Tilt = Quaternion.Euler(65.0f, electronNumber * 29.0f, 35.0f);
        return motion;
    }

    private void UpdateVisibilityCycle(ref ElectronMotion motion)
    {
        float now = Time.time;
        if (!motion.Hidden && now >= motion.MoveUntil)
        {
            motion.Hidden = true;
            motion.HiddenUntil = now + 2.0f;
            return;
        }

        if (motion.Hidden && now >= motion.HiddenUntil)
        {
            motion.Hidden = false;
            motion.MoveUntil = now + UnityEngine.Random.Range(1.0f, 5.0f);
        }
    }

    private Vector3 GetSphericalOrbitalPosition(ElectronMotion motion)
    {
        float radians = (orbitAngle * motion.SpeedMultiplier + motion.Phase) * Mathf.Deg2Rad;
        Vector3 localPosition = new(
            Mathf.Cos(radians) * motion.Radius,
            Mathf.Sin(radians * 0.73f + motion.Phase * Mathf.Deg2Rad) * motion.Radius * 0.52f,
            Mathf.Sin(radians) * motion.Radius);

        return motion.Tilt * localPosition;
    }

    private Vector3 GetDumbbellOrbitalPosition(ElectronMotion motion)
    {
        Vector3 axis = motion.Axis.normalized;
        Vector3 tangent = Vector3.Cross(axis, Vector3.up);
        if (tangent.sqrMagnitude < 0.001f)
        {
            tangent = Vector3.Cross(axis, Vector3.right);
        }

        tangent.Normalize();

        float radians = (orbitAngle * motion.SpeedMultiplier + motion.Phase) * Mathf.Deg2Rad;
        float alongAxis = Mathf.Sin(radians) * motion.Radius;
        float lobeCurve = Mathf.Sin(radians * 2.0f) * motion.Width;
        return axis * alongAxis + tangent * lobeCurve;
    }

    private static Vector3 GetAxisForDumbbellElectron(int orbitalIndex)
    {
        switch (orbitalIndex % 3)
        {
            case 0:
                return Vector3.right;
            case 1:
                return Vector3.up;
            default:
                return Vector3.forward;
        }
    }

    private static Vector3 GetNucleonPosition(int index, int total)
    {
        if (total <= 1)
        {
            return Vector3.zero;
        }

        float goldenAngle = Mathf.PI * (3.0f - Mathf.Sqrt(5.0f));
        float y = 1.0f - index / (float)(total - 1) * 2.0f;
        float radius = Mathf.Sqrt(1.0f - y * y);
        float theta = goldenAngle * index;
        Vector3 direction = new(Mathf.Cos(theta) * radius, y, Mathf.Sin(theta) * radius);
        float clusterRadius = 0.36f + Mathf.Pow(total, 1.0f / 3.0f) * 0.15f;
        return direction * clusterRadius;
    }

    private void InitializeGuiStyles()
    {
        if (panelStyle != null)
        {
            return;
        }

        panelStyle = new GUIStyle(GUI.skin.box)
        {
            padding = new RectOffset(12, 12, 12, 12),
            fontSize = 14,
            alignment = TextAnchor.UpperLeft
        };

        selectedButtonStyle = new GUIStyle(GUI.skin.button)
        {
            fontStyle = FontStyle.Bold
        };
        selectedButtonStyle.normal.textColor = Color.cyan;
        selectedButtonStyle.hover.textColor = Color.cyan;
        selectedButtonStyle.active.textColor = Color.cyan;
    }

    private readonly struct ElementData
    {
        public ElementData(int atomicNumber, string symbol, string name, int protons, int electrons, int neutrons)
        {
            AtomicNumber = atomicNumber;
            Symbol = symbol;
            Name = name;
            Protons = protons;
            Electrons = electrons;
            Neutrons = neutrons;
        }

        public int AtomicNumber { get; }
        public string Symbol { get; }
        public string Name { get; }
        public int Protons { get; }
        public int Electrons { get; }
        public int Neutrons { get; }
    }

    private enum OrbitalShape
    {
        Sphere,
        Dumbbell
    }

    private struct ElectronMotion
    {
        public OrbitalShape Shape;
        public Vector3 Axis;
        public Quaternion Tilt;
        public float Radius;
        public float Width;
        public float Phase;
        public float SpeedMultiplier;
        public float MoveUntil;
        public float HiddenUntil;
        public bool Hidden;
    }
}
