using UnityEngine;

/// <summary>
/// 伤害飘字：上飘 + 渐隐，0.6 秒后回池。
/// Day 5 起由对象池管理（Get/Release 替代 Instantiate/Destroy）。
/// </summary>
public class DamageNumber : MonoBehaviour
{
    private const float Lifetime = 0.6f;
    private const float RiseSpeed = 1.5f;

    private float _age;
    private TextMesh _text;

    private void Awake()
    {
        _text = GetComponent<TextMesh>();
    }

    /// <summary>在指定世界位置弹出一条伤害数字（池取用）。</summary>
    public static void Show(Vector3 worldPos, float amount)
    {
        var dn = GameBootstrap.DamageNumberPool.Get();
        dn.Setup(worldPos, amount);
    }

    /// <summary>池复用入口：重置生命周期与显示。</summary>
    public void Setup(Vector3 worldPos, float amount)
    {
        _age = 0f;
        transform.position = worldPos;
        _text.text = ((int)amount).ToString(); // 字符串直接 ToString（不做缓存——最小复杂度约定）

        var c = _text.color;
        c.a = 1f;
        _text.color = c;
    }

    private void Update()
    {
        if (GameManager.I == null || GameManager.I.State != GameState.Playing)
        {
            return; // 时停/结算时飘字冻结（与全局暂停一致）
        }

        _age += Time.deltaTime;
        transform.position += Vector3.up * (RiseSpeed * Time.deltaTime);

        // 渐隐
        var c = _text.color;
        c.a = Mathf.Clamp01(1f - _age / Lifetime);
        _text.color = c;

        if (_age >= Lifetime)
        {
            GameBootstrap.DamageNumberPool.Release(this);
        }
    }
}
