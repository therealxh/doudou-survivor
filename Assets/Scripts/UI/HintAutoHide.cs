using UnityEngine;

/// <summary>
/// 操作提示自动隐藏（Day 9/10 抛光）：仅在游戏进行中显示，
/// 升级 / 结算面板弹出时自动隐藏，避免压在面板标题上。
/// </summary>
public class HintAutoHide : MonoBehaviour
{
    private void Update()
    {
        bool playing = GameManager.I != null && GameManager.I.State == GameState.Playing;
        if (gameObject.activeSelf != playing)
        {
            gameObject.SetActive(playing);
        }
    }
}
