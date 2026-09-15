using UnityEngine;

/// <summary>
/// 投射物（飞刀子弹）：直线飞行，射程耗尽或命中敌人后消失。
/// Day 2 朴素版：Physics 触发命中 + Instantiate/Destroy（Day 5 改为手算碰撞 + 对象池）。
/// </summary>
public class Projectile : MonoBehaviour
{
    public float speed = 12f;
    public float range = 6f;
    public float damage = 10f;

    private Vector3 _dir;
    private Vector3 _start;
    private bool _active;

    /// <summary>发射：由武器调用。</summary>
    public void Launch(Vector3 dir, float dmg)
    {
        _dir = dir;
        damage = dmg;
        _start = transform.position;
        _active = true;
    }

    private void Update()
    {
        if (!_active)
        {
            return;
        }
        if (GameManager.I == null || GameManager.I.State != GameState.Playing)
        {
            return;
        }

        transform.position += _dir * (speed * Time.deltaTime);

        // 超出射程 → 回收
        if ((transform.position - _start).sqrMagnitude > range * range)
        {
            Despawn();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!_active)
        {
            return;
        }

        var enemy = other.GetComponent<Enemy>();
        if (enemy == null)
        {
            return;
        }

        enemy.TakeDamage(damage, _dir);
        Despawn();
    }

    private void Despawn()
    {
        _active = false;
        Destroy(gameObject); // Day 5 改为池回收
    }
}
