# Day 2 思路记录 — 摇杆与飞刀

> 对应代码：`FloatingJoystick` / `WeaponBase` / `WeaponKnife` / `Projectile` / `GameBootstrap`（UI 构建）
> 重做对照难度：★★★☆☆（新增 4 个文件，核心难点是 UGUI 事件与坐标换算）

## 今天做了什么

浮动摇杆（UGUI 全代码构建）→ 武器基类（冷却计时 + 自动索敌）→ 飞刀（投射物弹道）→ 朴素版 Physics 命中（为 Day 5 重构留基线）。

## 关键决策与权衡

### 1. 浮动摇杆 vs 固定摇杆
- 选**浮动**：按住屏幕任意位置即出现摇杆，手指按哪儿哪儿就是中心——手游标配体验（对标弹壳特攻队）。
- 结构：全屏透明 Image 接收事件 + 底座/手柄两个方块（与几何体美术统一）。
- 坐标换算：`RectTransformUtility.ScreenPointToLocalPointInRectangle`（屏幕坐标 → UI 局部坐标），偏移量归一化除以半径得到 Value。

### 2. 武器用继承，不用 SO + 策略模式
- `WeaponBase` 抽象类管公共逻辑（冷却/索敌/升级），子类只管 `Fire()` 一件事。
- 这是"最小复杂度档"的落地：数据用字段、行为用 override，重做时半小时能复刻。

### 3. 索敌用"简单遍历"，空间哈希留给命中检测
- 索敌每 0.8s 才发生一次，遍历 300 个敌人做距离平方比较，成本可忽略。
- 空间哈希（Day 6）的价值在**每帧高频**的命中查询（子弹、大蒜光环）——这个取舍本身就是面试可讲的点。

### 4. 朴素版 Physics 命中（刻意的性能基线）
- 子弹 = `Rigidbody(kinematic)` + Trigger Collider → `OnTriggerEnter` 判定。
- 这是**故意**用物理引擎实现的"朴素版"：Day 5 会改成手算圆形碰撞 + 空间哈希，性能报告需要前后对比数据。
- 注意：`Rigidbody` 必须留一个（触发事件的前提），且用 kinematic 避免重力。

### 5. 射程 6m vs 敌人出生 10m
- 飞刀射程 6m，测试怪出生在 10m 环上——开局前两秒"飞刀打不到远处敌人"是正常现象（等怪靠近到 6m 内才开始命中）。

## 踩的坑（重要）

### 1. Unity 不会自动发现"外部工具写入的新文件"！
从 IDE 外部（Qoder）写入新 `.cs` 文件后，Unity 的 AssetDatabase **不会自动扫描到新文件**——旧文件报"找不到 FloatingJoystick 类型"，但其实文件在磁盘上好好的。
- **解决**：写完代码后必须 `AssetDatabase.Refresh()`（我用 MCP：`execute_code` 里调用），或者手动聚焦 Unity 窗口触发刷新。
- **教训**：外部工作流的"写代码 → 刷新 → 编译 → 检查 Console"要作为固定流程。

### 2. 编辑器失焦时 Play 几乎不推进
编辑器不在前台时（InteractionMode=Default），游戏帧几乎不走（frame=2 卡住）——这会让"后台自动化验证"失效。
- 已把 `InteractionMode=NoThrottling` 写进编辑器偏好（重启编辑器后生效）；期间用"把 Unity 窗口调到前台"的方式完成验证。

### 3. execute_code 环境里 `Object` 是歧义类型
`System.Object` 与 `UnityEngine.Object` 冲突——用全限定名 `UnityEngine.Object.FindObjectOfType<T>()`。

## 关键数字

| 项 | 值 | 位置 |
|---|---|---|
| 飞刀 伤害/冷却/射程/弹速 | 10 / 0.8s / 6m / 12 m/s | `WeaponKnife` |
| 摇杆 可拖半径/死区 | 140 px / 0.1（Value 平方 < 0.01） | `FloatingJoystick` |
| 摇杆底座/手柄尺寸 | 300×300 / 120×120 px | `GameBootstrap.BuildUI` |
| Canvas 参考分辨率 | 1080×1920（竖屏） | `GameBootstrap` |

## 验证证据（MCP 自动采样）
- 编译：零错误零警告。
- 飞刀链路：`kills=5, enemies=0`——5 只测试怪全部被飞刀击杀（索敌→发射→命中→扣血→击杀→计数）。
- 摇杆逻辑（模拟指针事件）：按下底座出现 ✓ → 拖动 100px 输出 Value=(0.71, 0) ✓（100/140 精确） → 抬起归零并隐藏 ✓。
- 手感（鼠标拖摇杆 + 飞刀节奏）：用户试玩确认。
