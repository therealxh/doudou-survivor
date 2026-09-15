using UnityEngine;

/// <summary>
/// 玩家：键盘（WASD/方向键）输入 → 平面移动。
/// Day 2 起加入摇杆输入合并。
/// </summary>
public class PlayerController : MonoBehaviour
{
    public float MoveSpeed = 5f;
    public float Radius = 0.5f;
    public FloatingJoystick Joystick; // Bootstrap 注入

    private SpriteRenderer _sr;

    private void Awake()
    {
        _sr = GetComponentInChildren<SpriteRenderer>();
    }

    private void Update()
    {
        Vector2 dir = ReadInput();
        if (dir.sqrMagnitude > 1f)
        {
            dir.Normalize(); // 斜向不加速
        }

        Vector3 move = new Vector3(dir.x, 0f, dir.y);
        transform.position += move * (MoveSpeed * Time.deltaTime);

        // 2.5D：朝向用左右翻转表现（sprite 保持直立，不旋转）
        if (move.sqrMagnitude > 0.01f && _sr != null)
        {
            _sr.flipX = move.x < 0f;
        }
    }

    private Vector2 ReadInput()
    {
        // 摇杆优先（触屏/鼠标拖动）；否则读键盘
        Vector2 stick = Joystick != null ? Joystick.Value : Vector2.zero;
        if (stick.sqrMagnitude > 0.01f)
        {
            return stick;
        }

        float x = Input.GetAxisRaw("Horizontal");
        float y = Input.GetAxisRaw("Vertical");
        return new Vector2(x, y);
    }
}
