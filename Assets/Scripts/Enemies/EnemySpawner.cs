using UnityEngine;

/// <summary>
/// 刷怪管理器：视野外圆环刷怪；间隔随时间递减；怪属性随时间增强（复合）；同屏上限 300。
/// Day 5 起怪来自对象池。
/// </summary>
public class EnemySpawner : MonoBehaviour
{
    public float BaseInterval = 1.0f;       // 初始刷怪间隔（秒）
    public float IntervalDecayPerMin = 0.12f; // 每分钟减少（Day 8 配平：0.1 → 0.12，压力爬升更快）
    public float MinInterval = 0.2f;        // 间隔下限
    public int MaxAlive = 300;              // 同屏上限

    public float BaseEnemyHp = 10f;         // 初始怪血量（开局可一刀；中后期靠曲线变肉，对冲玩家成长）
    public float HpGrowthPerMinute = 1.15f; // 每分钟 ×1.15（Day 8 配平：1.1 → 1.15，成长更快）
    public float EnemySpeed = 2.2f;
    public float EnemyRadius = 0.4f;

    private float _timer;
    private CameraRig _rig;

    private void Update()
    {
        if (GameManager.I == null || GameManager.I.State != GameState.Playing)
        {
            return;
        }

        _timer -= Time.deltaTime;
        if (_timer > 0f)
        {
            return;
        }

        _timer = CurrentInterval();

        if (GameManager.I.Enemies.Count >= MaxAlive)
        {
            return; // 到达同屏上限：不生成（计时继续走）
        }
        SpawnOne();
    }

    /// <summary>当前刷怪间隔：随时间线性递减到下限。</summary>
    private float CurrentInterval()
    {
        float minutes = GameManager.I.ElapsedTime / 60f;
        return Mathf.Max(BaseInterval - IntervalDecayPerMin * minutes, MinInterval);
    }

    /// <summary>当前怪血量：每分钟 ×1.1 复合增长。</summary>
    private float CurrentHp()
    {
        float minutes = GameManager.I.ElapsedTime / 60f;
        return BaseEnemyHp * Mathf.Pow(HpGrowthPerMinute, minutes);
    }

    private void SpawnOne()
    {
        Vector3 center = GameBootstrap.Player != null
            ? GameBootstrap.Player.transform.position
            : Vector3.zero;

        // 环半径 = 相机可视区对角半径 + 余量（保证屏幕外）
        float ringRadius = 9f;
        if (_rig == null && Camera.main != null)
        {
            _rig = Camera.main.GetComponent<CameraRig>();
        }
        if (_rig != null)
        {
            ringRadius = _rig.GetSpawnRingRadius();
        }

        float ang = Random.Range(0f, Mathf.PI * 2f);
        Vector3 pos = center + new Vector3(Mathf.Cos(ang) * ringRadius, 0f, Mathf.Sin(ang) * ringRadius);
        pos.y = 0.5f;

        // 从池取怪（Day 5 起：池 Get 替代 Instantiate）
        var enemy = GameBootstrap.EnemyPool.Get();
        enemy.transform.position = pos;
        enemy.transform.rotation = Quaternion.identity; // 2.5D：直立 sprite，不旋转
        enemy.Hp = CurrentHp();
        enemy.MoveSpeed = EnemySpeed;
        enemy.Radius = EnemyRadius;
    }
}
