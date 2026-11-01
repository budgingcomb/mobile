using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// Загадка 1: повторить мелодию из 4 нот (ГДД 3.4). Обучающая: без штрафа к Забвению.
public class MusicBoxPuzzle : MemoryPuzzle
{
    public AudioClip[] notes;                 // 4 клипа
    public AudioClip wrongClip, rightClip;
    [SerializeField] int[] melody = { 0, 2, 1, 3 };   // та же последовательность в начале колыбельной

    GameObject panel;
    Image[] keyImg = new Image[4];
    Text info;
    int progress;
    bool listening;
    static readonly Color[] KeyColors =
    {
        new Color(0.95f, 0.6f, 0.5f), new Color(0.95f, 0.85f, 0.5f),
        new Color(0.55f, 0.85f, 0.6f), new Color(0.55f, 0.7f, 0.95f)
    };

    public override void Begin(MemoryObject o)
    {
        base.Begin(o);
        if (panel == null) Build();
        panel.SetActive(true);
        progress = 0; listening = false;
        info.text = "Повтори мелодию из четырёх нот";
        for (int i = 0; i < 4; i++) keyImg[i].color = KeyColors[i];
    }

    public override void Cancel()
    {
        StopAllCoroutines();
        listening = false; progress = 0;
        if (panel != null) panel.SetActive(false);
    }

    public override void Hint()
    {
        if (panel == null || !panel.activeSelf || listening) return;
        StartCoroutine(Flash(melody[progress], 1.2f));
    }

    void Build()
    {
        var ui = UIManager.Instance;
        var root = ui.Rect("MusicBoxPanel", ui.Content, new Vector2(0.5f, 0f), new Vector2(0f, 0f), new Vector2(1200f, 700f));
        panel = root.gameObject;

        info = ui.Txt("Info", root, new Vector2(0.5f, 0f), new Vector2(0f, 500f), new Vector2(1100f, 90f),
                      "", 46, TextAnchor.MiddleCenter, Color.white);
        for (int i = 0; i < 4; i++)
        {
            int idx = i;
            var b = ui.Btn("Key" + i, root, new Vector2(0.5f, 0f), new Vector2(-345f + i * 230f, 330f), new Vector2(210f, 210f),
                           (i + 1).ToString(), () => OnKey(idx), 80);
            keyImg[i] = b.GetComponent<Image>();
            keyImg[i].color = KeyColors[i];
        }
        ui.Btn("Replay", root, new Vector2(0.5f, 0f), new Vector2(0f, 120f), new Vector2(620f, 130f),
               "Послушать мелодию", () => { if (!listening) StartCoroutine(PlayMelody()); }, 44);
        panel.SetActive(false);
    }

    void OnKey(int i)
    {
        if (listening) return;
        GameManager.Instance.Sfx(notes[i]);
        StartCoroutine(Flash(i, 0.25f));

        if (i == melody[progress])
        {
            progress++;
            if (progress >= melody.Length) StartCoroutine(Win());
        }
        else
        {
            progress = 0;
            info.text = "Не то. Послушай ещё раз.";   // обучение: без штрафа
            GameManager.Instance.Sfx(wrongClip);
            if (ControlSettings.vibration) Handheld.Vibrate();
        }
    }

    IEnumerator PlayMelody()
    {
        listening = true;
        progress = 0;
        info.text = "Слушай...";
        for (int i = 0; i < melody.Length; i++)
        {
            GameManager.Instance.Sfx(notes[melody[i]]);
            StartCoroutine(Flash(melody[i], 0.4f));
            yield return new WaitForSeconds(0.7f);
        }
        info.text = "Теперь повтори";
        listening = false;
    }

    IEnumerator Win()
    {
        listening = true;
        info.text = "Получилось!";
        GameManager.Instance.Sfx(rightClip);
        yield return new WaitForSeconds(0.9f);
        panel.SetActive(false);
        listening = false;
        owner.CompletePuzzle();
    }

    IEnumerator Flash(int i, float seconds)
    {
        keyImg[i].color = Color.white;
        yield return new WaitForSeconds(seconds);
        keyImg[i].color = KeyColors[i];
    }
}
