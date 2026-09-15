using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 回旋镖实体（Day 8 新武器）：向目标方向飞出 maxRange 后折返玩家；
/// 全程穿透伤害（同一次投掷对每只敌人只结算一次）。
/// </summary>
public class Boomerang : MonoBehaviour
{
    public float Speed = 9f;
    public float Radius = 0.4f;

    private Vector3 _dir;
    private Vector3 _origin;
    private float _maxRange;
    private float _damage;
    private bool _active;
    private bool _returning;

    private readonly HashSet<Enemy> _hitOnce = new HashSet<Enemy>();
    private static readonly List<Enemy> Candidates = new List<Enemy>();

    private void OnEnable()
    {
        _active = false; // 池取出后、Launch 前静止
    }

    public void Launch(Vector3 from, Vector3 dir, float maxRange, float damage)
    {
        _origin = from;
        _dir = dir;
        _maxRange = maxRange;
        _damage = damage;
        _returning = false;
        _active = true;
        _hitOnce.Clear();
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

        var player = GameBootstrap.Player;

        if (!_returning)
        {
            transform.position += _dir * (Speed * Time.deltaTime);
            if ((transform.position - _origin).sqrMagnitude >= _maxRange * _maxRange)
            {
                _returning = true; // 到达最远处：折返
            }
        }
        else
        {
            if (player == null)
            {
                Despawn();
                return;
            }
            Vector3 to = player.transform.position - transform.position;
            to.y = 0f;
            float d = to.magnitude;
            if (d < 0.6f)
            {
                Despawn(); // 回到玩家手中
                return;
            }
            _dir = to / d; // 返程实时朝玩家飞
            transform.position += _dir * (Speed * Time.deltaTime);
        }

        transform.Rotate(0f, 720f * Time.deltaTime, 0f, Space.World); // 旋转
        HitCheck();
    }

    private void HitCheck()
    {
        GameManager.I.Grid.Query(transform.position, Radius + 0.45f, Candidates);
        for (int i = 0; i < Candidates.Count; i++)
        {
            var e = Candidates[i];
            if (e == null || !e.gameObject.activeSelf)
            {
                continue;
            }
            if (_hitOnce.Contains(e))
            {
                continue; // 本次投掷已伤过
            }
            if (CircleHit.Hit(transform.position, Radius, e.transform.position, e.Radius))
            {
                _hitOnce.Add(e);
                Vector3 dir = e.transform.position - transform.position;
                dir.y = 0f;
                e.TakeDamage(_damage, dir);
            }
        }
    }

    private void Despawn()
    {
        _active = false;
        GameBootstrap.BoomerangPool.Release(this);
    }
}
