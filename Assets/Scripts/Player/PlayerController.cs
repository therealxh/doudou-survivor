using UnityEngine;

/// <summary>
/// 玩家：键盘（WASD/方向键）输入 → 平面移动。
/// Day 2 起加入摇杆输入合并。
/// </summary>
public class PlayerController : MonoBehaviour
{
    public float MoveSpeed = 5f;
    public float Radius = 0.5f;

    private void Update()
    {
        Vector2 dir = ReadInput();
        if (dir.sqrMagnitude > 1f)
        {
            dir.Normalize(); // 斜向不加速
        }

        Vector3 move = new Vector3(dir.x, 0f, dir.y);
        transform.position += move * (MoveSpeed * Time.deltaTime);

        // 移动时转向移动方向（视觉反馈）
        if (move.sqrMagnitude > 0.01f)
        {
            transform.rotation = Quaternion.LookRotation(move, Vector3.up);
        }
    }

    private Vector2 ReadInput()
    {
        float x = Input.GetAxisRaw("Horizontal");
        float y = Input.GetAxisRaw("Vertical");
        return new Vector2(x, y);
    }
}
