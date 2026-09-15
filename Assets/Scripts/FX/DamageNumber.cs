using UnityEngine;

/// <summary>
/// 伤害飘字：上飘 + 渐隐，0.6 秒后自动销毁。
/// Day 3 朴素版：Instantiate/Destroy（Day 5 换对象池；字符串直接 ToString，不做缓存）。
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

    /// <summary>在指定世界位置弹出一条伤害数字。</summary>
    public static void Show(Vector3 worldPos, float amount)
    {
        var template = GameBootstrap.DamageNumberTemplate;
        var go = Object.Instantiate(template, worldPos, template.transform.rotation);
        go.SetActive(true);
        go.GetComponent<TextMesh>().text = ((int)amount).ToString();
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
            Destroy(gameObject); // Day 5 改为池回收
        }
    }
}
