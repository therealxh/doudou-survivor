# 豆豆幸存者 · 10 天开发版设计规格

| 项 | 内容 |
|----|------|
| 日期 | 2026-09-15 |
| 状态 | 设计评审已通过（2026-09-15 用户批准） |
| 关系 | 本文档是《项目交接文档》第 3 节的最终修订版；两者冲突时以本文档为准 |
| 仓库建议名 | DoudouSurvivor |

---

## 1. 项目目标与复杂度约束

### 1.1 目标
- 10 个开发日内完成可运行的 Roguelike 割草生存 Demo（对标《弹壳特攻队》《吸血鬼幸存者》）
- 交付物：可运行构建、1 页性能报告（帧率 / DrawCall / GC 优化前后对比）、代码仓库 + README、1 分钟演示视频
- 阶段 2（约一周）：用户独立重写 P0 / P1 模块，AI 只做教练，不代写

### 1.2 复杂度约束（2026-09-15 评审新增）
- 「性能三件套」（对象池 / 空间哈希 / 手算碰撞）全部保留，但按第 5 节的**最小实现规格**执行
- 砍除：策略模式 + SO 数据分离（降级为简单继承）、字符串预缓存、材质合批细分优化、属性修饰符系统、事件总线
- 新增决策：竖屏 1080×1920；不写自动化测试；音效 / 粒子默认不做

---

## 2. 范围

### 2.1 做（10 天版形态）

| 内容 | 形态 |
|------|------|
| 摇杆移动 | UGUI 摇杆（触屏 + 鼠标）+ WASD 双输入 |
| 自动攻击 | 2 武器：飞刀（弹道）/ 大蒜（光环），继承结构 |
| 升级三选一 | 时停 + 面板；等权随机；满级项退出候选池 |
| 敌潮递增 | 1 种怪；刷怪间隔递减 + 属性增强曲线 + 同屏上限 300 |
| 经验宝石 | 1 种面值 + 磁铁吸附（手写 Lerp） |
| 被动系统 | 攻击 / 移速 / 拾取范围，各 3 级（每级 +10%） |
| 武器升级 | 各 3 级 |
| 性能优化 | 对象池 / 手算碰撞 / 空间哈希 / 闪白 MPB / Canvas 动静分离 |
| 结算界面 | 存活时间 + 击杀数 + 重开（场景重载） |
| 主菜单 | 标题 + 开始按钮 |

### 2.2 不做
Boss、进化、局外成长、存档、Mock 广告、微信小游戏；精英怪（默认不做，余力再议）；音效 / 粒子（默认不做，最后一天有余力再议）；自动化测试。

### 2.3 明确排除的技术
Input System 包、DOTween、EventBus、Cinemachine、Unity Physics（碰撞全手算）、Animator（动画全程序化）。

---

## 3. 技术基线
- 引擎：Unity 2022.3.62f3c1，3D (URP) 模板
- 视角：正交相机，俯角约 55°
- 屏幕：竖屏 1080×1920
- 渲染：几何体 + 纯色材质；怪物程序化动画（缩放呼吸 + 朝向摆动）
- 输入：旧版 Input API + UGUI 摇杆（触屏与鼠标指针统一走 EventSystem 拖拽）
- 场景：单场景 `SampleScene`（零资产修改）；重开 = SceneManager.LoadScene；全部游戏对象由 GameBootstrap 运行时构建

---

## 4. 工程结构

### 4.1 脚本清单（Assets/Scripts/，共 23 个文件，预计合计 1500~2200 行含注释）

