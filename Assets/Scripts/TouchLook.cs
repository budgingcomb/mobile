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
    [SerializeField] Transform cameraPivot;
    [SerializeField] Camera cam;
    [SerializeField] float fieldOfView = 70f;     // по GDD: 70 градусов против укачивания
    [SerializeField] float maxPitch = 80f;

    float pitch;           // текущий наклон вверх/вниз
    int lookFinger = -1;   // палец, который крутит камеру

    void Awake()
    {
        // Починка "летающего тела": камера должна стоять ровно в центре головы.
        // Если у камеры или у CameraPivot есть смещение, поворот идёт по дуге вокруг чужой точки.
        var p = cameraPivot.localPosition;
        cameraPivot.localPosition = new Vector3(0f, p.y, 0f);   // pivot строго над центром игрока
        if (cam != null && cam.transform != cameraPivot)
        {
            cam.transform.SetParent(cameraPivot, false);        // камера дочерняя к pivot
            cam.transform.localPosition = Vector3.zero;         // без смещения
            cam.transform.localRotation = Quaternion.identity;  // без собственного поворота
        }
        pitch = 0f;
        cameraPivot.localRotation = Quaternion.identity;
    }

    void OnEnable()
    {
        EnhancedTouchSupport.Enable();
        if (cam != null) cam.fieldOfView = fieldOfView;
    }

    void OnDisable()
    {
        EnhancedTouchSupport.Disable();
        lookFinger = -1;
    }

    bool InMyHalf(Vector2 p)
    {
        bool right = p.x > Screen.width * 0.5f;
        return ControlSettings.leftHanded ? !right : right;
    }

    void Update()
    {
        foreach (var t in Touch.activeTouches)
        {
            // Берём палец один раз при касании: в нашей половине и не на кнопке UI
            if (t.phase == TouchPhase.Began && lookFinger == -1 && InMyHalf(t.startScreenPosition)
                && !(EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(t.touchId)))
                lookFinger = t.finger.index;

            if (t.finger.index != lookFinger) continue;

            if (t.phase == TouchPhase.Moved)
                Rotate(t.delta);

            if (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled)
                lookFinger = -1;
        }

        // Мышь с зажатой правой кнопкой, только для проверки в редакторе
        if (Touch.activeTouches.Count == 0 && Mouse.current != null && Mouse.current.rightButton.isPressed)
            Rotate(Mouse.current.delta.ReadValue());
    }

    void Rotate(Vector2 delta)
    {
        float s = ControlSettings.lookSensitivity;
        transform.Rotate(0f, delta.x * s, 0f);                              // влево-вправо
        pitch = Mathf.Clamp(pitch - delta.y * s, -maxPitch, maxPitch);      // вверх-вниз
        cameraPivot.localEulerAngles = new Vector3(pitch, 0f, 0f);
    }
}