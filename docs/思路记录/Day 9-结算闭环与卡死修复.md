# Day 9 思路记录 — 结算闭环与"11 分钟卡死"修复

> 对应问题：用户报告"运行到 11 分钟左右卡死"
> 对应代码：`ResultPanel`（新增）、`GameManager.EndRun`、`GameBootstrap.BuildResultPanel`、`HintAutoHide`（新增）

## "11 分钟卡死"的真相

**症状**：玩到约 10-11 分钟，游戏停住、无任何响应。

**根因（一行代码级）**：单局时长上限是 **600 秒（10 分钟）**，到点触发胜利结算 `EndRun(true)`——但 `EndRun` 只做了"状态切换 + 时停"，**结算面板从未实现**（Day 5 起就挂着 `Day 9 接面板` 的 TODO）。于是：
- 时间到 → 时停 → **没有任何 UI/提示** → 玩家看到的只有"突然不动了" = 卡死
- 死亡路径（`EndRun(false)`）同理。

**教训**：**状态机里"不可逆的时停状态"必须有配套的出口 UI**——每一个"会让游戏停住"的入口（升级/结算），都要有"恢复/继续"的交互。

## 本日实现

### 1. 结算面板（ResultPanel）
- 显示：标题（"存活成功！"/"游戏结束"，配色区分）+ 成绩（存活时间 / 击杀数 / 达到等级）+ **重新开始**按钮。
- 触发：`GameManager.EndRun(win)` 直接调 `ResultPanel.I.Show(win)`（与升级面板同模式）。
- UI 置顶：**复用"子 Canvas（overrideSorting=20）+ 自带 GraphicRaycaster"**（比升级面板更高一层）——直接应用 Day 10 的射线教训。

### 2. 重开闭环（关键细节）
- `OnRestart`：**先把 `Time.timeScale` 恢复为 1**（否则新局开局就继承时停→ 又是"卡死"！）→ `SceneManager.LoadScene`。
- 重开依赖 Bootstrap 的 `sceneLoaded` 订阅重建全部（Day 0 设计的"每次场景加载自动 Build"在此兑现）。
- 验证中发现：`LoadScene` 不是"调用即完成"（下一帧才完成加载）——**同帧检查会误判"重开失败"**，推几帧后再查才是真相。

### 3. Hint 自动隐藏（抛光）
- 新组件 `HintAutoHide`：仅在 `State == Playing` 时显示操作提示——升级/结算面板弹出时不再有"WASD..."压在面板标题上。

## 验证证据（真实点击路径，吸取教训）
- 触发结算 → 面板激活、标题"存活成功！"、统计文本正确（0分2秒/0杀/1级）。
- 射线命中链：首命中可点击者 = **RestartButton** ✓。
- 完整点击路径（pointerDown/Up/Click）→ 推几帧后：**新局 state=Playing、时间=0.16、面板关闭、RunDuration 复位 600、timeScale=1** ✓。
- 失败路径（EndRun(false)）→ 标题"游戏结束" ✓；截图 `docs/art/结算面板预览.png`（ScreenCapture 才拍得到 UGUI——Camera.Render 拍不到 Overlay UI，记录备忘）。
- HintAutoHide：Playing 显示 True / 结算显示 False ✓。

## 关键数字
| 项 | 值 |
|---|---|
| 单局时长 | 600 秒（10 分钟） |
| 结算面板排序组 | sortingOrder=20（升级面板 10） |
| 重开流程 | timeScale 复位 → LoadScene → sceneLoaded 重建 |
