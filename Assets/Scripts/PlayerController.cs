using UnityEngine;
using UnityEngine.InputSystem;

// Ходьба игрока (Айбека). Без бега и прыжков. Вешается на объект с CharacterController.
// Поворот тела (yaw) делает TouchLook, здесь только перемещение.
[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [SerializeField] FloatingJoystick joystick;
    [SerializeField] float walkSpeed = 2.2f;      // м/с, комната маленькая, ходим неспешно
    [SerializeField] float acceleration = 10f;    // плавный разгон (против укачивания)
    [SerializeField] float gravity = -9.81f;

    CharacterController cc;
    Vector3 planarVelocity;   // текущая скорость по полу
    float verticalVelocity;   // скорость по вертикали (гравитация)

    void Awake() => cc = GetComponent<CharacterController>();

    void OnDisable() { planarVelocity = Vector3.zero; } // на паузе/в кат-сцене не "доезжаем" по инерции

    void Update()
    {
        Vector2 input = joystick != null ? joystick.Value : Vector2.zero;

        // Клавиатура только для проверки в редакторе (WASD)
        if (input == Vector2.zero && Keyboard.current != null)
        {
            var k = Keyboard.current;
            input.x = (k.dKey.isPressed ? 1f : 0f) - (k.aKey.isPressed ? 1f : 0f);
            input.y = (k.wKey.isPressed ? 1f : 0f) - (k.sKey.isPressed ? 1f : 0f);
            input = Vector2.ClampMagnitude(input, 1f);
        }

        // Направление относительно взгляда игрока; чем дальше палец, тем быстрее идём
        Vector3 target = (transform.right * input.x + transform.forward * input.y) * walkSpeed;

        // Плавно меняем скорость, а не прыгаем сразу
        planarVelocity = Vector3.MoveTowards(planarVelocity, target, acceleration * Time.deltaTime);

        // Гравитация: на земле держим небольшой прижим, чтобы isGrounded не мигал
        if (cc.isGrounded && verticalVelocity < 0f) verticalVelocity = -2f;
        verticalVelocity += gravity * Time.deltaTime;

        cc.Move((planarVelocity + Vector3.up * verticalVelocity) * Time.deltaTime);
    }
}
