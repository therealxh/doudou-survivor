using UnityEngine;

/// <summary>
/// 爆炸视觉（Day 8）：爆炸圈快速扩散 + 渐隐，0.35 秒后回池。
/// 手雷爆炸时调用 Show（此前只有伤害没有表现，用户反馈"爆炸没效果"）。
/// </summary>
public class BlastEffect : MonoBehaviour
{
    private const float Lifetime = 0.35f;

    private float _age;
    private float _radius;
    private float _endMult = 1.15f;
    private SpriteRenderer _sr;

    private void Awake()
    {
        _sr = GetComponentInChildren<SpriteRenderer>();
    }

    private void OnEnable()
    {
        _age = 0f; // 池复用重置
    }

    /// <summary>在指定位置播放爆炸圈（radius = 爆炸判定半径，米）。</summary>
    public static void Show(Vector3 pos, float radius)
    {
        var b = GameBootstrap.BlastPool.Get();
        b.Setup(pos, radius, "explosion", 1.15f);
    }

    /// <summary>大蒜冲击波（Day 8）：换用波纹贴图、收到判定半径即止。</summary>
    public static void ShowShockwave(Vector3 pos, float radius)
    {
        var b = GameBootstrap.BlastPool.Get();
        b.Setup(pos, radius, "shockwave", 1f);
    }

    private void Setup(Vector3 pos, float radius, string spriteName, float endMult)
    {
        _radius = radius;
        _endMult = endMult;
        _sr.sprite = GameBootstrap.LoadSprite(spriteName); // 池对象两种用途：显式设置贴图
        transform.position = new Vector3(pos.x, 0.32f, pos.z); // 略高于地面，贴地表现
        transform.rotation = Quaternion.Euler(-90f, 0f, 0f);   // 平躺
        transform.localScale = Vector3.one * (radius * 2f * 0.7f); // 初始略小于判定圈

        _sr.color = new Color(1.15f, 1.15f, 1.15f, 1f); // 微提亮：地面深色上更醒目
    }

    private void Update()
    {
        if (GameManager.I == null || GameManager.I.State != GameState.Playing)
        {
            return;
        }

        _age += Time.deltaTime;
        float k = Mathf.Clamp01(_age / Lifetime);

        // 扩散（0.7x → endMult 判定直径）+ 渐隐（1−k²：前中期保持实色，末段快速隐去）
        transform.localScale = Vector3.one * (_radius * 2f * Mathf.Lerp(0.7f, _endMult, k));
        _sr.color = new Color(1.15f, 1.15f, 1.15f, 1f - k * k);

        if (_age >= Lifetime)
        {
            GameBootstrap.BlastPool.Release(this);
        }
    }
}
