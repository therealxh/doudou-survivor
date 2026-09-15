using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 投射物（飞刀子弹）：直线飞行，手算命中（CircleHit），射程耗尽或命中后回池。
/// Day 5 起不再使用 Physics（Rigidbody/Collider 全部移除）。
/// </summary>
public class Projectile : MonoBehaviour
{
    public float speed = 12f;
    public float range = 6f;
    public float damage = 10f;
    public float Radius = 0.2f; // 命中判定半径

    private Vector3 _dir;
    private Vector3 _start;
    private bool _active;

    /// <summary>查询候选缓冲（静态共享：子弹在主线程串行执行，无重入）</summary>
    private static readonly List<Enemy> Candidates = new List<Enemy>();

    private void OnEnable()
    {
        _active = false; // 池取出后、Launch 前保持静止
    }

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

        // 手算命中：空间哈希邻近查询（Day 6 起，替代全表遍历）→ CircleHit 精筛。
        // 命中集合中选【最近】的一只：与武器索敌选择一致，保证火力集中
        // （否则每发打在随机一只身上、伤害雨露均沾，高血量怪永远打不死）。
        GameManager.I.Grid.Query(transform.position, Radius + 0.45f, Candidates);

        Enemy hit = null;
        float bestSqr = float.MaxValue;
        for (int i = 0; i < Candidates.Count; i++)
        {
            var e = Candidates[i];
            if (e == null || !e.gameObject.activeSelf)
            {
                continue; // 已回收（可能被本帧早先的子弹击杀）
            }
            if (CircleHit.Hit(transform.position, Radius, e.transform.position, e.Radius))
            {
                float d = (e.transform.position - transform.position).sqrMagnitude;
                if (d < bestSqr)
                {
                    bestSqr = d;
                    hit = e;
                }
            }
        }
        if (hit != null)
        {
            hit.TakeDamage(damage, _dir);
            Despawn();
            return;
        }

        // 超出射程 → 回池
        if ((transform.position - _start).sqrMagnitude > range * range)
        {
            Despawn();
        }
    }

    private void Despawn()
    {
        _active = false;
        GameBootstrap.KnifePool.Release(this);
    }
}
