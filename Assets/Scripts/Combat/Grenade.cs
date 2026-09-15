using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 手雷（Day 7 新武器）：从玩家飞向目标点，到达后范围爆炸并回池。
/// 查询走空间哈希（三件套②）+ CircleHit 精筛。
/// </summary>
public class Grenade : MonoBehaviour
{
    public float Speed = 8f;
    public float BlastRadius = 3f; // Day 8 配平：2m → 3m（用户反馈范围偏小）

    private Vector3 _from;
    private Vector3 _to;
    private float _damage;
    private bool _active;
    private float _travel;

    /// <summary>查询候选缓冲（静态共享：主线程串行执行）</summary>
    private static readonly List<Enemy> Candidates = new List<Enemy>();

    private void OnEnable()
    {
        _active = false; // 池取出后、Launch 前静止
    }

    public void Launch(Vector3 from, Vector3 to, float damage)
    {
        _from = from;
        _to = to;
        _damage = damage;
        _travel = 0f;
        _active = true;
        transform.position = from;
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

        float total = Vector3.Distance(_from, _to);
        _travel += Speed * Time.deltaTime;
        float k = total > 0.01f ? Mathf.Clamp01(_travel / total) : 1f;
        transform.position = Vector3.Lerp(_from, _to, k);

        if (k >= 1f)
        {
            Explode();
        }
    }

    private void Explode()
    {
        _active = false;

        // 爆炸视觉（Day 8）：扩散渐隐圈
        BlastEffect.Show(transform.position, BlastRadius);

        // AOE：空间哈希邻近查询 → CircleHit 精筛
        GameManager.I.Grid.Query(transform.position, BlastRadius + 0.45f, Candidates);
        for (int i = 0; i < Candidates.Count; i++)
        {
            var e = Candidates[i];
            if (e == null || !e.gameObject.activeSelf)
            {
                continue;
            }
            if (CircleHit.Hit(transform.position, BlastRadius, e.transform.position, e.Radius))
            {
                Vector3 dir = e.transform.position - transform.position;
                dir.y = 0f;
                e.TakeDamage(_damage, dir); // 爆炸把怪向四周击退
            }
        }

        GameBootstrap.GrenadePool.Release(this);
    }
}
