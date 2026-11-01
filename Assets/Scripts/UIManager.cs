using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

// Весь интерфейс создаётся кодом при старте: HUD, меню, пауза, дневник, настройки, субтитры.
// Спрайты (круг, кольцо, виньетка) тоже рисуются кодом, внешних картинок не нужно.
[DefaultExecutionOrder(-100)]
public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    public FloatingJoystick joystick;
    public Interactor interactor;
    public ForgettingSystem forgetting;

    public RectTransform Content { get; private set; }   // корень внутри безопасной зоны экрана

    static readonly string[] MemoryNames =
    { "Музыкальная шкатулка", "Плюшевый медвежонок", "Настольные часы", "Детский рисунок",
      "Игрушечный паровозик", "Фоторамка", "Ночник" };

    Font font;
    Transform canvasRoot;
    Image vignette, fade;
    Text subtitleText, toastText, centerText, sensLabel, volLabel, leftBtnText, vibBtnText;
    GameObject hud, subtitleBox, toastBox, skipBtn;
    GameObject mainMenu, pausePanel, diaryPanel, settingsPanel, endPanel;
    Button continueBtn;
    Text[] diaryName = new Text[7];
    GameObject[] diaryReplay = new GameObject[7];
    Coroutine toastRoutine;
    Sprite circle, ring, vignetteSprite;

    void Awake()
    {
        Instance = this;
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font == null) font = Resources.GetBuiltinResource<Font>("Arial.ttf");

        circle = MakeCircle(128);
        ring = MakeRing(128, 0.78f, 0.97f);
        vignetteSprite = MakeVignette(128);
        BuildCanvas();
        BuildHud();
        BuildMainMenu();
        BuildPause();
        BuildDiary();
        BuildSettings();
        BuildEnd();
        BuildOverlays();
        forgetting.vignette = vignette;
        HideAllPanels();
    }

    // ---------- Построение ----------
    void BuildCanvas()
    {
        var go = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        go.transform.SetParent(transform, false);
        var c = go.GetComponent<Canvas>();
        c.renderMode = RenderMode.ScreenSpaceOverlay;
        var cs = go.GetComponent<CanvasScaler>();
        cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        cs.referenceResolution = new Vector2(1920f, 1080f);
        cs.matchWidthOrHeight = 0.5f;
        canvasRoot = go.transform;

        // Безопасная зона (вырезы и закруглённые углы)
        var safe = Stretch("Safe", canvasRoot);
        Rect sa = Screen.safeArea;
        safe.anchorMin = new Vector2(sa.xMin / Screen.width, sa.yMin / Screen.height);
        safe.anchorMax = new Vector2(sa.xMax / Screen.width, sa.yMax / Screen.height);
        safe.offsetMin = safe.offsetMax = Vector2.zero;
        Content = safe;
    }

    void BuildHud()
    {
        // Виньетка Забвения под HUD
        vignette = Img("Vignette", canvasRoot, Vector2.zero, Vector2.zero, Vector2.zero, new Color(1, 1, 1, 0), vignetteSprite);
        Fullscreen(vignette.rectTransform);

        // Джойстик и кольцо удержания (двигаются по экрану, поэтому лежат в корне Canvas)
        var jbg = Img("JoyBg", canvasRoot, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(220f, 220f), new Color(1, 1, 1, 0.22f), circle);
        var jh = Img("JoyHandle", jbg.transform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(100f, 100f), new Color(1, 1, 1, 0.55f), circle);
        jh.transform.SetParent(canvasRoot, false);   // ручка двигается независимо от основы
        joystick.Bind(jbg.rectTransform, jh.rectTransform);

        var ringRoot = Rect("HoldRing", canvasRoot, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(150f, 150f));
        Img("Bg", ringRoot, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(150f, 150f), new Color(1, 1, 1, 0.25f), ring);
        var fill = Img("Fill", ringRoot, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(150f, 150f), new Color(1f, 0.92f, 0.7f, 1f), ring);
        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Radial360;
        fill.fillOrigin = 2;
        fill.fillClockwise = true;
        fill.fillAmount = 0f;
        interactor.Bind(ringRoot, fill);

        // HUD в безопасной зоне
        hud = Rect("Hud", Content, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero).gameObject;
        Fullscreen(hud.GetComponent<RectTransform>());
        var h = hud.transform;

        Img("Crosshair", h, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(12f, 12f), new Color(1, 1, 1, 0.8f), circle);
        Btn("Pause", h, new Vector2(1f, 1f), new Vector2(-90f, -90f), new Vector2(130f, 130f), "II", () => GameManager.Instance.Pause(), 56);
        Btn("Lamp", h, new Vector2(1f, 0f), new Vector2(-150f, 120f), new Vector2(230f, 130f), "Лампа", () => GameManager.Instance.UseHint(), 44);
        skipBtn = Btn("Skip", h, new Vector2(1f, 0.5f), new Vector2(-190f, 0f), new Vector2(330f, 120f), "Пропустить", null, 42).gameObject;

        // Субтитры (ГДД: для всех голограмм)
        var sb = Img("Subtitles", h, new Vector2(0.5f, 0f), new Vector2(0f, 90f), new Vector2(1400f, 110f), new Color(0, 0, 0, 0.55f));
        subtitleBox = sb.gameObject;
        subtitleText = Txt("T", sb.transform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1360f, 100f), "", 40, TextAnchor.MiddleCenter, Color.white);

        var tb = Img("Toast", h, new Vector2(0.5f, 1f), new Vector2(0f, -90f), new Vector2(1300f, 110f), new Color(0, 0, 0, 0.6f));
        toastBox = tb.gameObject;
        toastText = Txt("T", tb.transform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1260f, 100f), "", 38, TextAnchor.MiddleCenter, Color.white);

        subtitleBox.SetActive(false);
        toastBox.SetActive(false);
        skipBtn.SetActive(false);
    }

    void BuildMainMenu()
    {
        mainMenu = Panel("MainMenu", new Color(0.04f, 0.04f, 0.06f, 0.93f));
        var p = mainMenu.transform;
        Txt("Title", p, new Vector2(0.5f, 0.5f), new Vector2(0f, 300f), new Vector2(1500f, 160f), "СЕРАЯ КОМНАТА", 120, TextAnchor.MiddleCenter, Color.white);
        Txt("Sub", p, new Vector2(0.5f, 0.5f), new Vector2(0f, 190f), new Vector2(1200f, 80f), "Память предметов", 48, TextAnchor.MiddleCenter, new Color(1, 1, 1, 0.7f));
        Btn("New", p, new Vector2(0.5f, 0.5f), new Vector2(0f, 40f), new Vector2(560f, 130f), "Новая игра", () => { GameManager.Instance.NewGame(); }, 52);
        continueBtn = Btn("Continue", p, new Vector2(0.5f, 0.5f), new Vector2(0f, -110f), new Vector2(560f, 130f), "Продолжить", () => { GameManager.Instance.ContinueGame(); }, 52);
        Btn("Quit", p, new Vector2(0.5f, 0.5f), new Vector2(0f, -260f), new Vector2(560f, 130f), "Выход", () => GameManager.Instance.Quit(), 52);
    }

    void BuildPause()
    {
        pausePanel = Panel("Pause", new Color(0, 0, 0, 0.75f));
        var p = pausePanel.transform;
        Txt("T", p, new Vector2(0.5f, 0.5f), new Vector2(0f, 330f), new Vector2(900f, 120f), "Пауза", 90, TextAnchor.MiddleCenter, Color.white);
        Btn("Resume", p, new Vector2(0.5f, 0.5f), new Vector2(0f, 160f), new Vector2(560f, 125f), "Продолжить", () => GameManager.Instance.Resume(), 50);
        Btn("Diary", p, new Vector2(0.5f, 0.5f), new Vector2(0f, 20f), new Vector2(560f, 125f), "Дневник", () => ShowPanel(diaryPanel, true), 50);
        Btn("Settings", p, new Vector2(0.5f, 0.5f), new Vector2(0f, -120f), new Vector2(560f, 125f), "Настройки", () => ShowPanel(settingsPanel, false), 50);
        Btn("Menu", p, new Vector2(0.5f, 0.5f), new Vector2(0f, -260f), new Vector2(560f, 125f), "В меню", () => GameManager.Instance.ReturnToMenu(), 50);
    }

    void BuildDiary()
    {
        diaryPanel = Panel("Diary", new Color(0.05f, 0.05f, 0.07f, 0.95f));
        var p = diaryPanel.transform;
        Txt("T", p, new Vector2(0.5f, 1f), new Vector2(0f, -110f), new Vector2(1300f, 110f), "Дневник воспоминаний", 70, TextAnchor.MiddleCenter, Color.white);
        for (int i = 0; i < 7; i++)
        {
            int idx = i;
            int row = i / 4, col = i % 4;
            int inRow = row == 0 ? 4 : 3;
            float x = (col - (inRow - 1) * 0.5f) * 420f;
            float y = 150f - row * 320f;
            var cell = Img("Cell" + i, p, new Vector2(0.5f, 0.5f), new Vector2(x, y), new Vector2(390f, 300f), new Color(1, 1, 1, 0.1f));
            Txt("N", cell.transform, new Vector2(0.5f, 1f), new Vector2(0f, -45f), new Vector2(370f, 60f), (i + 1).ToString(), 44, TextAnchor.MiddleCenter, new Color(1, 1, 1, 0.5f));
            diaryName[i] = Txt("Name", cell.transform, new Vector2(0.5f, 0.5f), new Vector2(0f, 0f), new Vector2(360f, 120f), "?", 38, TextAnchor.MiddleCenter, Color.white);
            if (i == 0)
            {
                var b = Btn("Replay", cell.transform, new Vector2(0.5f, 0f), new Vector2(0f, 55f), new Vector2(330f, 90f), "Пересмотреть", () => GameManager.Instance.ReplayMemory(idx), 34);
                diaryReplay[0] = b.gameObject;
            }
        }
        Btn("Back", p, new Vector2(0.5f, 0f), new Vector2(0f, 90f), new Vector2(420f, 120f), "Назад", () => ShowPanel(pausePanel, false), 48);
    }

    void BuildSettings()
    {
        settingsPanel = Panel("Settings", new Color(0.05f, 0.05f, 0.07f, 0.95f));
        var p = settingsPanel.transform;
        Txt("T", p, new Vector2(0.5f, 1f), new Vector2(0f, -100f), new Vector2(1200f, 110f), "Настройки", 70, TextAnchor.MiddleCenter, Color.white);

        sensLabel = Txt("SensL", p, new Vector2(0.5f, 0.5f), new Vector2(-520f, 250f), new Vector2(520f, 70f), "", 38, TextAnchor.MiddleLeft, Color.white);
        MakeSlider(p, new Vector2(230f, 250f), 0.05f, 0.4f, ControlSettings.lookSensitivity, v => { ControlSettings.lookSensitivity = v; RefreshSettingsText(); });
        volLabel = Txt("VolL", p, new Vector2(0.5f, 0.5f), new Vector2(-520f, 130f), new Vector2(520f, 70f), "", 38, TextAnchor.MiddleLeft, Color.white);
        MakeSlider(p, new Vector2(230f, 130f), 0f, 1f, ControlSettings.masterVolume, v => { ControlSettings.masterVolume = v; AudioListener.volume = v; RefreshSettingsText(); });

        var lb = Btn("Left", p, new Vector2(0.5f, 0.5f), new Vector2(-320f, 0f), new Vector2(600f, 120f), "", () => { ControlSettings.leftHanded = !ControlSettings.leftHanded; RefreshSettingsText(); }, 40);
        leftBtnText = lb.GetComponentInChildren<Text>();
        var vb = Btn("Vib", p, new Vector2(0.5f, 0.5f), new Vector2(320f, 0f), new Vector2(600f, 120f), "", () => { ControlSettings.vibration = !ControlSettings.vibration; RefreshSettingsText(); }, 40);
        vibBtnText = vb.GetComponentInChildren<Text>();

        Btn("Demo", p, new Vector2(0.5f, 0.5f), new Vector2(0f, -150f), new Vector2(760f, 120f), "Демо: усилить Забвение (+20%)", () => GameManager.Instance.DemoForgetting(), 38);
        Btn("Back", p, new Vector2(0.5f, 0f), new Vector2(0f, 90f), new Vector2(420f, 120f), "Назад", () => { ControlSettings.Save(); ShowPanel(pausePanel, false); }, 48);
    }

    void BuildEnd()
    {
        endPanel = Panel("End", new Color(0.03f, 0.03f, 0.05f, 0.9f));
        var p = endPanel.transform;
        Txt("T", p, new Vector2(0.5f, 0.5f), new Vector2(0f, 220f), new Vector2(1500f, 140f), "Воспоминание возвращено", 84, TextAnchor.MiddleCenter, new Color(1f, 0.92f, 0.7f));
        Txt("S", p, new Vector2(0.5f, 0.5f), new Vector2(0f, 60f), new Vector2(1400f, 220f),
            "Ключик из шкатулки открыл шкаф в зоне Б.\nВпереди ещё шесть воспоминаний.", 48, TextAnchor.MiddleCenter, Color.white);
        Btn("Go", p, new Vector2(0.5f, 0.5f), new Vector2(-300f, -170f), new Vector2(520f, 125f), "Продолжить", () => GameManager.Instance.Resume(), 48);
        Btn("Menu", p, new Vector2(0.5f, 0.5f), new Vector2(300f, -170f), new Vector2(520f, 125f), "В меню", () => GameManager.Instance.ReturnToMenu(), 48);
    }

    void BuildOverlays()
    {
        // Затемнение и центральный текст лежат поверх всего
        fade = Img("Fade", canvasRoot, Vector2.zero, Vector2.zero, Vector2.zero, new Color(0, 0, 0, 0));
        Fullscreen(fade.rectTransform);
        centerText = Txt("Center", canvasRoot, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1500f, 200f), "", 70, TextAnchor.MiddleCenter, Color.white);
        centerText.gameObject.SetActive(false);
    }

    // ---------- Показ экранов ----------
    void HideAllPanels()
    {
        mainMenu.SetActive(false); pausePanel.SetActive(false); diaryPanel.SetActive(false);
        settingsPanel.SetActive(false); endPanel.SetActive(false); hud.SetActive(false);
    }

    void ShowPanel(GameObject p, bool refreshDiary)
    {
        HideAllPanels();
        if (refreshDiary) RefreshDiary();
        if (p == settingsPanel) RefreshSettingsText();
        p.SetActive(true);
    }

    public void ShowMainMenu(bool hasSave)
    {
        HideAllPanels();
        continueBtn.interactable = hasSave;
        mainMenu.SetActive(true);
    }

    public void ShowHud() { HideAllPanels(); hud.SetActive(true); }
    public void ShowPause() { ShowPanel(pausePanel, false); }
    public void ShowEnd() { GameManager.Instance.Pause(); ShowPanelOnly(endPanel); }

    void ShowPanelOnly(GameObject p) { HideAllPanels(); p.SetActive(true); }

    void RefreshDiary()
    {
        for (int i = 0; i < 7; i++)
        {
            bool done = SaveSystem.Data.memoryStates[i] == (int)MemoryState.Restored;
            diaryName[i].text = done ? MemoryNames[i] : "?";
        }
        diaryReplay[0].SetActive(SaveSystem.Data.memoryStates[0] == (int)MemoryState.Restored);
    }

    void RefreshSettingsText()
    {
        sensLabel.text = "Чувствительность камеры: " + ControlSettings.lookSensitivity.ToString("0.00");
        volLabel.text = "Громкость: " + Mathf.RoundToInt(ControlSettings.masterVolume * 100f) + "%";
        leftBtnText.text = "Режим левши: " + (ControlSettings.leftHanded ? "вкл" : "выкл");
        vibBtnText.text = "Вибрация: " + (ControlSettings.vibration ? "вкл" : "выкл");
    }

    // ---------- Субтитры, подсказки, затемнение ----------
    public void SetSubtitle(string text)
    {
        subtitleBox.SetActive(!string.IsNullOrEmpty(text));
        subtitleText.text = text ?? "";
    }

    public void ShowSkip(bool visible, UnityAction onClick)
    {
        skipBtn.SetActive(visible);
        var b = skipBtn.GetComponent<Button>();
        b.onClick.RemoveAllListeners();
        if (onClick != null) b.onClick.AddListener(onClick);
    }

    public void Toast(string text, float seconds)
    {
        if (toastRoutine != null) StopCoroutine(toastRoutine);
        toastRoutine = StartCoroutine(ToastRoutine(text, seconds));
    }

    IEnumerator ToastRoutine(string text, float seconds)
    {
        toastText.text = text;
        toastBox.SetActive(true);
        yield return new WaitForSecondsRealtime(seconds);
        toastBox.SetActive(false);
    }

    public IEnumerator FadeTo(float alpha, float seconds)
    {
        float start = fade.color.a, t = 0f;
        while (t < seconds)
        {
            t += Time.unscaledDeltaTime;
            fade.color = new Color(0, 0, 0, Mathf.Lerp(start, alpha, t / seconds));
            yield return null;
        }
        fade.color = new Color(0, 0, 0, alpha);
    }

    public void ShowCenter(string text) { centerText.text = text; centerText.gameObject.SetActive(true); }
    public void HideCenter() { centerText.gameObject.SetActive(false); }

    // ---------- Помощники построения (public: ими пользуются загадки) ----------
    public RectTransform Rect(string name, Transform parent, Vector2 anchor, Vector2 pos, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var r = go.GetComponent<RectTransform>();
        r.SetParent(parent, false);
        r.anchorMin = r.anchorMax = r.pivot = anchor;
        r.anchoredPosition = pos;
        r.sizeDelta = size;
        return r;
    }

    RectTransform Stretch(string name, Transform parent)
    {
        var r = Rect(name, parent, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        Fullscreen(r);
        return r;
    }

    static void Fullscreen(RectTransform r)
    {
        r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one;
        r.offsetMin = Vector2.zero; r.offsetMax = Vector2.zero;
    }

    GameObject Panel(string name, Color c)
    {
        var img = Img(name, canvasRoot, Vector2.zero, Vector2.zero, Vector2.zero, c, null, true);
        Fullscreen(img.rectTransform);
        return img.gameObject;
    }

    public Image Img(string name, Transform parent, Vector2 anchor, Vector2 pos, Vector2 size, Color c, Sprite s = null, bool raycast = false)
    {
        var r = Rect(name, parent, anchor, pos, size);
        var img = r.gameObject.AddComponent<Image>();
        img.sprite = s; img.color = c; img.raycastTarget = raycast;
        return img;
    }

    public Text Txt(string name, Transform parent, Vector2 anchor, Vector2 pos, Vector2 size, string s, int fontSize, TextAnchor align, Color color)
    {
        var r = Rect(name, parent, anchor, pos, size);
        var t = r.gameObject.AddComponent<Text>();
        t.font = font; t.text = s; t.fontSize = fontSize; t.alignment = align; t.color = color;
        t.raycastTarget = false;
        t.horizontalOverflow = HorizontalWrapMode.Wrap;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        return t;
    }

    public Button Btn(string name, Transform parent, Vector2 anchor, Vector2 pos, Vector2 size, string label, UnityAction onClick, int fontSize = 44)
    {
        var img = Img(name, parent, anchor, pos, size, new Color(0.14f, 0.14f, 0.17f, 0.88f), null, true);
        var b = img.gameObject.AddComponent<Button>();
        b.targetGraphic = img;
        if (onClick != null) b.onClick.AddListener(onClick);
        Txt("Label", img.transform, new Vector2(0.5f, 0.5f), Vector2.zero, size, label, fontSize, TextAnchor.MiddleCenter, Color.white);
        return b;
    }

    void MakeSlider(Transform parent, Vector2 pos, float min, float max, float value, UnityAction<float> onChange)
    {
        var root = Rect("Slider", parent, new Vector2(0.5f, 0.5f), pos, new Vector2(600f, 70f));
        Img("Bg", root, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(600f, 16f), new Color(1, 1, 1, 0.25f), null, true);

        var fillArea = Rect("FillArea", root, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(600f, 16f));
        var fill = Img("Fill", fillArea, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(10f, 16f), new Color(1f, 0.92f, 0.7f, 0.9f));
        fill.rectTransform.anchorMin = Vector2.zero; fill.rectTransform.anchorMax = new Vector2(0f, 1f);
        fill.rectTransform.pivot = new Vector2(0f, 0.5f);
        fill.rectTransform.sizeDelta = new Vector2(10f, 0f);

        var slideArea = Rect("HandleArea", root, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(600f, 70f));
        slideArea.anchorMin = Vector2.zero; slideArea.anchorMax = Vector2.one;
        slideArea.offsetMin = new Vector2(35f, 0f); slideArea.offsetMax = new Vector2(-35f, 0f);
        var handle = Img("Handle", slideArea, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(70f, 70f), Color.white, circle, true);

        var s = root.gameObject.AddComponent<Slider>();
        s.fillRect = fill.rectTransform;
        s.handleRect = handle.rectTransform;
        s.targetGraphic = handle;
        s.direction = Slider.Direction.LeftToRight;
        s.minValue = min; s.maxValue = max;
        s.value = value;
        s.onValueChanged.AddListener(onChange);
    }

    // ---------- Рисованные спрайты ----------
    static Sprite MakeCircle(int n)
    {
        return MakeSprite(n, (d) => Mathf.Clamp01((1f - d) * n * 0.25f));
    }

    static Sprite MakeRing(int n, float inner, float outer)
    {
        return MakeSprite(n, (d) =>
        {
            float a = Mathf.Clamp01((d - inner) * n * 0.25f) * Mathf.Clamp01((outer - d) * n * 0.25f);
            return a;
        });
    }

    static Sprite MakeVignette(int n)
    {
        return MakeSprite(n, (d) => Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.45f, 1.15f, d)), true);
    }

    // d: расстояние от центра, 0 в центре, 1 на краю по оси
    static Sprite MakeSprite(int n, System.Func<float, float> alpha, bool gray = false)
    {
        var tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
        var px = new Color32[n * n];
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float dx = (x + 0.5f) / n * 2f - 1f, dy = (y + 0.5f) / n * 2f - 1f;
                float a = Mathf.Clamp01(alpha(Mathf.Sqrt(dx * dx + dy * dy)));
                byte c = gray ? (byte)95 : (byte)255;
                px[y * n + x] = new Color32(c, c, gray ? (byte)102 : (byte)255, (byte)(a * 255f));
            }
        tex.SetPixels32(px);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;
        tex.Apply(false, true);   // true: освободить копию в RAM
        return Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f);
    }
}
