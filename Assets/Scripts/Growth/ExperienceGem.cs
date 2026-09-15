using UnityEngine;

/// <summary>
/// 经验宝石：敌人死亡掉落；玩家靠近后磁吸飞向玩家，拾取后加经验（成长系统的燃料）。
/// </summary>
public class ExperienceGem : MonoBehaviour
{
    public float ExpValue = 1f;
    public float MagnetSpeed = 8f;

    private PlayerStats _stats;
    private bool _magnet;

    private void OnEnable()
    {
        _magnet = false; // 池复用重置
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

        transform.Rotate(0f, 120f * Time.deltaTime, 0f, Space.World); // 自转提示可拾取

        if (_stats == null)
        {
            _stats = player.GetComponent<PlayerStats>();
        }

        float range = (_stats != null ? _stats.PickupRange : 3.5f) * (_stats != null ? _stats.PickupMult : 1f);
        float d = Vector3.Distance(transform.position, player.transform.position);

        if (!_magnet && d <= range)
        {
            _magnet = true; // 进入范围即吸附（之后不再脱钩）
        }

        if (!_magnet)
        {
            return;
        }

        transform.position = Vector3.MoveTowards(transform.position, player.transform.position, MagnetSpeed * Time.deltaTime);

        if (Vector3.Distance(transform.position, player.transform.position) <= 0.4f)
        {
            var levels = GameManager.I.Levels;
            if (levels != null)
            {
                levels.AddExp(ExpValue);
            }
            GameBootstrap.GemPool.Release(this);
        }
    }
}
