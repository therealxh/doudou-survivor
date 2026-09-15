using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 环绕飞刃实体（Day 8 新武器）：绕玩家匀速旋转；每 0.4s 对接触到的敌人扫击一次。
/// 数量 / 伤害 / 半径由 WeaponOrbit 定期同步。
/// </summary>
public class Orbiter : MonoBehaviour
{
    public float DamagePerHit = 3f;
    public float HitInterval = 0.4f;
    public float OrbitRadius = 1.7f;
    public float OrbitSpeed = 200f;  // 度/秒
    public float Radius = 0.35f;     // 命中判定半径
    public float PhaseDeg;           // 初始相位（多枚均分，由 WeaponOrbit 设置）

    private float _angle;
    private float _hitTimer;

    private static readonly List<Enemy> Candidates = new List<Enemy>();

    private void OnEnable()
    {
        _hitTimer = 0f;
    }

    /// <summary>把当前相位应用到实际角度（池取出 / 均分入场时调用）。</summary>
    public void ApplyPhase()
    {
        _angle = PhaseDeg;
    }

    private void Update()
    {
        if (GameManager.I == null || GameManager.I.State != GameState.Playing)
        {
            return;
        }
        var player = GameBootstrap.Player;
        if (player == null)
        {
            return;
        }

        // 绕玩家旋转
        _angle += OrbitSpeed * Time.deltaTime;
        float rad = _angle * Mathf.Deg2Rad;
        Vector3 c = player.transform.position;
        transform.position = new Vector3(c.x + Mathf.Cos(rad) * OrbitRadius, 0.5f, c.z + Mathf.Sin(rad) * OrbitRadius);
        // 刀尖朝切向（像旋转的刀环，视觉更清晰；细长物自转时投影太弱）
        transform.rotation = GameBootstrap.FlatRotation(new Vector3(-Mathf.Sin(rad), 0f, Mathf.Cos(rad)));

        // 周期性扫击：对接触到的全部敌人各造成一次伤害
        _hitTimer -= Time.deltaTime;
        if (_hitTimer > 0f)
        {
            return;
        }
        _hitTimer = HitInterval;

        GameManager.I.Grid.Query(transform.position, Radius + 0.45f, Candidates);
        for (int i = 0; i < Candidates.Count; i++)
        {
            var e = Candidates[i];
            if (e == null || !e.gameObject.activeSelf)
            {
                continue;
            }
            if (CircleHit.Hit(transform.position, Radius, e.transform.position, e.Radius))
            {
                Vector3 dir = e.transform.position - transform.position;
                dir.y = 0f;
                e.TakeDamage(DamagePerHit, dir);
            }
        }
    }
}
