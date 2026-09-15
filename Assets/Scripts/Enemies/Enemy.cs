using UnityEngine;

/// <summary>
/// 敌人：Day 1 朴素版 = 朝玩家直线移动。
/// 后续扩展：受击反馈（Day 3）、刷怪曲线（Day 4）、池化（Day 5）。
/// </summary>
public class Enemy : MonoBehaviour
{
    public float Radius = 0.4f;
    public float MoveSpeed = 2.2f;
    public float Hp = 10f;

    private void OnEnable()
    {
        // 向全局名单注册（池化后同样是这套机制：取用时注册、回收时注销）
        if (GameManager.I != null)
        {
            GameManager.I.Enemies.Add(this);
        }
    }

    private void OnDisable()
    {
        if (GameManager.I != null)
        {
            GameManager.I.Enemies.Remove(this);
        }
    }

    private void Update()
    {
        if (GameManager.I == null || GameManager.I.State != GameState.Playing)
        {
            return;
        }

        Transform player = GameBootstrap.Player != null ? GameBootstrap.Player.transform : null;
        if (player == null)
        {
            return;
        }

        Vector3 dir = player.position - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude > 0.0001f)
        {
            transform.position += dir.normalized * (MoveSpeed * Time.deltaTime);
            transform.rotation = Quaternion.LookRotation(dir, Vector3.up);
        }
    }
}
