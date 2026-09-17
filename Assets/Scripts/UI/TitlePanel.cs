using UnityEngine;

/// <summary>
/// 标题界面（Day 9）：游戏入口——标题 + 开始按钮 + 操作说明。
/// 初始显示；点击“开始游戏”进入正式一局（GameManager 默认状态为 Title）。
/// 面板使用“置顶子 Canvas”模式（与升级/结算面板一致）。
/// </summary>
public class TitlePanel : MonoBehaviour
{
    public static TitlePanel I { get; private set; }

    private GameObject _panelRoot;

    private void Awake()
    {
        I = this;
    }

    /// <summary>由 Bootstrap 注入面板根引用。</summary>
    public void Wire(GameObject panelRoot)
    {
        _panelRoot = panelRoot;
    }

    /// <summary>开始游戏（按钮回调）：隐藏标题并进入 Playing。</summary>
    public void OnStartGame()
    {
        _panelRoot.SetActive(false);
        if (GameManager.I != null)
        {
            GameManager.I.StartGame();
        }
    }
}
