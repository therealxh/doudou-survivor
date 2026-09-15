using UnityEngine;

/// <summary>
/// 玩家属性与生存状态：HP / 受击无敌帧 / 拾取范围。
/// 被动加成（攻击/移速/拾取）Day 8 加入。
/// </summary>
public class PlayerStats : MonoBehaviour
{
    public float MaxHp = 100f;
    public float Hp { get; private set; }
    public float PickupRange = 2.5f;    // 磁铁吸附范围（Day 7 使用）
    public float InvulDuration = 0.5f;  // 受击后无敌时间（秒）

    private float _invulTimer;
    private float _flashTimer;
    private Material _mat;
    private Color _baseColor;

    private void Awake()
    {
        Hp = MaxHp;
        var r = GetComponent<Renderer>();
        _baseColor = r.sharedMaterial.GetColor("_BaseColor"); // 基色从共享材质读
        _mat = r.material; // 访问 .material 克隆出独享实例（受击闪红用；合批影响在 Day 6 讨论）
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
                _mat.SetColor("_BaseColor", _baseColor);
            }
        }
    }

    // 朴素版接触判定：怪碰到玩家触发（玩家为 Trigger + Kinematic Rigidbody，Day 5 换手算距离）
    private void OnTriggerStay(Collider other)
    {
        var enemy = other.GetComponent<Enemy>();
        if (enemy != null)
        {
            TakeDamage(enemy.TouchDamage);
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

        // 受击闪红
        _mat.SetColor("_BaseColor", new Color(1f, 0.45f, 0.45f));
        _flashTimer = 0.12f;

        if (Hp <= 0f)
        {
            Hp = 0f;
            GameManager.I.EndRun(false); // 血尽 → 结算（Day 9 接面板）
        }
    }
}
