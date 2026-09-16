using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 结算面板（Day 9/10）：胜利 / 失败时显示成绩（存活时间 / 击杀 / 等级）与“重新开始”。
/// 由 GameManager.EndRun 触发；面板使用“置顶子 Canvas”模式（吸取升级面板被摇杆层挡住的教训）。
/// </summary>
public class ResultPanel : MonoBehaviour
{
    public static ResultPanel I { get; private set; }

    private GameObject _panelRoot;
    private Text _title;
    private Text _stats;
    private Button _restartBtn;

    private void Awake()
    {
        I = this;
    }

    /// <summary>由 Bootstrap 代码构建 UI 后注入控件引用。</summary>
    public void Wire(GameObject panelRoot, Text title, Text stats, Button restartBtn)
    {
        _panelRoot = panelRoot;
        _title = title;
        _stats = stats;
        _restartBtn = restartBtn;
    }

    /// <summary>显示结算面板（win = 胜利 / 失败）。</summary>
    public void Show(bool win)
    {
        _panelRoot.SetActive(true);
        _title.text = win ? "存活成功！" : "游戏结束";
        _title.color = win
            ? new Color(1f, 0.95f, 0.6f)
            : new Color(1f, 0.5f, 0.5f);

        var gm = GameManager.I;
        float t = gm != null ? gm.ElapsedTime : 0f;
        int kills = gm != null ? gm.KillCount : 0;
        int lv = gm != null && gm.Levels != null ? gm.Levels.Level : 1;

        _stats.text = "存活时间：" + Mathf.FloorToInt(t / 60f) + " 分 "
                    + Mathf.FloorToInt(t % 60f) + " 秒\n"
                    + "击杀数：" + kills + "\n"
                    + "达到等级：" + lv;
    }

    /// <summary>重新开始：恢复时间流速后重载场景（Bootstrap 会在场景加载时重建全部）。</summary>
    public void OnRestart()
    {
        Time.timeScale = 1f; // 必须恢复：否则新局开局即是停状态
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
