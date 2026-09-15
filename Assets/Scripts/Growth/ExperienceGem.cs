using UnityEngine;

/// <summary>
/// 经验宝石：敌人死亡掉落，原地悬浮 + 自转。
/// Day 3 朴素版（无吸附/无拾取）；Day 7 增加磁铁吸附与拾取。
/// </summary>
public class ExperienceGem : MonoBehaviour
{
    public float ExpValue = 1f;

    private void Update()
    {
        if (GameManager.I == null || GameManager.I.State != GameState.Playing)
        {
            return;
        }

        // 自转（视觉提示：这是个可交互物）
        transform.Rotate(0f, 120f * Time.deltaTime, 0f, Space.World);
    }
}
