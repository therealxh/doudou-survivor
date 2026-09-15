# Day 4 思路记录 — 刷怪管理与性能基线

> 对应代码：`EnemySpawner`（新增）、`PerfStats`（新增）、`CameraRig`（加刷怪环计算）、`GameBootstrap`（接线，移除 Day 1 测试怪）
> 重做对照难度：★★★☆☆（公式都简单，重点是"性能数据怎么采"的工程方法）

## 今天做了什么

正式的敌潮系统：视野外圆环刷怪、间隔递减、属性复合增长、同屏上限 300 → 性能数据自动采集（PerfStats）→ **300 怪压力测试基线存档**。

## 关键决策与权衡

### 1. 刷怪曲线的形式（全部线性/复合，不引曲线资产）
- **刷怪间隔**：`max(1.0 − 0.1×分钟, 0.2)` ——线性递减，8 分钟后到下限。
- **怪 HP**：`20 × 1.1^分钟` ——复合增长（指数），后期压力陡增。
- **上限 300**：达到后停止生成（计时继续，一有死亡立即补位）。
- 为什么这样：公式 3 行、可预测、调参就是改数字；面试也能讲清"为什么复合而不是线性"。

### 2. 刷怪环半径 = 相机对角半径 + 2m（由 CameraRig 提供）
- 正交相机可视区是个矩形：`halfH = orthographicSize`、`halfW = halfH × aspect`。
- 圆环半径 = `√(halfW² + halfH²) + 2` ——保证怪出生在屏幕外（竖屏下算出来约 9m）。
- 挂在 `CameraRig` 上是因为"视野几何"属于相机职责（设计 §3.3 也是这么分工的）。

### 3. PerfStats：让游戏自己写性能日志
- 每 5 秒输出一行：`[Perf] fps=.. drawCalls=.. gcKB=.. enemies=..`
- fps 用 `Time.unscaledDeltaTime` 窗口平均（不受 timeScale 干扰）；
- DrawCalls / GC 用 `ProfilerRecorder`（Render / Memory 分类）。
- **工程价值**：外部工具（MCP）事后读 Console 就能拿到数据——比"截图看 Profiler"自动化得多。

### 4. 压力测试的"加速 + 保护"配方
- `Time.timeScale = 20` 快速积累怪（25 秒真实 ≈ 500 秒游戏时间）；
- 临时关闭玩家 Collider：避免测试期间被 300 怪围殴致死（性能测试不测生存）；
- 顶到 300 后 `timeScale = 1` 稳定采样。

## 踩的坑

### 1. timeScale 加速会"穿透"Physics 触发！（重要的认知）
- 20 倍速下子弹每物理帧（固定 50Hz 实时）的位移达 4.8m，**直接跳过怪**——加速期命中率骤降到几乎为零，击杀数只有 12。
- 这不是 bug，而是"Physics 方案的天生局限"：物理帧率与游戏时间不同步。**手算碰撞（Day 5）每帧自己做距离判定，不受此影响**——这个坑直接给 Day 5 的重构增加了论据。

### 2. MCP server 偶发闪断（环境级）
- 现象：`No Unity Editor instances found` 随机出现（伴随 2 个 mcp-for-unity 进程的交替期）；重试不久后恢复。
- **应对模式（以后沿用）**：数据尽量"游戏自己写 Console → 事后读取"，读取被闪断打断就重试——比实时流式读取稳得多。

## 关键数字（刷怪曲线）

| 项 | 值 | 位置 |
|---|---|---|
| 刷怪间隔 | 1.0s 起，每分钟 −0.1，下限 0.2 | `EnemySpawner.CurrentInterval` |
| 怪 HP | 20 × 1.1^分钟（复合） | `EnemySpawner.CurrentHp` |
| 同屏上限 | 300 | `EnemySpawner.MaxAlive` |
| 刷怪环半径 | 相机对角半径 + 2m（≈9m） | `CameraRig.GetSpawnRingRadius` |
| 性能输出 | 每 5s 一条 [Perf] | `PerfStats.ReportInterval` |

## 验证证据
- 编译零错误；Spawner/PerfStats 正常挂载并工作（首帧即出第一只怪）。
- 压力测试：加速 25 秒（真实时间）→ 游戏时间 562s → **同屏顶到 300 怪**。
- 基线数据已存档：`docs/perf/baseline-day4.md`（300 怪：fps 64-71 / DrawCalls ~1840 / GC 13-26KB 帧）。
- 真开局的节奏感（间隔从 1s 开始）：用户试玩验收。
