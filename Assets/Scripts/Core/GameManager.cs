using System.Collections.Generic;
using UnityEngine;

public enum GameState
{
    Playing,   // 正常游玩
    LevelUp,   // 升级时停
    GameOver   // 结算（死亡或胜利）
}

/// <summary>
/// 全局状态机 + 回合数据 + 敌人名单（唯一权威来源）。
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager I { get; private set; }

    public GameState State { get; private set; } = GameState.Playing;

    /// <summary>本局已进行的秒数（仅 Playing 态累加）。</summary>
    public float ElapsedTime { get; private set; }

    /// <summary>本局击杀数。</summary>
    public int KillCount { get; set; }

    /// <summary>存活敌人名单：Enemy 在 OnEnable 注册、OnDisable 注销。</summary>
    public readonly List<Enemy> Enemies = new List<Enemy>();

    /// <summary>空间哈希（三件套②）：每帧末重建，供子弹/大蒜做邻近查询。</summary>
    public readonly SpatialHashGrid Grid = new SpatialHashGrid();

    /// <summary>成长系统（Bootstrap 装配时注入）。</summary>
    public LevelSystem Levels { get; set; }

    /// <summary>单局时长上限（秒）：到时按胜利结算。</summary>
    public float RunDuration = 600f;

    private void Awake()
    {
        I = this;
        Enemies.Clear(); // 防场景重载时的残留
    }

    private void Update()
    {
        if (State != GameState.Playing)
        {
            return;
        }

        ElapsedTime += Time.deltaTime;
        if (ElapsedTime >= RunDuration)
        {
            EndRun(true); // 存活到时间上限 = 胜利
        }
    }

    private void LateUpdate()
    {
        // 每帧末重建空间哈希（所有 gameplay Update 之后；查询方晚一帧使用，行为等价）
        if (State == GameState.Playing)
        {
            Grid.Rebuild(Enemies);
        }
    }

    /// <summary>进入升级选卡：时停。</summary>
    public void EnterLevelUp()
    {
        State = GameState.LevelUp;
        Time.timeScale = 0f;
    }

    /// <summary>选卡完成：恢复。</summary>
    public void Resume()
    {
        State = GameState.Playing;
        Time.timeScale = 1f;
    }

    /// <summary>结束本局（死亡或胜利）：时停并弹出结算面板。</summary>
    public void EndRun(bool win)
    {
        State = GameState.GameOver;
        Time.timeScale = 0f;
        if (ResultPanel.I != null)
        {
            ResultPanel.I.Show(win); // 结算面板（Day 9/10：此前缺失导致“玩到 10 分钟游戏像卡死”）
        }
    }
}
