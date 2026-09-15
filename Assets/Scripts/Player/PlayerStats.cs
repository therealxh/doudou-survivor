using UnityEngine;

/// <summary>
/// 玩家属性与生存状态：HP / 受击无敌帧 / 拾取范围 / 接触伤害判定。
/// Day 5 起接触判定改手算（CircleHit 遍历敌人，弃 Physics 触发）。
/// 被动加成（攻击/移速/拾取）Day 8 加入。
/// </summary>
public class PlayerStats : MonoBehaviour
{
    public float MaxHp = 100f;
    public float Hp { get; private set; }
    public float AttackMult = 1f;  // 攻击力倍率（升级被动加成）
    public float PickupMult = 1f;  // 拾取范围倍率（升级被动加成）
    public float Radius = 0.5f;         // 接触判定半径
    public float PickupRange = 3.5f;    // 磁铁吸附范围（升级被动可放大）
    public float InvulDuration = 0.5f;  // 受击后无敌时间（秒）

    private float _invulTimer;
    private float _flashTimer;
    private SpriteRenderer _sr;

    private void Awake()
    {
        Hp = MaxHp;
        _sr = GetComponentInChildren<SpriteRenderer>();
    }

    private void Update()
    {
        if (GameManager.I == null || GameManager.I.State != GameState.Playing)
        {
            return;
        }

        if (_invulTimer > 0f)
        {
            _invulTimer -= Time.deltaTime;
        }
        if (_flashTimer > 0f)
        {
            _flashTimer -= Time.deltaTime;
            if (_flashTimer <= 0f)
            {
                _sr.color = Color.white;
            }
        }

        CheckContactDamage();
    }

    /// <summary>手算接触判定：贴身怪对玩家造成伤害（无敌帧限频）。</summary>
    private void CheckContactDamage()
    {
        var enemies = GameManager.I.Enemies;
        Vector3 pos = transform.position;
        for (int i = enemies.Count - 1; i >= 0; i--)
        {
            var e = enemies[i];
            if (e == null)
            {
                continue;
            }
            if (CircleHit.Hit(pos, Radius, e.transform.position, e.Radius))
            {
                TakeDamage(e.TouchDamage);
                return; // 每帧最多触发一次（无敌帧进一步限频）
            }
        }
    }

    public void TakeDamage(float dmg)
    {
        if (_invulTimer > 0f)
        {
            return; // 无敌帧内忽略
        }
        if (GameManager.I == null || GameManager.I.State != GameState.Playing)
        {
            return;
        }

        Hp -= dmg;
        _invulTimer = InvulDuration;

        _sr.color = new Color(1f, 0.45f, 0.45f); // 受击闪红
        _flashTimer = 0.12f;

        if (Hp <= 0f)
        {
            Hp = 0f;
            GameManager.I.EndRun(false); // 血尽 → 结算（Day 9 接面板）
        }
    }

    /// <summary>回复生命（升级被动用）。</summary>
    public void Heal(float v)
    {
        Hp = Mathf.Min(MaxHp, Hp + v);
    }
}