| 模块 | 文件 | 职责 | 重做档 |
|------|------|------|--------|
| Core | GameBootstrap.cs | 运行时入口：代码装配相机/地面/材质/玩家/模板/管理器（零资产手术） | P1 |
| Core | GameManager.cs | 状态机（Playing/LevelUp/GameOver）、时停（timeScale）、胜负流程 | P1 |
| Core | PerfStats.cs | 开发验证工具：每 5s 输出帧时间 / DrawCall / GC Alloc（读 ProfilerRecorder） | P2 只读 |
| Core | ObjectPool.cs | 泛型对象池（三件套①） | P0-a |
| Core | SpatialHashGrid.cs | 空间哈希（三件套②） | P0-a |
| Core | CircleHit.cs | 距离平方圆形碰撞（三件套③） | P0-a |
| Player | PlayerController.cs | 摇杆 + WASD 输入合并 → 移动 | P1 |
| Player | PlayerStats.cs | 血量 / 移速 / 攻击 / 拾取范围 + 被动加成 | P1 |
| Player | FloatingJoystick.cs | UGUI 摇杆（死区、归一化） | P1 |
| Weapons | WeaponBase.cs | 抽象基类：冷却、索敌（遍历最近）、升级 | P0-b |
| Weapons | WeaponKnife.cs | 飞刀：发射弹道 | P0-b |
| Weapons | WeaponGarlic.cs | 大蒜：范围伤害（走空间哈希查询） | P0-b |
| Weapons | WeaponManager.cs | 持有武器、升级应用、三选一候选池 | P0-b |
| Growth | LevelSystem.cs | 经验 / 等级 / 升级触发 / 生成三选一选项 | P0-b |
| Growth | ExperienceGem.cs | 磁铁吸附（范围判断 + Lerp 加速飞向玩家） | P1 |
| Combat | Projectile.cs | 弹道移动 + 空间哈希命中 + 回收 | P0-b |
| Enemies | Enemy.cs | 追击、受击（掉血 / 闪白 / 击退）、死亡掉宝石 | P1 |
| Enemies | EnemySpawner.cs | 视野外圆环刷怪、间隔递减、属性曲线、上限 300 | P1 |
| 表现 | DamageNumber.cs | 飘字（池化；字符串直接 ToString，不做缓存） | P2 只读 |
| 表现 | CameraRig.cs | 平滑跟随 + 提供刷怪环坐标计算 | P2 只读 |
| UI | HUD.cs | 血条 + 经验条 + 计时（独立 Canvas） | P2 只读 |
| UI | LevelUpPanel.cs | 三选一面板 | P2 只读 |
| UI | GameOverPanel.cs | 结算 + 重开 | P2 只读 |

### 4.2 场景与对象组装（全代码构建风格）
- 场景：`SampleScene` 零资产修改（不手术 .unity / .prefab / .asset 的 YAML）；游戏对象全部由 `GameBootstrap`（RuntimeInitializeOnLoadMethod + SceneManager.sceneLoaded）在运行时构建：相机、地面、材质、玩家、对象模板、UI
- 对象模板：Player、Enemy、Knife、Gem、DamageNumber、Joystick 均为代码构建的 GameObject 模板（无 prefab 资产）
- 重开 = SceneManager.LoadScene 后自动重新构建
- 无 ScriptableObject 资产（已砍）

### 4.3 目录结构
```text
Assets/
├── Scenes/Main.unity
├── Scripts/
│   ├── Core/  Player/  Weapons/  Growth/  Combat/  Enemies/  UI/
├── Prefabs/
└── Materials/
```

---

## 5. 性能三件套（最小实现规格）

> 说明：本节描述 Day 5-6 重构后的**最终形态**；Day 1-4 按朴素实现留性能基线（Physics 触发 / Instantiate-Destroy / 全表遍历命中），两者对比即性能报告的数据来源。

### 5.1 对象池 ObjectPool<T>（约 80 行）
- 四类池及预热数：怪 150、子弹 50、宝石 150、飘字 30（Inspector 可调）；不足时动态补充，无上限
- 结构：`Stack<T>` 存空闲对象；`Get()` 取出并激活；`Release()` 失活并放回

```csharp
public class ObjectPool<T> where T : Component
{
    private readonly Stack<T> _idle = new();
    public T Get()   => _idle.Count > 0 ? _idle.Pop() : Create();
    public void Release(T o) { o.gameObject.SetActive(false); _idle.Push(o); }
}
```
- 明确不做：多池统一管理器、扩容预警、分帧回收

### 5.2 手算碰撞 CircleHit（约 20 行）
```csharp
public static class CircleHit
{
    public static bool Hit(Vector2 a, float ra, Vector2 b, float rb)
    { float r = ra + rb; return (a - b).sqrMagnitude <= r * r; }
}
```
- 使用点：① 子弹 vs 怪 ② 大蒜 vs 怪 ③ 怪 vs 玩家 ④ 宝石磁吸范围判断
- 一律用 `sqrMagnitude` 比较，避免开方

