using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;

// Плавающий джойстик: появляется там, где палец коснулся своей половины экрана.
// Картинки создаёт UIManager и передаёт через Bind. EnhancedTouch включает GameManager.
public class FloatingJoystick : MonoBehaviour
{
    [SerializeField] float radiusDp = 60f;
    [SerializeField, Range(0f, 0.5f)] float deadZone = 0.1f;

    public Vector2 Value { get; private set; }   // длина 0..1, читает PlayerController

    RectTransform background, handle;
    int finger = -1;
    Vector2 origin;
    float radiusPx;

    public void Bind(RectTransform bg, RectTransform h)
    {
        background = bg; handle = h;
        Hide();
    }

    void OnEnable()
    {
        radiusPx = radiusDp * (Screen.dpi > 0f ? Screen.dpi / 160f : 2.5f);
        Hide();
    }

    void OnDisable() { Hide(); }

    bool InMyHalf(Vector2 p)
    {
        bool left = p.x < Screen.width * 0.5f;
        return ControlSettings.leftHanded ? !left : left;
    }

    void Update()
    {
        if (background == null) return;
        foreach (var t in Touch.activeTouches)
        {
            if (t.phase == TouchPhase.Began && finger == -1 && InMyHalf(t.startScreenPosition)
                && !(EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(t.touchId))
                && !Interactor.WantsTouch(t.startScreenPosition))
            {
                finger = t.finger.index;
                origin = t.screenPosition;
                background.position = origin;
                handle.position = origin;
                background.gameObject.SetActive(true);
                handle.gameObject.SetActive(true);
            }

            if (t.finger.index != finger) continue;

            if (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled)
            {
                Hide();
                continue;
            }

            Vector2 offset = Vector2.ClampMagnitude(t.screenPosition - origin, radiusPx);
            Vector2 v = offset / radiusPx;
            handle.position = origin + offset;

            float m = v.magnitude;
            Value = m < deadZone ? Vector2.zero : v.normalized * ((m - deadZone) / (1f - deadZone));
        }
    }

    void Hide()
    {
        finger = -1;
        Value = Vector2.zero;
        if (background != null) background.gameObject.SetActive(false);
        if (handle != null) handle.gameObject.SetActive(false);
    }
}
