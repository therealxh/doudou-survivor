using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 升级三选一面板（Day 7）：时停中显示 3 个升级项；点击 → 应用升级并恢复游戏。
/// 控件由 GameBootstrap.BuildUI 代码构建后 Wire 注入。
/// </summary>
public class LevelUpPanel : MonoBehaviour
{
    public static LevelUpPanel I { get; private set; }

    private GameObject _panelRoot;
    private Button[] _buttons;
    private Text[] _titles;
    private Text[] _descs;
    private List<UpgradeOption> _options;

    private void Awake()
    {
        I = this;
    }

    /// <summary>由 Bootstrap 注入控件引用（代码构建 UI）。</summary>
    public void Wire(GameObject panelRoot, Button[] buttons, Text[] titles, Text[] descs)
    {
        _panelRoot = panelRoot;
        _buttons = buttons;
        _titles = titles;
        _descs = descs;
    }

    public void Show(List<UpgradeOption> options)
    {
        _options = options;
        _panelRoot.SetActive(true);

        for (int i = 0; i < _buttons.Length; i++)
        {
            bool has = i < options.Count;
            _buttons[i].gameObject.SetActive(has);
            if (!has)
            {
                continue;
            }
            _titles[i].text = options[i].Title;
            _descs[i].text = options[i].Desc;
        }
    }

    public void Hide()
    {
        _options = null;
        _panelRoot.SetActive(false);
    }

    /// <summary>按钮点击（Bootstrap 绑定）：应用第 i 个升级。</summary>
    public void OnPick(int index)
    {
        if (_options == null)
        {
            return;
        }
        var ls = GameManager.I != null ? GameManager.I.Levels : null;
        if (ls != null)
        {
            ls.Pick(index);
        }
    }
}
