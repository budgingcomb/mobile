using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;

// Меню: Серая комната -> Собрать вертикальный срез.
// Создаёт сцену: комнату, игрока, свет, шкатулку, шкаф, системы и звуки, и всё связывает.
public static class SliceBuilder
{
    const string Audio = "Assets/Audio/";

    [MenuItem("Серая комната/Собрать вертикальный срез")]
    public static void Build()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        if (!File.Exists(Audio + "lullaby.wav"))
        {
            EditorUtility.DisplayDialog("Нет звуков", "Не найдена папка Assets/Audio со звуками из архива.", "Ок");
            return;
        }

        int layer = EnsureLayer("Interactable");
        Directory.CreateDirectory("Assets/Materials");
        Directory.CreateDirectory("Assets/Scenes");
        ConfigureAudioImport();

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // ---------- Материалы ----------
        var mFloor = Mat("Floor", new Color(0.26f, 0.26f, 0.28f));
        var mWall = Mat("Wall", new Color(0.46f, 0.46f, 0.49f));
        var mFurn = Mat("Furniture", new Color(0.36f, 0.35f, 0.37f));
        var mSoft = Mat("Soft", new Color(0.5f, 0.5f, 0.53f));
        var mWindow = Mat("Window", new Color(0.7f, 0.78f, 0.9f), new Color(0.45f, 0.55f, 0.75f));
        var mGold = Mat("Gold", new Color(0.9f, 0.7f, 0.2f), new Color(1.2f, 0.9f, 0.3f));
        // Родной цвет шкатулки; emission почти чёрный, но не нулевой, иначе Unity выключит ключевое слово
        var mBox = Mat("MusicBox", new Color(0.62f, 0.34f, 0.16f), new Color(0.02f, 0.02f, 0.02f));
        var mHolo = Mat("Hologram", new Color(0.7f, 0.85f, 1f, 0.35f), new Color(0.25f, 0.4f, 0.6f), true);

        // ---------- Комната ----------
        var room = new GameObject("Room").transform;
        Prim(PrimitiveType.Cube, "Floor", room, new Vector3(0, -0.05f, 0), new Vector3(7.4f, 0.1f, 8.4f), mFloor);
        Prim(PrimitiveType.Cube, "Ceiling", room, new Vector3(0, 3.05f, 0), new Vector3(7.4f, 0.1f, 8.4f), mWall);
        Prim(PrimitiveType.Cube, "WallBack", room, new Vector3(0, 1.5f, -4.1f), new Vector3(7.4f, 3.1f, 0.2f), mWall);
        Prim(PrimitiveType.Cube, "WallFront", room, new Vector3(0, 1.5f, 4.1f), new Vector3(7.4f, 3.1f, 0.2f), mWall);
        Prim(PrimitiveType.Cube, "WallLeft", room, new Vector3(-3.6f, 1.5f, 0), new Vector3(0.2f, 3.1f, 8.4f), mWall);
        Prim(PrimitiveType.Cube, "WallRight", room, new Vector3(3.6f, 1.5f, 0), new Vector3(0.2f, 3.1f, 8.4f), mWall);
        // Перегородка между зонами А и Б с проходом
        Prim(PrimitiveType.Cube, "DividerL", room, new Vector3(-2.15f, 1.5f, -1.5f), new Vector3(2.7f, 3f, 0.2f), mWall);
        Prim(PrimitiveType.Cube, "DividerR", room, new Vector3(2.25f, 1.5f, -1.5f), new Vector3(2.5f, 3f, 0.2f), mWall);
        Prim(PrimitiveType.Cube, "DividerTop", room, new Vector3(0.1f, 2.7f, -1.5f), new Vector3(1.9f, 0.6f, 0.2f), mWall);
        Prim(PrimitiveType.Cube, "Window", room, new Vector3(-3.47f, 1.7f, 0.2f), new Vector3(0.05f, 1.2f, 1.4f), mWindow, false);