### 5.3 空间哈希 SpatialHashGrid（约 100 行）
- 格子边长 1m；key = `((long)cellX << 32) | (uint)cellY`
- 每帧重建：清空全部 List → 遍历所有敌人按位置插入对应格
- `Query(pos, radius, result)`：只遍历半径覆盖的少数格子，收集候选敌人
- 结果列表类内复用，不每次 new
- 使用点：① 子弹命中 ② 大蒜光环
- 明确不做：增量更新、动态格子大小、多线程

### 5.4 设计取舍要点（重做自测素材）
1. 对象池为什么用 Stack：复用最近释放的对象，缓存友好、实现最简
2. 为什么弃 Physics：Collider / Rigidbody 有固定同步开销，300 怪时成为瓶颈；全圆形游戏用距离平方即可
3. 为什么空间哈希每帧重建：避免维护「敌人移动时更新格子归属」的状态同步复杂度；重建 300 次插入的成本可忽略
4. 为什么武器索敌不用空间哈希：索敌每 0.8s 一次，遍历 300 次比较可忽略；空间哈希留给每帧高频的命中查询

---

## 6. 武器、成长与数值

### 6.1 武器基类骨架
```csharp
public abstract class WeaponBase : MonoBehaviour
{
    [SerializeField] protected float cooldown = 0.8f, range = 6f, damage = 10f;
    public int Level { get; private set; } = 1;
    private float _timer;
    protected abstract void Fire();                          // 子类实现
    protected Enemy FindNearest() { /* 简单遍历全部敌人 */ }
    public void Upgrade() { Level++; /* 数值提升 */ }
}
```
- 冷却计时在 Update 中推进；时停（LevelUp 状态）时不动作

### 6.2 飞刀 WeaponKnife
- `Fire()`：找最近敌 → 从子弹池 Get → 朝目标方向直线发射
- 升级：每级 +5 伤害（冷却、射程不变）

### 6.3 大蒜 WeaponGarlic
- `Fire()`：`Query(自身位置, 半径)` → 对结果中全部敌人扣血
- 升级：每级 +3 伤害

### 6.4 升级三选一
- 触发：经验满 → GameManager 时停（timeScale = 0）→ LevelSystem 生成选项
- 选项池 = 未满级武器 + 未满级被动；等权随机取 3 个（池不足 3 时按实际数量）
- 选择后应用升级 → 恢复 timeScale = 1；武器 / 被动到 3 级即退出候选池

### 6.5 被动
- PlayerStats 三个加成字段：攻击 +10%/级、移速 +10%/级、拾取范围 +10%/级，各 3 级

### 6.6 数值表（全部 Inspector 可调）

| 参数 | 初值 | 成长 |
|------|------|------|
| 玩家 HP | 100 | — |
| 玩家移速 | 5 m/s | 被动每级 +10% |
| 玩家受击无敌帧 | 0.5s | — |
| 怪接触伤害 | 5 / 次（受无敌帧限频） | — |
| 飞刀 伤害 / 冷却 / 射程 / 弹速 | 10 / 0.8s / 6m / 12 m/s | 每级 +5 伤害 |
| 大蒜 伤害 / 冷却 / 半径 | 5 / 0.5s / 2.5m | 每级 +3 伤害 |
| 怪 HP / 移速 / 半径 | 20 / 2.2 m/s / 0.4m | HP 每分钟 ×1.1（复合） |
| 刷怪间隔 | 1.0s 起 | 每分钟 −0.1s，下限 0.2s |
| 同屏上限 | 300 | — |
| 宝石经验值 | 1 | — |
| 升级需求 | 5 + 当前等级 × 3 | — |
| 单局时长上限 | 10 分钟（到时按胜利结算） | — |

---

## 7. 数据流

