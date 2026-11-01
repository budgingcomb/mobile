using UnityEngine;

// Проигрывает воспоминание. Сейчас: силуэт мамы из примитивов + колыбельная + субтитры.
// Потом можно заменить на видео: тот же Play/Stop/Finish и тот же вызов owner.FinishHologram().
public class HologramPlayer : MonoBehaviour
{
    public GameObject visual;       // силуэт, выключен по умолчанию
    public AudioSource source;      // на объекте силуэта
    public AudioClip clip;

    static readonly float[] LineTimes = { 0f, 4f, 10f, 18f, 24f, 28f };
    static readonly string[] Lines =
    {
        "(Тихо звучит колыбельная)",
        "Мама: Спи, мой Айбек. Я рядом.",
        "Мама: Слушай нашу мелодию. Всего четыре нотки.",
        "(Четыре ноты повторяются снова и снова)",
        "Мама: Дом всегда помнит тех, кого любит.",
        "Мама: Спокойной ночи, солнышко."
    };

    MemoryObject owner;
    bool playing, replay;
    float t, length;
    int lineIndex;
    Vector3 basePos;

    public bool IsPlaying => playing;

    public void Play(MemoryObject o, bool isReplay)
    {
        if (playing) Stop();
        owner = o; replay = isReplay;
        playing = true; t = 0f; lineIndex = -1;
        length = clip != null ? clip.length : 8f;
        basePos = visual.transform.position;
        visual.transform.localScale = Vector3.one * 0.01f;
        visual.SetActive(true);
        if (clip != null) { source.clip = clip; source.Play(); }

        GameManager.Instance.forgetting.paused = true;           // пока идёт воспоминание, Забвение стоит
        bool canSkip = replay || SaveSystem.Data.holoSeen[owner.memoryIndex];
        UIManager.Instance.ShowSkip(canSkip, Finish);            // пропуск только после первого просмотра
    }

    void Update()
    {
        if (!playing) return;
        t += Time.deltaTime;

        // Плавное появление и исчезновение, лёгкое покачивание
        float k = Mathf.Clamp01(Mathf.Min(t, length - t) / 1.2f);
        visual.transform.localScale = Vector3.one * Mathf.Lerp(0.01f, 1f, Mathf.SmoothStep(0f, 1f, k));
        visual.transform.position = basePos + Vector3.up * (0.03f * Mathf.Sin(t * 1.5f));

        // Субтитры по времени
        int idx = lineIndex;
        while (idx + 1 < LineTimes.Length && t >= LineTimes[idx + 1]) idx++;
        if (idx != lineIndex) { lineIndex = idx; UIManager.Instance.SetSubtitle(Lines[idx]); }

        if (t >= length) Finish();
    }

    void Finish()
    {
        if (!playing) return;
        End();
        if (!replay)
        {
            SaveSystem.Data.holoSeen[owner.memoryIndex] = true;
            owner.FinishHologram();
        }
    }

    public void Stop()
    {
        if (playing) End();
    }

    void End()
    {
        playing = false;
        visual.transform.position = basePos;
        visual.SetActive(false);
        if (source != null) source.Stop();
        UIManager.Instance.SetSubtitle(null);
        UIManager.Instance.ShowSkip(false, null);
        GameManager.Instance.forgetting.paused = false;
    }
}
