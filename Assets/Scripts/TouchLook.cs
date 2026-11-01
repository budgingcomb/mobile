using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;

// Поворот камеры свайпом. Вешается на игрока (тело крутится по горизонтали),
// cameraPivot - дочерний объект с камерой (наклон вверх-вниз).
public class TouchLook : MonoBehaviour
{
    public Transform cameraPivot;
    public Camera cam;
    [SerializeField] float fieldOfView = 70f;   // по ГДД, против укачивания
    [SerializeField] float maxPitch = 80f;

    float pitch;
    int lookFinger = -1;

    void Awake()
    {
        // Камера должна стоять ровно в центре головы, иначе поворот идёт по дуге
        var p = cameraPivot.localPosition;
        cameraPivot.localPosition = new Vector3(0f, p.y, 0f);
        if (cam != null && cam.transform != cameraPivot)
        {
            cam.transform.SetParent(cameraPivot, false);
            cam.transform.localPosition = Vector3.zero;
            cam.transform.localRotation = Quaternion.identity;
        }
        ResetView();
        if (cam != null) cam.fieldOfView = fieldOfView;
    }

    public void ResetView()
    {
        pitch = 0f;
        cameraPivot.localRotation = Quaternion.identity;
    }

    void OnDisable() { lookFinger = -1; }

    bool InMyHalf(Vector2 p)
    {
        bool right = p.x > Screen.width * 0.5f;
        return ControlSettings.leftHanded ? !right : right;
    }

    void Update()
    {
        foreach (var t in Touch.activeTouches)
        {
            if (t.phase == TouchPhase.Began && lookFinger == -1 && InMyHalf(t.startScreenPosition)
                && !(EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(t.touchId))
                && !Interactor.WantsTouch(t.startScreenPosition))
                lookFinger = t.finger.index;

            if (t.finger.index != lookFinger) continue;

            if (t.phase == TouchPhase.Moved) Rotate(t.delta);
            if (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled) lookFinger = -1;
        }

        // Мышь (правая кнопка) только для проверки в редакторе
        if (Touch.activeTouches.Count == 0 && Mouse.current != null && Mouse.current.rightButton.isPressed)
            Rotate(Mouse.current.delta.ReadValue());
    }

    void Rotate(Vector2 delta)
    {
        float s = ControlSettings.lookSensitivity;
        transform.Rotate(0f, delta.x * s, 0f);
        pitch = Mathf.Clamp(pitch - delta.y * s, -maxPitch, maxPitch);
        cameraPivot.localEulerAngles = new Vector3(pitch, 0f, 0f);
    }
}