输入（摇杆 / WASD）→ PlayerController 移动 → 武器冷却检查 → 自动索敌 → Fire → 飞刀经 Projectile 每帧移动 + 空间哈希命中 / 大蒜经空间哈希范围扣血 → 命中后：掉血、闪白（MPB）、击退、飘字 → 死亡：回池 + 掉宝石 → 宝石磁吸 → 经验满 → 时停 + 三选一 → 应用升级 → 恢复；玩家血尽或 10 分钟到 → 结算 → 重开（场景重载）。

---

## 8. 验收标准（10 天结束时）

1. 完整一局：开局 → 割草升级 → 死亡或 10 分钟存活 → 结算 → 重开，中途无卡死
2. 同屏 300 怪稳定 60 FPS（PC Build 实测，Profiler 截图）
3. 1 页性能报告：帧率 / DrawCall / GC Alloc 优化前后对比（Day 1-4 留朴素基线）
4. 代码仓库 + README（架构图 + 技术点）+ 1 分钟演示视频
5. 构建产物：PC 包必交；Android APK 视 Day 1 环境验证结果（投入 ≤ 0.5 天）

---

## 9. 逐日计划（Day 1 以工程创建日为准起算）

| 天 | 开发内容 | 验收标准 |
|----|----------|----------|
| Day 1 | 工程搭建（URP 3D）、目录、GameManager、相机、地面、纯 WASD 移动、测试怪追击（朴素 Instantiate） | 建工程 + 验证 Android 打包 + 能移动、怪追你 |
| Day 2 | UGUI 摇杆（死区 / 归一化）+ 武器框架 + 飞刀 | 摇杆手感顺 + 飞刀自动索敌打怪 |
| Day 3 | 敌人完整化 + 命中链路 + 闪白 / 击退 / 飘字 / 宝石掉落（全朴素） | 打死怪有完整反馈链路 |
| Day 4 | 刷怪管理器（间隔递减 / 属性曲线 / 上限 300） | 跑压力测试：基线数据 + Profiler 截图 |
| Day 5 | 性能重构①：对象池 + 手算圆形碰撞（弃 Physics） | 行为不变 + 帧率回升（对比数据①） |
| Day 6 | 性能重构②：空间哈希 + 闪白 MPB + Canvas 动静分离 | 300 怪 60FPS 达标（对比数据②） |
| Day 7 | 成长系统：经验宝石 + 磁铁吸附 + 升级三选一 + 大蒜 | 割怪 → 升级 → 选卡 → 变强循环成立 |
| Day 8 | 3 被动 + 武器升级 3 级 + 刷怪强度曲线 + 经验曲线 | 构筑感成立，一局能玩 10 分钟 |
| Day 9 | HUD + 主菜单 + 死亡结算 + 10 分钟胜利 | 全流程闭环、无卡死 |
| Day 10 | 打包（PC / Android）+ 性能报告 + README + 演示视频 | 交付物齐 |

附注：撞休息日内容顺延半天；紧张时砍 Day 8 强度曲线打磨，用最简递增逻辑兜底。

---

## 10. 阶段 2 重做计划（9/24 起，每天 3-4h）

| 档 | 范围 | 目标 |
|----|------|------|
| P0-a | ObjectPool + SpatialHashGrid + CircleHit（3 文件 + 接入点） | 亲手写三件套，能用 Profiler 讲前后对比 |
| P0-b | WeaponBase / Knife / Garlic / Manager + Projectile + LevelSystem / LevelUpPanel | 武器与升级闭环 |
| P1 | Enemy / EnemySpawner / ExperienceGem / PlayerController / PlayerStats / FloatingJoystick / GameManager | 手写主干 |
| P2 | DamageNumber / CameraRig / HUD / GameOverPanel | 只读不重做 |

机制：每天完成后做「三问自测」（为什么这么设计 / 踩过什么坑 / 关键数字为什么是它）；卡住超过 30 分钟对照原代码找差异。

---

## 11. 错误处理与测试策略

- 错误处理：目标为「全程不卡死」（验收标准 1）；不做异常兜底框架。时停用 `Time.timeScale = 0`；重开用场景重载
- 测试：不写自动化测试（10 天 Demo + 最小复杂度约束）；验收方式 = 每日运行 + 可玩性检查 + Day 4/5/6 的 Profiler 数据