        // Зона А: кровать, тумбочка, ковёр, полка
        Prim(PrimitiveType.Cube, "BedFrame", room, new Vector3(-2.5f, 0.2f, 2.2f), new Vector3(1.1f, 0.35f, 2.1f), mFurn);
        Prim(PrimitiveType.Cube, "Mattress", room, new Vector3(-2.5f, 0.45f, 2.2f), new Vector3(1.0f, 0.2f, 2.0f), mSoft);
        Prim(PrimitiveType.Cube, "Pillow", room, new Vector3(-2.5f, 0.6f, 3.0f), new Vector3(0.7f, 0.15f, 0.4f), mSoft, false);
        Prim(PrimitiveType.Cube, "Nightstand", room, new Vector3(-1.5f, 0.25f, 3.2f), new Vector3(0.55f, 0.5f, 0.55f), mFurn);
        Prim(PrimitiveType.Cylinder, "Rug", room, new Vector3(-0.6f, 0.012f, 1.0f), new Vector3(2.4f, 0.01f, 2.4f), mSoft, false);
        Prim(PrimitiveType.Cube, "Shelf", room, new Vector3(3.35f, 1.4f, 1.5f), new Vector3(0.3f, 0.08f, 1.6f), mFurn);

        // Зона Б: шкаф без передней стенки, дверь на петле
        var cup = new GameObject("Cupboard").transform;
        cup.SetParent(room, false);
        Prim(PrimitiveType.Cube, "CBack", cup, new Vector3(2.2f, 1f, -3.95f), new Vector3(1.4f, 2f, 0.05f), mFurn);
        Prim(PrimitiveType.Cube, "CLeft", cup, new Vector3(1.5f, 1f, -3.65f), new Vector3(0.05f, 2f, 0.6f), mFurn);
        Prim(PrimitiveType.Cube, "CRight", cup, new Vector3(2.9f, 1f, -3.65f), new Vector3(0.05f, 2f, 0.6f), mFurn);
        Prim(PrimitiveType.Cube, "CTop", cup, new Vector3(2.2f, 2f, -3.65f), new Vector3(1.4f, 0.05f, 0.6f), mFurn);
        Prim(PrimitiveType.Cube, "CBottom", cup, new Vector3(2.2f, 0.05f, -3.65f), new Vector3(1.4f, 0.05f, 0.6f), mFurn);
        Prim(PrimitiveType.Cube, "CShelf", cup, new Vector3(2.2f, 1f, -3.65f), new Vector3(1.3f, 0.05f, 0.5f), mFurn);
        Prim(PrimitiveType.Cube, "Key", cup, new Vector3(2.2f, 1.06f, -3.65f), new Vector3(0.14f, 0.04f, 0.05f), mGold, false);
        var hinge = new GameObject("DoorHinge").transform;
        hinge.SetParent(cup, false);
        hinge.localPosition = new Vector3(1.5f, 1f, -3.35f);
        Prim(PrimitiveType.Cube, "Door", hinge, new Vector3(0.7f, 0f, 0f), new Vector3(1.4f, 1.95f, 0.05f), mFurn, true, false);
        var cupLight = MakeLight("CupboardLight", cup, new Vector3(2.2f, 1.5f, -3.2f), LightType.Point, new Color(1f, 0.8f, 0.5f), 1.5f, 3f);
        cupLight.gameObject.SetActive(false);

