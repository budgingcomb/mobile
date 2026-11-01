using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;

// Плавающий джойстик: появляется там, где палец коснулся "своей" половины экрана.
// Canvas должен быть Screen Space - Overlay. У картинок джойстика отключи Raycast Target.
public class FloatingJoystick : MonoBehaviour
{
    [SerializeField] RectTransform background;   // круг-основа
    [SerializeField] RectTransform handle;       // ручка внутри круга
    [SerializeField] float radiusDp = 60f;       // радиус хода ручки в dp
    [SerializeField, Range(0f, 0.5f)] float deadZone = 0.1f;

    public Vector2 Value { get; private set; }   // длина 0..1, читает PlayerController

    int finger = -1;          // номер пальца, который держит джойстик
    Vector2 origin;           // где палец коснулся
    float radiusPx;

    void OnEnable()
    {
        EnhancedTouchSupport.Enable();
        radiusPx = radiusDp * Screen.dpi / 160f;           // dp -> пиксели
        if (Screen.dpi <= 0f) radiusPx = radiusDp * 2.5f;  // запасной вариант, если dpi неизвестен
        Hide();
    }

    void OnDisable()
    {
        EnhancedTouchSupport.Disable();
        Hide();
    }

    // true, если точка в нашей половине экрана (учитывает режим левши)
    bool InMyHalf(Vector2 p)
    {
        bool left = p.x < Screen.width * 0.5f;
        return ControlSettings.leftHanded ? !left : left;
    }

    void Update()
    {
        foreach (var t in Touch.activeTouches)
        {
            // Новый палец в нашей половине и не на кнопке интерфейса -> берём его
            if (t.phase == TouchPhase.Began && finger == -1 && InMyHalf(t.startScreenPosition)
                && !(EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(t.touchId))
                && !Interactor.WantsTouch(t.startScreenPosition))  // палец на предмете: это не джойстик/камера
            {
                finger = t.finger.index;
                origin = t.screenPosition;
                background.position = origin;
                background.gameObject.SetActive(true);
            }

            if (t.finger.index != finger) continue;  // чужие пальцы игнорируем

            if (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled)
            {
                Hide();
                continue;
            }

            // Смещение пальца от точки касания, ограниченное радиусом
            Vector2 offset = Vector2.ClampMagnitude(t.screenPosition - origin, radiusPx);
            Vector2 v = offset / radiusPx;
            handle.position = origin + offset;

            // Мёртвая зона: маленькие дрожания пальца не двигают игрока
            float m = v.magnitude;
            Value = m < deadZone ? Vector2.zero : v.normalized * ((m - deadZone) / (1f - deadZone));
        }
    }

    void Hide()
    {
        finger = -1;
        Value = Vector2.zero;
        if (background != null) background.gameObject.SetActive(false);
    }
}
