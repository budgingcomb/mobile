using System;
using UnityEngine;
using UnityEngine.Events;

// Состояния предмета по ГДД (раздел 8.5). В сохранение пишется число.
public enum MemoryState { Gray = 0, Touched = 1, HologramPlaying = 2, PuzzleActive = 3, Restored = 4 }

// Вешается на предмет памяти. Collider должен быть на слое Interactable.
public class MemoryObject : MonoBehaviour
{
    public static Transform Player;   // выставляет Interactor

    [Header("Связи")]
    public int memoryIndex = 0;          // 0..6, номер в дневнике
    public HologramPlayer hologram;      // если пусто, голограмма идёт по таймеру-заглушке
    public MemoryPuzzle puzzle;          // если пусто, загадка решается по таймеру-заглушке

    [Header("Вид")]
    [SerializeField] float shimmerDistance = 3f;
    [SerializeField, ColorUsage(false, true)] Color glowColor = new Color(0.9f, 0.8f, 0.5f);
    [SerializeField] float colorReturnSeconds = 2.5f;

    [Header("Заглушки (работают, только если нет hologram / puzzle)")]
    [SerializeField] float fakeHologramSeconds = 3f;
    [SerializeField] float fakePuzzleSeconds = 3f;

    [Header("События")]
    public UnityEvent onHologramStart;
    public UnityEvent onPuzzleStart;
    public UnityEvent onRestored;

    public event Action<MemoryObject, MemoryState> StateChanged;

    public MemoryState State => state;
    public bool CanBeTouched => state == MemoryState.Gray;

    static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    static readonly int LegacyColorId = Shader.PropertyToID("_Color");
    static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");
    static MaterialPropertyBlock mpb;

    Renderer[] rends;
    Color[] original, gray;
    int colorId;
    MemoryState state = MemoryState.Gray;
    float timer, glow, colorT, pingTimer;
    float shownGlow = -1f, shownColorT = -1f;
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

    public void SetAimed(bool value) => aimed = value;

    // Подсказка "Лампа": предмет ярко светится несколько секунд
    public void Ping(float seconds) { if (state == MemoryState.Gray) pingTimer = seconds; }

    public void Touch()
    {
        if (!CanBeTouched) return;
        glow = 0f; pingTimer = 0f;
        Apply(0f, 0f);
        SetState(MemoryState.Touched);
        SetState(MemoryState.HologramPlaying);
    }

    public void FinishHologram()
    {
        if (state == MemoryState.HologramPlaying) SetState(MemoryState.PuzzleActive);
    }

    public void CompletePuzzle()
    {
        if (state == MemoryState.PuzzleActive) SetState(MemoryState.Restored);
    }

    // Загрузка состояния без событий; незаконченные состояния начинаются заново с Gray
    public void LoadState(MemoryState s)
    {
        timer = 0f; pingTimer = 0f;
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

    // Для поражения и выхода в меню: остановить голограмму и загадку, вернуть сохранённое состояние
    public void ResetToSaved(MemoryState saved)
    {
        if (hologram != null) hologram.Stop();
        if (puzzle != null) puzzle.Cancel();
        LoadState(saved);
    }

    void SetState(MemoryState s)
    {
        state = s;
        timer = 0f;
        switch (s)
        {
            case MemoryState.HologramPlaying:
                if (hologram != null) hologram.Play(this, false);
                onHologramStart.Invoke();
                break;
            case MemoryState.PuzzleActive:
                if (puzzle != null) puzzle.Begin(this);
                onPuzzleStart.Invoke();
                break;
            case MemoryState.Restored:
                onRestored.Invoke();
                break;
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
                if (hologram != null) break;
                timer += dt;
                if (timer >= fakeHologramSeconds) FinishHologram();
                break;
            case MemoryState.PuzzleActive:
                if (puzzle != null) break;
                timer += dt;
                if (timer >= fakePuzzleSeconds) CompletePuzzle();
                break;
            case MemoryState.Restored:
                if (colorT < 1f)
                {
                    colorT = Mathf.MoveTowards(colorT, 1f, dt / colorReturnSeconds);
                    Apply(colorT, 0f);
                }
                else enabled = false;
                break;
        }
    }

    void UpdateGlow(float dt)
    {
        float target = 0f;
        if (pingTimer > 0f) { pingTimer -= dt; target = 0.7f + 0.3f * Mathf.Sin(Time.time * 8f); }
        else if (aimed) target = 1f;
        else if (Player != null)
        {
            float sqr = (Player.position - transform.position).sqrMagnitude;
            if (sqr < shimmerDistance * shimmerDistance)
                target = 0.3f + 0.2f * Mathf.Sin(Time.time * 3f);
        }
        glow = Mathf.MoveTowards(glow, target, 4f * dt);
        Apply(0f, glow);
    }

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
