using UnityEngine;

/// <summary>
/// 敌人：追击玩家 + 受击反馈（闪白 / 击退 / 飘字 / 死亡掉宝石）。
/// Day 5 起由对象池管理生命周期（Get/Release 替代 Instantiate/Destroy）。
/// 闪白仍为"每怪独享材质实例"（Day 6 换 MaterialPropertyBlock）。
/// </summary>
public class Enemy : MonoBehaviour
{
    public float Radius = 0.4f;
    public float MoveSpeed = 2.2f;
    public float Hp = 10f;
    public float TouchDamage = 5f;     // 接触玩家时的单次伤害
    public float KnockbackSpeed = 6f;  // 受击击退初速（m/s）
    public float StunDuration = 0.2f;  // 受击硬直：击退期间暂停追击，让击退看得清

    private SpriteRenderer _sr;     // 闪白：SpriteRenderer.color 直接染色（过曝白），无需材质实例
    private static readonly Color FlashColor = new Color(2f, 2f, 2f);
    private float _flashTimer;
    private float _stunTimer;       // 受击硬直剩余时间
    private Vector3 _knockVel;      // 击退速度脉冲（快速衰减）

    private void Awake()
    {
        _sr = GetComponent<SpriteRenderer>();
    }

    private void OnEnable()
    {
        // 向全局名单注册（池化后同样是这套机制：取用时注册、回收时注销）
        if (GameManager.I != null)
        {
            GameManager.I.Enemies.Add(this);
        }

        // 池化复用：重置受击状态（否则复用时会带着上一轮的死状态）
        _flashTimer = 0f;
        _stunTimer = 0f;
        _knockVel = Vector3.zero;
        if (_sr != null)
        {
            _sr.color = Color.white;
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

        // 闪白计时：到点恢复基色
        if (_flashTimer > 0f)
        {
            _flashTimer -= Time.deltaTime;
            if (_flashTimer <= 0f)
            {
                _sr.color = Color.white;
            }
        }

        // 击退脉冲：叠加位移并快速衰减
        if (_knockVel.sqrMagnitude > 0.0001f)
        {
            transform.position += _knockVel * Time.deltaTime;
            _knockVel = Vector3.Lerp(_knockVel, Vector3.zero, 8f * Time.deltaTime);
        }

        // 受击硬直衰减
        if (_stunTimer > 0f)
        {
            _stunTimer -= Time.deltaTime;
        }

        // 追击玩家（受击硬直期间暂停——否则追击会抵消掉击退的位移）
        if (_stunTimer > 0f)
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
            transform.rotation = GameBootstrap.FlatRotation(dir);
        }
    }

    /// <summary>受击：扣血 + 飘字；未死亡则闪白/击退，死亡则掉宝石并回收。</summary>
    public void TakeDamage(float dmg, Vector3 knockDir)
    {
        Hp -= dmg;
        DamageNumber.Show(transform.position + Vector3.up * 0.7f, dmg);

        if (Hp <= 0f)
        {
            Die();
            return;
        }

        // 闪白 + 击退 + 硬直
        _sr.color = FlashColor;
        _flashTimer = 0.1f;
        _stunTimer = StunDuration;

        Vector3 dir = knockDir;
        dir.y = 0f;
        if (dir.sqrMagnitude > 0.0001f)
        {
            _knockVel = dir.normalized * KnockbackSpeed;
        }
    }

    private void Die()
    {
        if (GameManager.I != null)
        {
            GameManager.I.KillCount++;
        }

        // 掉落经验宝石（池取用）
        var gem = GameBootstrap.GemPool.Get();
        gem.transform.position = transform.position;

        GameBootstrap.EnemyPool.Release(this); // 回收（OnDisable 自动从名单注销）
    }
}
