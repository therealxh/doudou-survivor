# Day 3 思路记录 — 反馈链路（受击反馈闭环）

> 对应代码：`Enemy`（完整受击）、`PlayerStats`（新增）、`DamageNumber`（新增）、`ExperienceGem`（新增）、`GameBootstrap`（模板/接线）
> 重做对照难度：★★★☆☆（概念不难，但"谁需要 Rigidbody、谁需要 Trigger"要理清）

## 今天做了什么

敌人受击反馈闭环：闪白 / 击退 / 飘字 / 死亡掉宝石 → 玩家 HP + 受击无敌帧 + 接触伤害（怪抓人掉血）→ 血尽结束。

## 关键决策与权衡

### 1. 闪白用"每怪独享材质"——这是刻意的朴素实现
- `renderer.material`（而不是 sharedMaterial）会克隆出独享材质实例，改颜色只影响这一只怪。
- **代价**：每只怪一个材质 → 破坏合批 → 渲染开销上升。这正是 Day 6 用 `MaterialPropertyBlock` 修复的性能对比素材（"修好合批"是 Day 6 的重点之一）。
- 玩家受击闪红同理（玩家只有一个，无副作用）。

### 2. 击退 = 速度脉冲，不是瞬移
- 受击瞬间给一个反向初速（6 m/s），然后每帧 `Lerp` 衰减到零——产生"被打飞一下"的冲击感。
- 相比直接位移 x 米，脉冲版更自然，也就多 3 行代码。

### 3. 飘字用 TextMesh + LegacyRuntime.ttf
- 世界空间数字：`TextMesh` 直接挂 GameObject，零资产依赖（字体用 Unity 内置）。
- 生命周期 0.6s：上飘 + 渐隐；模板对象在 Bootstrap 里建一次（inactive），Show() 时 Instantiate。
- 刻意固定 55° 朝向相机（不逐帧 billboard，相机角度是固定值）。

### 4. 玩家受击判定：谁需要 Rigidbody？谁需要 Trigger？（朴素 Physics 完整形态）
- 规则：**触发事件 = 至少一方是 Trigger Collider + 至少一方有 Rigidbody**。
- 本游戏的搭配：
  - 子弹：`Rigidbody(kinematic)` + `Trigger` ✅（它负责主动撞人）
  - 怪：普通 BoxCollider（无 RB）——作为"被动方"被子弹/玩家触发器感知
  - 玩家：`Rigidbody(kinematic)` + `Trigger` ✅（它负责感知贴脸的怪）
- 玩家用 `OnTriggerStay`（持续接触）+ 无敌帧限频（0.5s）→ 怪贴脸也不会每帧掉血。

### 5. 无敌帧 0.5s 的数学
- 怪接触伤害 5 / 次，无敌帧 0.5s → 单只怪贴脸的理论 DPS = 10/s；玩家 100 血最多可扛 10 秒被围殴。这是"手感调参"的第一批候选参数。

## 踩的坑（今天的大发现）

### 1. 编辑器失焦时 Play 几乎不推进 —— 排查过程与最终解法
- 现象：编辑器不在前台时游戏帧卡住（frame=2），之前靠"把窗口拉到前台"绕过，不可持续。
- 排查：InteractionMode（EditorPrefs）写了没用 → `PlayerSettings.runInBackground` 发现是 **False**（URP 模板默认！）→ 已设 True。
- **最终解法（重要，以后沿用）**：**"暂停 + 手动步进"验证模式**——
  ```csharp
  EditorApplication.isPaused = true;   // 进入受控模式
  for (int i = 0; i < N; i++) EditorApplication.Step();  // 每次调用同步推进一帧
  ```
  `Step()` 同步推进一帧（含 Update），**完全不受窗口焦点影响、确定性推进**——验证游戏逻辑的最佳工具。
- 验证"实时手感"时（动画质感、手感节奏）仍需用户聚焦试玩，两种模式互补。

### 2. 项目设置变更 = 要提交的文件
- `PlayerSettings.runInBackground = true` 修改的是 `ProjectSettings/ProjectSettings.asset`——属于项目配置，已随代码一起提交。

## 关键数字

| 项 | 值 | 位置 |
|---|---|---|
| 怪接触伤害 | 5 / 次 | `Enemy.TouchDamage` |
| 玩家无敌帧 | 0.5s | `PlayerStats.InvulDuration` |
| 击退初速 | 6 m/s（Lerp 系数 8 快速衰减） | `Enemy.KnockbackSpeed` |
| 闪白时长 | 怪 0.1s / 玩家 0.12s | `Enemy` / `PlayerStats` |
| 飘字 | 0.6s 生命 / 上飘 1.5 m/s / 字号 40 × charSize 0.08 | `DamageNumber` |

## 验证证据（暂停 + Step 推帧，确定性验证）
- 编译：零错误零警告。
- 击杀与掉落：推 220 帧后 `kills=4, gems=4`（一一对应）；补刀后总计 `kills=5, gems=5`。
- 接触伤害：玩家贴身被怪打到 `playerHp=95`（-5 ✓）。
- 无敌帧序列：受击后立即再受击被挡（`h1=h2=95`）→ 推 0.64s（>0.5s）后再受击生效（`h3=85`）✓。
- 飘字生成：手动受击采样 `dmgNumbersNow=1` ✓（0.6s 后自动消失）。
- 闪白/击退的**视觉**效果：用户试玩验收。