        // ---------- Свет ----------
        var sun = MakeLight("Directional Light", null, Vector3.zero, LightType.Directional, new Color(0.85f, 0.88f, 1f), 0.6f, 0f);
        sun.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        var warm = MakeLight("WarmLight", null, new Vector3(-1.5f, 1.8f, 2.4f), LightType.Point, new Color(1f, 0.8f, 0.55f), 0f, 9f);
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.33f, 0.33f, 0.36f);

        // ---------- Игрок ----------
        var playerGo = new GameObject("Player");
        playerGo.transform.position = new Vector3(1.5f, 0.05f, 1.0f);
        playerGo.transform.rotation = Quaternion.Euler(0f, -74f, 0f);
        var cc = playerGo.AddComponent<CharacterController>();
        cc.height = 1.2f; cc.radius = 0.25f; cc.center = new Vector3(0f, 0.6f, 0f); cc.skinWidth = 0.03f; cc.stepOffset = 0.2f;

        var pivot = new GameObject("CameraPivot").transform;
        pivot.SetParent(playerGo.transform, false);
        pivot.localPosition = new Vector3(0f, 1.1f, 0f);
        var camGo = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
        camGo.tag = "MainCamera";
        camGo.transform.SetParent(pivot, false);
        var cam = camGo.GetComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.1f, 0.1f, 0.12f);
        cam.nearClipPlane = 0.03f; cam.farClipPlane = 30f; cam.fieldOfView = 70f;

        var pc = playerGo.AddComponent<PlayerController>();
        var look = playerGo.AddComponent<TouchLook>();
        var joy = playerGo.AddComponent<FloatingJoystick>();
        var inter = playerGo.AddComponent<Interactor>();
        pc.joystick = joy;
        look.cameraPivot = pivot; look.cam = cam;
        inter.cam = cam; inter.interactMask = 1 << layer;

        var start = new GameObject("PlayerStart").transform;
        start.SetPositionAndRotation(playerGo.transform.position, playerGo.transform.rotation);

        // ---------- Шкатулка и голограмма ----------
        var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
        box.name = "MusicBox";
        box.layer = layer;
        box.transform.position = new Vector3(-1.5f, 0.62f, 3.2f);
        box.transform.localScale = new Vector3(0.42f, 0.24f, 0.3f);
        box.GetComponent<Renderer>().sharedMaterial = mBox;
        box.GetComponent<BoxCollider>().size = Vector3.one * 1.8f;   // увеличенная зона касания для пальца
        var mem = box.AddComponent<MemoryObject>();
        var holo = box.AddComponent<HologramPlayer>();
        var puzzle = box.AddComponent<MusicBoxPuzzle>();
        mem.memoryIndex = 0; mem.hologram = holo; mem.puzzle = puzzle;

        var holoRoot = new GameObject("HologramVisual");
        holoRoot.transform.position = new Vector3(-1.1f, 0f, 1.5f);
        holoRoot.transform.rotation = Quaternion.Euler(0f, -120f, 0f);
        Prim(PrimitiveType.Capsule, "Body", holoRoot.transform, new Vector3(0f, 0.7f, 0f), new Vector3(0.5f, 0.6f, 0.5f), mHolo, false);
        Prim(PrimitiveType.Sphere, "Head", holoRoot.transform, new Vector3(0f, 1.5f, 0f), new Vector3(0.3f, 0.3f, 0.3f), mHolo, false);
        var hl = MakeLight("HoloLight", holoRoot.transform, new Vector3(0f, 1f, 0.4f), LightType.Point, new Color(0.6f, 0.8f, 1f), 0.8f, 3f);
        var holoSrc = holoRoot.AddComponent<AudioSource>();
        holoSrc.playOnAwake = false; holoSrc.spatialBlend = 0.6f; holoSrc.minDistance = 3f; holoSrc.maxDistance = 15f;
        holoRoot.SetActive(false);
        holo.visual = holoRoot; holo.source = holoSrc; holo.clip = Clip("lullaby");

        puzzle.notes = new[] { Clip("note_1"), Clip("note_2"), Clip("note_3"), Clip("note_4") };
        puzzle.wrongClip = Clip("wrong"); puzzle.rightClip = Clip("success");

        // ---------- Системы ----------
        var sys = new GameObject("Systems");
        var ambient = Source(sys.transform, "Ambient", Clip("ambient"), true, 0.35f);
        var whisper = Source(sys.transform, "Whisper", Clip("whisper"), true, 0f);
        var heart = Source(sys.transform, "Heartbeat", Clip("heartbeat"), true, 0f);
        var sfx = Source(sys.transform, "Sfx", null, false, 1f);

        var forget = sys.AddComponent<ForgettingSystem>();
        forget.ambient = ambient; forget.whisper = whisper; forget.heartbeat = heart; forget.mainLight = sun;

        var ui = sys.AddComponent<UIManager>();
        ui.joystick = joy; ui.interactor = inter; ui.forgetting = forget;

        var gm = sys.AddComponent<GameManager>();
        gm.player = pc; gm.look = look; gm.joystick = joy; gm.interactor = inter;
        gm.forgetting = forget; gm.ui = ui; gm.memories = new[] { mem };
        gm.sfx = sfx; gm.restoreClip = Clip("restore"); gm.doorClip = Clip("door");
        gm.warmLight = warm; gm.cupboardHinge = hinge; gm.cupboardLight = cupLight.gameObject; gm.playerStart = start;

        var es = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));

        // ---------- Настройки проекта и сохранение сцены ----------
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
        PlayerSettings.allowedAutorotateToPortrait = false;
        PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
        PlayerSettings.allowedAutorotateToLandscapeLeft = true;
        PlayerSettings.allowedAutorotateToLandscapeRight = true;

        const string scenePath = "Assets/Scenes/GraySlice.unity";
        EditorSceneManager.SaveScene(scene, scenePath);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(scenePath, true) };
        AssetDatabase.SaveAssets();
        Debug.Log("Серая комната: сцена собрана и сохранена в " + scenePath + ". Нажми Play.");
    }

    // ---------- Помощники ----------
    static AudioClip Clip(string n) => AssetDatabase.LoadAssetAtPath<AudioClip>(Audio + n + ".wav");

    static AudioSource Source(Transform parent, string name, AudioClip clip, bool loop, float vol)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var s = go.AddComponent<AudioSource>();
        s.clip = clip; s.loop = loop; s.volume = vol; s.playOnAwake = false; s.spatialBlend = 0f;
        return s;
    }

    static Light MakeLight(string name, Transform parent, Vector3 pos, LightType type, Color color, float intensity, float range)
    {
        var go = new GameObject(name);
        if (parent != null) go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        var l = go.AddComponent<Light>();
        l.type = type; l.color = color; l.intensity = intensity;
        if (type != LightType.Directional) l.range = range;
        l.shadows = LightShadows.None;   // на мобильных тени не нужны
        return l;
    }

    static GameObject Prim(PrimitiveType t, string name, Transform parent, Vector3 pos, Vector3 scale, Material mat, bool collider = true, bool isStatic = true)
    {
        var g = GameObject.CreatePrimitive(t);
        g.name = name;
        g.transform.SetParent(parent, false);
        g.transform.localPosition = pos;
        g.transform.localScale = scale;
        g.GetComponent<Renderer>().sharedMaterial = mat;
        if (!collider) Object.DestroyImmediate(g.GetComponent<Collider>());
        g.isStatic = isStatic;
        return g;
    }

    static Material Mat(string name, Color c, Color? emission = null, bool transparent = false)
    {
        string path = "Assets/Materials/" + name + ".mat";
        var sh = Shader.Find("Universal Render Pipeline/Lit");
        if (sh == null) sh = Shader.Find("Standard");
        bool urp = sh.name.StartsWith("Universal");
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null) { m = new Material(sh); AssetDatabase.CreateAsset(m, path); }
        else m.shader = sh;

        m.SetColor(urp ? "_BaseColor" : "_Color", c);
        if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", 0.15f);
        if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", 0f);
        if (emission.HasValue)
        {
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", emission.Value);
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        }
        if (transparent && urp)
        {
            m.SetFloat("_Surface", 1f);
            m.SetFloat("_Blend", 0f);
            m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            m.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            if (m.HasProperty("_SrcBlendAlpha")) m.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
            if (m.HasProperty("_DstBlendAlpha")) m.SetFloat("_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_ZWrite", 0f);
            m.SetOverrideTag("RenderType", "Transparent");
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.renderQueue = (int)RenderQueue.Transparent;
            m.SetShaderPassEnabled("ShadowCaster", false);
        }
        EditorUtility.SetDirty(m);
        return m;
    }

    static int EnsureLayer(string name)
    {
        var tm = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        var layers = tm.FindProperty("layers");
        for (int i = 8; i < 32; i++)
            if (layers.GetArrayElementAtIndex(i).stringValue == name) return i;
        for (int i = 8; i < 32; i++)
        {
            var p = layers.GetArrayElementAtIndex(i);
            if (string.IsNullOrEmpty(p.stringValue))
            {
                p.stringValue = name;
                tm.ApplyModifiedProperties();
                return i;
            }
        }
        return 0;
    }

    // Большие звуки не держим в памяти целиком; сжатие Vorbis, моно
    static void ConfigureAudioImport()
    {
        string[] streaming = { "lullaby" };
        string[] compressed = { "ambient", "whisper", "heartbeat", "restore", "door" };
        foreach (var n in streaming) SetAudio(n, AudioClipLoadType.Streaming);
        foreach (var n in compressed) SetAudio(n, AudioClipLoadType.CompressedInMemory);
    }

    static void SetAudio(string n, AudioClipLoadType load)
    {
        var imp = AssetImporter.GetAtPath(Audio + n + ".wav") as AudioImporter;
        if (imp == null) return;
        var s = imp.defaultSampleSettings;
        s.loadType = load;
        s.compressionFormat = AudioCompressionFormat.Vorbis;
        s.quality = 0.5f;
        imp.defaultSampleSettings = s;
        imp.forceToMono = true;
        imp.SaveAndReimport();
    }
}
