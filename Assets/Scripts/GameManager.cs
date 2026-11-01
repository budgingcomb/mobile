using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem.EnhancedTouch;

// Главный менеджер: состояние игры, меню и пауза, сохранение, поражение, награда за воспоминание.
public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Ссылки (проставляет сборщик сцены)")]
    public PlayerController player;
    public TouchLook look;
    public FloatingJoystick joystick;
    public Interactor interactor;
    public ForgettingSystem forgetting;
    public UIManager ui;
    public MemoryObject[] memories;
    public AudioSource sfx;
    public AudioClip restoreClip, doorClip;
    public Light warmLight;
    public Transform cupboardHinge;
    public GameObject cupboardLight;
    public Transform playerStart;

    [Header("Свет комнаты")]
    public Color grayAmbient = new Color(0.33f, 0.33f, 0.36f);
    public Color warmAmbient = new Color(0.62f, 0.5f, 0.38f);

    bool playing, paused;
    float rewardT, rewardTarget;
    Coroutine defeatRoutine;

    void Awake()
    {
        Instance = this;
        ControlSettings.Load();
        AudioListener.volume = ControlSettings.masterVolume;
        Application.targetFrameRate = 60;
        QualitySettings.vSyncCount = 0;
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
        Screen.autorotateToPortrait = false;
        Screen.autorotateToPortraitUpsideDown = false;
        Screen.autorotateToLandscapeLeft = true;
        Screen.autorotateToLandscapeRight = true;
        Screen.orientation = ScreenOrientation.AutoRotation;
        EnhancedTouchSupport.Enable();    // включаем один раз для всех скриптов ввода
    }

    void Start()
    {
        SaveSystem.Load();
        foreach (var m in memories) m.StateChanged += OnMemoryState;
        forgetting.Defeated += OnDefeat;
        ApplySave();
        SetInput(false);
        forgetting.paused = true;
        ui.ShowMainMenu(SaveSystem.HasSave);
    }

    // ---------- Запуск, пауза, выход ----------
    public void NewGame()
    {
        SaveSystem.Delete();
        ApplySave();
        TeleportToStart();
        StartPlay();
    }

    public void ContinueGame()
    {
        SaveSystem.Load();
        ApplySave();
        TeleportToStart();
        StartPlay();
    }

    void StartPlay()
    {
        playing = true; paused = false;
        Time.timeScale = 1f; AudioListener.pause = false;
        forgetting.paused = false;
        SetInput(true);
        ui.ShowHud();
    }

    public void Pause()
    {
        if (!playing || paused) return;
        paused = true;
        Time.timeScale = 0f; AudioListener.pause = true;
        SetInput(false);
        ui.ShowPause();
    }

    public void Resume()
    {
        paused = false;
        Time.timeScale = 1f; AudioListener.pause = false;
        SetInput(true);
        ui.ShowHud();
    }

    public void ReturnToMenu()
    {
        SaveNow();
        playing = false; paused = false;
        Time.timeScale = 1f; AudioListener.pause = false;
        SetInput(false);
        foreach (var m in memories) m.ResetToSaved(SavedState(m));
        forgetting.paused = true;
        ui.ShowMainMenu(SaveSystem.HasSave);
    }

    public void Quit() { SaveNow(); Application.Quit(); }

    void SetInput(bool on)
    {
        player.enabled = on; look.enabled = on; joystick.enabled = on; interactor.enabled = on;
    }

    // ---------- Сохранение ----------
    MemoryState SavedState(MemoryObject m) =>
        SaveSystem.Data.memoryStates[m.memoryIndex] == (int)MemoryState.Restored ? MemoryState.Restored : MemoryState.Gray;

    void ApplySave()
    {
        foreach (var m in memories) m.ResetToSaved(SavedState(m));
        forgetting.ResetTo(SaveSystem.Data.forgetting);
        rewardTarget = SaveSystem.Data.memoryStates[0] == (int)MemoryState.Restored ? 1f : 0f;
        rewardT = rewardTarget;
        ApplyReward();
    }

    void SaveNow()
    {
        SaveSystem.Data.forgetting = forgetting.Value;
        SaveSystem.Save();
    }

    void OnApplicationPause(bool p)
    {
        if (!p) return;
        if (playing) { SaveNow(); Pause(); }   // звонок или сворачивание: сохраняем и ставим на паузу
    }

    void OnApplicationQuit() { if (playing) SaveNow(); }

    // ---------- События воспоминаний ----------
    void OnMemoryState(MemoryObject m, MemoryState s)
    {
        if (s != MemoryState.Restored) return;
        forgetting.Add(-0.15f);                                   // ГДД: возвращённое воспоминание снимает 15%
        SaveSystem.Data.memoryStates[m.memoryIndex] = (int)MemoryState.Restored;
        SaveNow();                                                // автосохранение
        Sfx(restoreClip);
        if (m.memoryIndex == 0)
        {
            rewardTarget = 1f;
            StartCoroutine(RewardSequence());
        }
    }

    IEnumerator RewardSequence()
    {
        yield return new WaitForSeconds(1.5f);
        Sfx(doorClip);
        ui.Toast("В шкатулке лежал ключик. В зоне Б открылся шкаф.", 4f);
        yield return new WaitForSeconds(6f);
        if (playing && !paused) ui.ShowEnd();
    }

    void Update()
    {
        if (!Mathf.Approximately(rewardT, rewardTarget))
        {
            rewardT = Mathf.MoveTowards(rewardT, rewardTarget, Time.deltaTime / 3f);
            ApplyReward();
        }
    }

    // Тёплый свет расходится по комнате, шкаф открывается
    void ApplyReward()
    {
        warmLight.intensity = 1.6f * rewardT;
        RenderSettings.ambientLight = Color.Lerp(grayAmbient, warmAmbient, rewardT);
        float open = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((rewardT - 0.4f) / 0.6f));
        cupboardHinge.localRotation = Quaternion.Euler(0f, -100f * open, 0f);
        cupboardLight.SetActive(rewardT > 0.5f);
    }

    // ---------- Подсказка (Лампа) ----------
    public void UseHint()
    {
        if (!playing || paused) return;
        foreach (var m in memories)
        {
            if (m.State == MemoryState.PuzzleActive && m.puzzle != null) { m.puzzle.Hint(); goto done; }
        }
        foreach (var m in memories)
            if (m.State == MemoryState.Gray) { m.Ping(3f); goto done; }
        return;
    done:
        forgetting.Add(0.06f);                                    // ГДД: подсказка стоит +6%
        ui.Toast("Лампа осветила путь", 1.5f);
    }

    public void ReplayMemory(int index)
    {
        foreach (var m in memories)
        {
            if (m.memoryIndex != index || m.hologram == null) continue;
            Resume();
            m.hologram.Play(m, true);     // повтор бесплатный, состояние не меняется
            return;
        }
    }

    public void DemoForgetting() { forgetting.Add(0.2f); }

    // ---------- Поражение (упрощённая версия) ----------
    void OnDefeat()
    {
        if (defeatRoutine == null) defeatRoutine = StartCoroutine(DefeatRoutine());
    }

    IEnumerator DefeatRoutine()
    {
        SetInput(false);
        yield return ui.FadeTo(1f, 1.2f);
        ui.ShowCenter("Забвение поглотило комнату...");
        yield return new WaitForSecondsRealtime(2.5f);

        foreach (var m in memories) m.ResetToSaved(SavedState(m));    // возвращённые воспоминания остаются
        forgetting.ResetTo(0.3f);                                     // ГДД: шкала сбрасывается до 30%
        TeleportToStart();
        ui.HideCenter();
        yield return ui.FadeTo(0f, 1f);
        if (playing && !paused) SetInput(true);
        defeatRoutine = null;
    }

    void TeleportToStart()
    {
        var cc = player.GetComponent<CharacterController>();
        cc.enabled = false;
        player.transform.SetPositionAndRotation(playerStart.position, playerStart.rotation);
        cc.enabled = true;
        look.ResetView();
    }

    public void Sfx(AudioClip c) { if (c != null) sfx.PlayOneShot(c); }
}
