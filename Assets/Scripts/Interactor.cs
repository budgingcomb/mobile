using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.UI;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;

// Вешается на Player. Подсвечивает предмет под прицелом и считает удержание пальца на предмете.
public class Interactor : MonoBehaviour
{
    public Camera cam;
    public LayerMask interactMask;          // слой Interactable (стены блокируют луч, касание сквозь стену невозможно)
    [SerializeField] float reach = 2.5f;
    [SerializeField] float holdSeconds = 1.5f;
    [SerializeField] float ringOffsetDp = 70f;

    static Interactor instance;

    RectTransform ringRoot;
    Image ringFill;
    Collider cachedCol;
    MemoryObject cachedObj;
    MemoryObject aimed, holdTarget;
    int holdFinger = -1;
    float timer, ringOffsetPx;

    public void Bind(RectTransform root, Image fill)
    {
        ringRoot = root; ringFill = fill;
        if (ringRoot != null) ringRoot.gameObject.SetActive(false);
    }

    void OnEnable()
    {
        instance = this;
        MemoryObject.Player = transform;
        ringOffsetPx = ringOffsetDp * (Screen.dpi > 0f ? Screen.dpi / 160f : 2.5f);
    }

    void OnDisable()
    {
        CancelHold();
        SetAimed(null);
        if (instance == this) instance = null;
    }

    // Для джойстика и камеры: true, если палец лёг на предмет, который можно коснуться
    public static bool WantsTouch(Vector2 screenPos)
    {
        if (instance == null) return false;
        var o = instance.RaycastObject(screenPos);
        return o != null && o.CanBeTouched;
    }

    MemoryObject RaycastObject(Vector2 screenPos)
    {
        Ray ray = cam.ScreenPointToRay(screenPos);
        if (!Physics.Raycast(ray, out RaycastHit hit, reach, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            return null;
        if (hit.collider != cachedCol)
        {
            cachedCol = hit.collider;
            cachedObj = ((interactMask.value & (1 << hit.collider.gameObject.layer)) != 0)
                ? hit.collider.GetComponentInParent<MemoryObject>() : null;
        }
        return cachedObj;
    }

    void Update()
    {
        var target = RaycastObject(new Vector2(Screen.width * 0.5f, Screen.height * 0.5f));
        if (target != null && !target.CanBeTouched) target = null;
        SetAimed(target);
        UpdateHold();
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
        bool found = false;
        foreach (var t in Touch.activeTouches)
        {
            if (t.phase == TouchPhase.Began && holdFinger == -1
                && !(EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(t.touchId)))
            {
                var o = RaycastObject(t.startScreenPosition);
                if (o != null && o.CanBeTouched)
                {
                    holdFinger = t.finger.index;
                    holdTarget = o;
                    timer = 0f;
                    if (ringRoot != null) ringRoot.gameObject.SetActive(true);
                }
            }

            if (t.finger.index != holdFinger) continue;
            found = true;

            if (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled) { CancelHold(); break; }

            if (RaycastObject(t.screenPosition) != holdTarget || !holdTarget.CanBeTouched) { CancelHold(); break; }

            timer += Time.deltaTime;
            if (ringRoot != null)
            {
                ringRoot.position = t.screenPosition + Vector2.up * ringOffsetPx;
                ringFill.fillAmount = timer / holdSeconds;
            }

            if (timer >= holdSeconds)
            {
                var done = holdTarget;
                CancelHold();
                done.Touch();
            }
            break;
        }
        if (holdFinger != -1 && !found) CancelHold();
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
