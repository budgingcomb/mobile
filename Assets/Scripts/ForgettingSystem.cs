using System;
using UnityEngine;
using UnityEngine.UI;

// Шкала Забвения 0..1 (ГДД 3.3). Показывается через мир: виньетка, шёпот, туман, мерцание света, сердцебиение.
public class ForgettingSystem : MonoBehaviour
{
    public AudioSource whisper, heartbeat, ambient;
    public Light mainLight;
    [HideInInspector] public Image vignette;     // выставляет UIManager
    public bool paused;                          // голограмма, пауза, меню

    public float Value { get; private set; } = 0.2f;
    public event Action Defeated;

    const float TimeRate = 0.04f / 60f;          // ГДД: ~4% в минуту от времени
    float shown, baseLight, ambientBase;
    bool defeated;

    void Start()
    {
        baseLight = mainLight != null ? mainLight.intensity : 1f;
        ambientBase = ambient != null ? ambient.volume : 0.3f;
        shown = Value;
        foreach (var s in new[] { whisper, heartbeat, ambient })
            if (s != null && !s.isPlaying) s.Play();
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogColor = new Color(0.42f, 0.42f, 0.45f);
    }

    public void Add(float d)
    {
        if (defeated) return;
        Value = Mathf.Clamp01(Value + d);
        if (Value >= 1f)
        {
            defeated = true;
            Defeated?.Invoke();
        }
    }

    public void ResetTo(float v)
    {
        defeated = false;
        Value = Mathf.Clamp01(v);
    }

    void Update()
    {
        if (!paused && !defeated) Add(TimeRate * Time.deltaTime);

        // Показываемое значение догоняет реальное плавно, поэтому эффекты не дёргаются
        shown = Mathf.MoveTowards(shown, Value, 0.6f * Time.unscaledDeltaTime);
        float s = shown;

        float vig = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.25f, 0.6f, s)) * 0.55f
                  + Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.6f, 0.9f, s)) * 0.4f;
        if (vignette != null) vignette.color = new Color(1f, 1f, 1f, vig);

        float fog = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.6f, 0.95f, s));
        RenderSettings.fog = fog > 0.01f;
        RenderSettings.fogDensity = fog * 0.18f;

        if (whisper != null) whisper.volume = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.3f, 0.5f, s)) * 0.5f;
        if (heartbeat != null) heartbeat.volume = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.85f, 0.95f, s)) * 0.9f;
        float duck = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.6f, 0.85f, s));
        if (ambient != null) ambient.volume = ambientBase * (1f - 0.8f * duck);

        if (mainLight != null)
        {
            float flick = Mathf.PerlinNoise(Time.time * 9f, 0.3f) > 0.72f ? 1f : 0f;
            mainLight.intensity = baseLight * (1f - 0.45f * duck * flick);
        }
    }
}
