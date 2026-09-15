# 性能基线 — Day 4（朴素实现）

> 测量环境：Unity Editor Play 模式（编辑器数据用于相对对比；最终验收以 PC Build 实测为准）
> 场景：同屏 300 怪达到上限、timeScale=1 稳定后采样
> 采集方式：`PerfStats` 每 5s 输出 Console；fps 用 `Time.unscaledDeltaTime` 窗口平均；DrawCalls / GC 来自 `ProfilerRecorder`

## 原始数据（按时间顺序）

| 敌人数 | fps | DrawCalls | GC/帧 (KB) | 备注 |
|---|---|---|---|---|
| 1   | —   | 0    | 0.4  | 开局 |
| 6   | 95  | 63   | 13.4 | |
| 61  | 74  | 389  | 29.1 | 加速积累期 |
| 100 | 30  | 627  | 13.2 | 加速积累期 |
| 213 | 76  | 1296 | 29.2 | 加速积累期 |
| 300 | 83  | 1847 | 13.2 | 加速积累期（顶到上限） |
| 300 | 71  | 1836 | 26.2 | **timeScale=1 采样** |
| 300 | 64  | 1843 | 13.2 | **timeScale=1 采样** |

## 朴素实现的三个已知问题（Day 5-6 的修复目标）

1. **DrawCall 随怪数线性增长**：300 怪 ≈ 1840 DrawCalls —— 根因：**每怪独享材质实例**（Day 3 刻意的朴素实现）。修复：共享材质 + `MaterialPropertyBlock` 闪白（Day 6）。
2. **持续 GC 分配 13~29 KB/帧**：根因：运行时 `Instantiate/Destroy`（怪/子弹/宝石/飘字）+ 飘字 `ToString`。修复：对象池（Day 5）。
3. **Physics 触发的加速穿透**：物理帧率固定 50Hz，`timeScale` 加速时每物理帧的位移过大 → 子弹跳过怪（加速测试期命中率骤降）。修复：手算碰撞不依赖物理帧率（Day 5）。

## 备注
- 加速积累期的数据背景是 `timeScale=20`，仅供"怪数 → 负载"趋势参考；权威基线是两条 timeScale=1 的数据。
- `cpu_frame_time_ms`（FrameTiming）在编辑器失焦节流时不可用，未纳入。
