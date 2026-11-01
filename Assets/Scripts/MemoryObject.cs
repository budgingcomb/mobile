using System;
using UnityEngine;
using UnityEngine.Events;

// Состояния предмета по ГДД (раздел 8.5). В сохранение пишется просто число.
public enum MemoryState { Gray = 0, Touched = 1, HologramPlaying = 2, PuzzleActive = 3, Restored = 4 }

// Вешается на предмет памяти. У предмета должен быть Collider на слое Interactable.
// Один материал на тип предмета: "родной" цвет берётся из материала, серый считается автоматически.
public class MemoryObject : MonoBehaviour
{
    public static Transform Player;   // выставляет Interactor

    [Header("Общее")]
    [SerializeField] string memoryId = "memory_1";
    [SerializeField] float shimmerDistance = 3f;                        // мерцание, когда игрок ближе
    [SerializeField, ColorUsage(false, true)] Color glowColor = new Color(0.9f, 0.8f, 0.5f);
    [SerializeField] float colorReturnSeconds = 2.5f;

    [Header("Заглушки прототипа (потом выключить)")]
    [SerializeField] bool fakeHologram = true;      // true: голограмма "играет" по таймеру
    [SerializeField] float fakeHologramSeconds = 3f;
    [SerializeField] bool fakePuzzle = true;        // true: загадка решается сама по таймеру
    [SerializeField] float fakePuzzleSeconds = 3f;

    [Header("События")]
    public UnityEvent onHologramStart;   // сюда потом подключишь видео
    public UnityEvent onPuzzleStart;
    public UnityEvent onRestored;

    public event Action<MemoryObject, MemoryState> StateChanged;   // для SaveSystem и ForgettingSystem

    public MemoryState State => state;
    public int StateId => (int)state;
    public string MemoryId => memoryId;
    public bool CanBeTouched => state == MemoryState.Gray;

    static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");     // URP
    static readonly int LegacyColorId = Shader.PropertyToID("_Color");       // Standard
    static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");
    static MaterialPropertyBlock mpb;                                        // один на всех, без лишних аллокаций

    Renderer[] rends;
    Color[] original, gray;
    int colorId;
    MemoryState state = MemoryState.Gray;
    float timer, glow, colorT;
    float shownGlow = -1f, shownColorT = -1f;   // что сейчас записано в материал
    bool aimed;

    void Awake()
    {
        rends = GetComponentsInChildren<Renderer>();
        if (mpb == null) mpb = new MaterialPropertyBlock();

        colorId = LegacyColorId;
        if (rends.Length > 0 && rends[0].sharedMaterial.HasProperty(BaseColorId)) colorId = BaseColorId;

        original = new Color[rends.Length];
        gray = new Color[rends.Length];
        for (int i = 0; i < rends.Length; i++)
        {
            var m = rends[i].sharedMaterial;
            original[i] = m.HasProperty(colorId) ? m.GetColor(colorId) : Color.white;
            float l = original[i].grayscale;
            gray[i] = new Color(l, l, l, original[i].a);
        }
        Apply(0f, 0f);
    }

    // ---------- Вызывается Interactor-ом ----------
    public void SetAimed(bool value) => aimed = value;

    public void Touch()
    {
        if (!CanBeTouched) return;
        glow = 0f;
        Apply(0f, 0f);
        SetState(MemoryState.Touched);
        SetState(MemoryState.HologramPlaying);   // в прототипе сразу идём дальше
    }

    // ---------- Вызывать из видеоплеера и загадки ----------
    public void FinishHologram()
    {
        if (state == MemoryState.HologramPlaying) SetState(MemoryState.PuzzleActive);
    }

    public void CompletePuzzle()
    {
        if (state == MemoryState.PuzzleActive) SetState(MemoryState.Restored);
    }

    // Для SaveSystem: загрузить состояние без событий. Промежуточные состояния начинаются заново.
    public void LoadState(MemoryState s)
    {
        timer = 0f;
        if (s == MemoryState.Restored)
        {
            state = MemoryState.Restored;
            colorT = 1f; glow = 0f;
            Apply(1f, 0f);
            enabled = false;
        }
        else
        {
            state = MemoryState.Gray;
            colorT = 0f;
            Apply(0f, 0f);
            enabled = true;
        }
    }

    void SetState(MemoryState s)
    {
        state = s;
        timer = 0f;
        switch (s)
        {
            case MemoryState.HologramPlaying: onHologramStart.Invoke(); break;
            case MemoryState.PuzzleActive:    onPuzzleStart.Invoke();   break;
            case MemoryState.Restored:        onRestored.Invoke();      break;
        }
        StateChanged?.Invoke(this, s);
    }

    void Update()
    {
        float dt = Time.deltaTime;
        switch (state)
        {
            case MemoryState.Gray:
                UpdateGlow(dt);
                break;

            case MemoryState.HologramPlaying:
                if (!fakeHologram) break;
                timer += dt;
                if (timer >= fakeHologramSeconds) FinishHologram();
                break;

            case MemoryState.PuzzleActive:
                if (!fakePuzzle) break;
                timer += dt;
                if (timer >= fakePuzzleSeconds) CompletePuzzle();
                break;

            case MemoryState.Restored:
                if (colorT < 1f)
                {
                    colorT = Mathf.MoveTowards(colorT, 1f, dt / colorReturnSeconds);
                    Apply(colorT, 0f);
                }
                else enabled = false;   // всё закончено, Update больше не вызывается
                break;
        }
    }

    // Мерцание рядом с игроком и ровное свечение, когда на предмет смотрит прицел
    void UpdateGlow(float dt)
    {
        float target = 0f;
        if (aimed) target = 1f;
        else if (Player != null)
        {
            float sqr = (Player.position - transform.position).sqrMagnitude;
            if (sqr < shimmerDistance * shimmerDistance)
                target = 0.3f + 0.2f * Mathf.Sin(Time.time * 3f);
        }
        glow = Mathf.MoveTowards(glow, target, 4f * dt);
        Apply(0f, glow);
    }

    // Пишет в материал только если значение реально изменилось
    void Apply(float c, float g)
    {
        if (Mathf.Abs(c - shownColorT) < 0.002f && Mathf.Abs(g - shownGlow) < 0.002f) return;
        shownColorT = c; shownGlow = g;
        Color emission = glowColor * g;
        for (int i = 0; i < rends.Length; i++)
        {
            rends[i].GetPropertyBlock(mpb);
            mpb.SetColor(colorId, Color.Lerp(gray[i], original[i], c));
            mpb.SetColor(EmissionId, emission);
            rends[i].SetPropertyBlock(mpb);
        }
    }
}
