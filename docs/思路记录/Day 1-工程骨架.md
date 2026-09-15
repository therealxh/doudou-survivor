# Day 1 思路记录 — 工程骨架

> 对应代码：`GameBootstrap` / `CameraRig` / `GameManager` / `PlayerController` / `Enemy`
> 重做对照难度：★★☆☆☆（概念都是入门级，但有几个"坑点"值得细读）

## 今天做了什么

运行时入口（代码构建一切）→ 正交俯视相机 → 地面 → 玩家（WASD 移动）→ 5 只测试怪（追击玩家）。

## 关键决策与权衡

### 1. 全代码构建（不用 prefab、不改场景文件）
- **为什么**：直接改 `.unity/.prefab` 的 YAML 极易出错且不可读；代码创建的一切都是普通 C#——你能直接读懂、直接改。对"AI 全包 + 用户重做"这个模式，这是出错率最低的路径。
- **代价**：Unity 编辑器里场景视图是空的，点 Play 才能看到游戏内容。
- **重做提示**：这套模式完全不需要学编辑器技巧，纯写 C# 即可。

### 2. 启动方式：RuntimeInitializeOnLoadMethod + sceneLoaded
- `[RuntimeInitializeOnLoadMethod]` 让游戏一启动就执行——不用往场景里放任何对象。
- **坑**：它只在"游戏启动"时跑一次；点"重开"（LoadScene 重新加载场景）**不会**再次触发！
- **解决**：订阅 `SceneManager.sceneLoaded`，每次场景加载完成后重新 `Build()` → 重开自动重建。
- 数字：启动时 Build 一次 + 之后每次场景加载 Build 一次。

### 3. 相机参数怎么算的
- 正交 `size = 6`：垂直可视范围共 12 个世界单位（竖屏手机视野）。
- 俯角 55° 的偏移向量：`Quaternion.Euler(55,0,0) * Vector3.back * 14` → `(0, 11.5, -8.0)`。
- **重做自测**：为什么用 `Vector3.back` 去旋转？（因为 back 是"从目标看相机"的反方向，转出俯视所需的"上方+后方"偏移）

### 4. 材质缓存（为合批做准备）
- `MakeMaterial` 用 `Dictionary<Color, Material>` 缓存：相同颜色共享同一个材质实例。
- **为什么**：Unity 合批的条件之一就是"同材质"——不同对象用同一材质才可能合批。
- 今天只有 3 个颜色（地面/玩家/怪），但机制从第一天就立住。

## 踩过的坑
1. 工程模板错成 2D → 用 Unity Hub 缓存里的官方 `urp-blank` 模板 + 命令行重建（`Unity.exe -batchmode -createProject -cloneFromTemplate`）。
2. Write 工具写"多层新目录"时偶尔报失败但文件实际已落盘（重试提示 no change 即说明已存在）——先建目录再写更稳。
3. Unity 批处理与编辑器**不能同时**打开同一工程（编译验证前先确认没有编辑器在跑）。
4. **敌人出生点悬空 bug**：`Vector3(cos, 0.5, sin) * 10` 会把 y 也乘 10 → 敌人浮在 5 米高。修复：只把 x/z 乘半径（分量分别计算）。教训：对 Vector3 整体缩放前先想清楚每个分量要不要一起变。
5. **Unity MCP 中文路径坑（环境级，值得记住）**：MCP for Unity 的 server（Python）用系统默认编码（中文系统=GBK）读 UTF-8 状态文件（内含中文工程路径）→ UnicodeDecodeError 被静默吞掉 → MCP 永远“找不到 Unity 实例”。修复：server 源码的 `open()` 补 `encoding='utf-8'` + mcp.json 加 `PYTHONUTF8=1`。教训：Windows 上处理非 ASCII 路径的工具链都要显式 UTF-8。

## 关键数字

| 项 | 值 | 位置 |
|---|---|---|
| 玩家移速 | 5 m/s | `PlayerController.MoveSpeed` |
| 玩家半径 | 0.5 | 与 1×1 方块视觉匹配 |
| 怪移速 | 2.2 m/s | 低于玩家（可以拉开距离） |
| 相机 距离/俯角/正交尺寸 | 14 / 55° / 6 | `CameraRig` + `GameBootstrap` |
| 测试怪 | 5 只，环状半径 10 | Day 1 临时，Day 4 由 Spawner 接管 |

## 验证证据
- 编译：batchmode 输出 `Tundra build success`，无 `error CS`；编辑器内 Console 零错误。
- 运行时（MCP 自动采样）：5 只敌人注册 ✓；敌人贴地 y=0.50 ✓（修复后）；追击方向正确（距离 10 → 9.91 递减）✓；相机位置 (0, 12, -8) 符合参数设计 ✓。
- WASD 手感：用户验收（注：编辑器 Edit → Preferences → General → Interaction Mode 需为 No Throttling，否则失焦时游戏不推进）。
