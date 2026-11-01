using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.UI;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;

// Вешается на Player. Две задачи:
// 1) подсвечивает предмет, на который смотрит прицел (центр экрана);
// 2) если игрок зажал палец на предмете 1,5 с, вызывает MemoryObject.Touch().
public class Interactor : MonoBehaviour
{
    [SerializeField] Camera cam;
    [SerializeField] LayerMask interactMask;          // слой Interactable
    [SerializeField] float reach = 2.5f;              // дальность касания, м
    [SerializeField] float holdSeconds = 1.5f;
    [SerializeField] RectTransform ringRoot;          // кольцо удержания (в Canvas)
    [SerializeField] Image ringFill;                  // заполняемая часть кольца
    [SerializeField] float ringOffsetDp = 70f;        // кольцо выше пальца, чтобы палец его не закрывал

    static Interactor instance;

    Collider cachedCol;          // кэш: чтобы не вызывать GetComponent каждый кадр
    MemoryObject cachedObj;
    MemoryObject aimed;          // предмет под прицелом
    MemoryObject holdTarget;     // предмет, который сейчас "касаются"
    int holdFinger = -1;
    float timer, ringOffsetPx;

    void OnEnable()
    {
        instance = this;
        MemoryObject.Player = transform;
        EnhancedTouchSupport.Enable();
        ringOffsetPx = ringOffsetDp * (Screen.dpi > 0f ? Screen.dpi / 160f : 2.5f);
        ringRoot.gameObject.SetActive(false);
    }

    void OnDisable()
    {
        EnhancedTouchSupport.Disable();
        CancelHold();
        SetAimed(null);
        if (instance == this) instance = null;
    }

    // Для джойстика и камеры: true, если палец лёг на предмет, который можно коснуться.
    // Тогда этот палец принадлежит Interactor-у.
    public static bool WantsTouch(Vector2 screenPos)
    {
        if (instance == null) return false;
        var o = instance.RaycastObject(screenPos);
        return o != null && o.CanBeTouched;
    }

    MemoryObject RaycastObject(Vector2 screenPos)
    {
        Ray ray = cam.ScreenPointToRay(screenPos);
        if (!Physics.Raycast(ray, out RaycastHit hit, reach, interactMask, QueryTriggerInteraction.Ignore))
            return null;
        if (hit.collider != cachedCol)
        {
            cachedCol = hit.collider;
            cachedObj = hit.collider.GetComponentInParent<MemoryObject>();
        }
        return cachedObj;
    }

    void Update()
    {
        UpdateAim();
        UpdateHold();
    }

    void UpdateAim()
    {
        var target = RaycastObject(new Vector2(Screen.width * 0.5f, Screen.height * 0.5f));
        if (target != null && !target.CanBeTouched) target = null;
        SetAimed(target);
    }

    void SetAimed(MemoryObject target)
    {
        if (target == aimed) return;
        if (aimed != null) aimed.SetAimed(false);
        aimed = target;
        if (aimed != null) aimed.SetAimed(true);
    }

    void UpdateHold()
    {
        bool holdFingerFound = false;

        foreach (var t in Touch.activeTouches)
        {
            // Новый палец: проверяем, лёг ли он на предмет (и не на кнопку интерфейса)
            if (t.phase == TouchPhase.Began && holdFinger == -1
                && !(EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(t.touchId)))
            {
                var o = RaycastObject(t.startScreenPosition);
                if (o != null && o.CanBeTouched)
                {
                    holdFinger = t.finger.index;
                    holdTarget = o;
                    timer = 0f;
                    ringRoot.gameObject.SetActive(true);
                }
            }

            if (t.finger.index != holdFinger) continue;
            holdFingerFound = true;

            if (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled)
            {
                CancelHold();
                break;
            }

            // Палец ушёл с предмета или предмет вышел за дальность: сброс
            if (RaycastObject(t.screenPosition) != holdTarget || !holdTarget.CanBeTouched)
            {
                CancelHold();
                break;
            }

            timer += Time.deltaTime;
            ringRoot.position = t.screenPosition + Vector2.up * ringOffsetPx;
            ringFill.fillAmount = timer / holdSeconds;

            if (timer >= holdSeconds)
            {
                var done = holdTarget;
                CancelHold();       // сначала сбрасываем кольцо, потом касаемся
                done.Touch();
            }
            break;
        }

        // Палец пропал без Ended (например, прервали звонком)
        if (holdFinger != -1 && !holdFingerFound) CancelHold();
    }

    void CancelHold()
    {
        holdFinger = -1;
        holdTarget = null;
        timer = 0f;
        if (ringFill != null) ringFill.fillAmount = 0f;
        if (ringRoot != null) ringRoot.gameObject.SetActive(false);
    }
}
