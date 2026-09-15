# 豆豆幸存者 · 10 天实现计划

> **面向 AI 代理的工作者：** 本计划在当前会话内联执行（executing-plans 模式，无子代理调度）；步骤使用复选框（`- [ ]`）语法跟踪进度。
> **权威规格：** `docs/superpowers/specs/2026-09-15-doudou-survivor-10day-design.md`（与本计划冲突时以规格为准；本计划的"排期调整"小节除外）

**目标：** 10 个开发日内交付可运行的 Roguelike 割草生存 Demo（PC 包 + 性能报告 + README + 演示视频），阶段 2 用户可在一周内重做 P0/P1

**架构：** 3D URP + 全代码构建（`GameBootstrap` 运行时装配全部对象，零资产手术）；23 个 C# 文件按 7 模块分目录；性能三件套 = 对象池 / 空间哈希 / 手算碰撞

**技术栈：** Unity 2022.3.62f3c1 / URP 14.0.12 / 旧 Input API / UGUI（旧 Text + LegacyRuntime.ttf）/ Day2-4 用 Physics 触发做朴素基线、Day5 起弃用 / 无自动化测试

---

## 通用约定（每个任务都适用）

1. **验证通道优先级**：Unity MCP（编译错误 / Play / Console / 截图）→ batchmode 编译（编辑器关闭时）→ 用户手动验收（标 **[需用户]**）
2. **材质**：`GameBootstrap.MakeMaterial(Color)` 静态缓存字典（同色共享实例）；URP Lit，`SetColor("_BaseColor", c)`
3. **文本**：`UnityEngine.UI.Text` + `Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")`
4. **启动**：`[RuntimeInitializeOnLoadMethod(AfterSceneLoad)]` + `SceneManager.sceneLoaded` → 每次进入 SampleScene 自动 `Build()`（重开自动重建；静态引用随之刷新）
5. **敌人名单**：`GameManager.I.Enemies`（`List<Enemy>`）；`Enemy.OnEnable` 加入 / `OnDisable` 移除（适配 Day5 池化的取用/回收，机制不变）
6. **UI 通信**：UI 在 `Update` 中轮询 `GameManager.I.State` 与数据（无事件系统）
7. **commit**：每日 ≥ 1 次；格式 `feat:` / `fix:` / `refactor:` / `docs:`
8. **每日产出**：代码 + 验证证据 + `docs/思路记录/Day N-*.md`（设计权衡 / 踩的坑 / 关键数字）+ commit

## 对设计的排期调整（2 处）

- Day 6 的"Canvas 动静分离"并入 Day 9（UI 尚未存在，无物可分离；Day 9 构建 UI 时直接按动静分离实现）
- 新增 2 个文件已回写设计 §4.1：`GameBootstrap.cs`（运行时入口）、`PerfStats.cs`（性能数据采集）

## 关键接口约定（防漂移，后续任务全部以此为准）

```csharp
public enum GameState { Playing, LevelUp, GameOver }

public class GameManager : MonoBehaviour {
    public static GameManager I;
    public GameState State { get; private set; }
    public float ElapsedTime;            // 回合秒数
    public int KillCount;
    public readonly List<Enemy> Enemies = new();
    public void EnterLevelUp();          // 升级时停
    public void Resume();                // 恢复
    public void EndRun(bool win);        // 结算
}

public class PlayerController : MonoBehaviour { public float Radius; public float MoveSpeed; }
public class PlayerStats : MonoBehaviour { public float MaxHp, MoveBonus, AttackBonus, PickupBonus; public float Hp; public void ApplyPassive(PassiveType t); }

public abstract class WeaponBase : MonoBehaviour {
    public int Level { get; protected set; } = 1;
    protected float Cooldown, Range, Damage;
    protected abstract void Fire();
    protected Enemy FindNearest();       // 遍历 GameManager.I.Enemies
    public void Upgrade();
    public bool IsMaxed => Level >= 3;
}

public class Enemy : MonoBehaviour {
    public float Radius, MoveSpeed, Hp;
    public void SetStats(float hp, float speed);   // 由 Spawner 按曲线赋值
    public void TakeDamage(float dmg, Vector2 knockDir);
}
```

---

## Day 1 — 骨架与移动

### 任务 1.1：运行时入口 + 相机

**文件：**
- 创建：`Assets/Scripts/Core/GameBootstrap.cs`
- 创建：`Assets/Scripts/Player/CameraRig.cs`

- [ ] 步骤 1：写 `GameBootstrap.cs`（核心结构如下，本日先接通相机/地面/材质；玩家与怪在任务 1.2 追加）

```csharp
public static class GameBootstrap
{
    private static readonly Dictionary<Color, Material> MatCache = new();
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Init()
    {
        SceneManager.sceneLoaded += (_, __) => Build();
        Build();
    }
    private static void Build()
    {
        SetupCamera();               // 复用 Camera.main
        BuildGround();               // 60x60 立方体压扁，深绿色
        // 任务 1.2 起：BuildPlayer(); BuildTemplates(); BuildManagers();
    }
    public static Material MakeMaterial(Color c)
    { /* 缓存查找；Shader.Find("Universal Render Pipeline/Lit")；_BaseColor */ }
    public static GameObject MakeBox(string name, Vector3 scale, Color color, float radius)
    { /* Cube + 缩放 + 材质 + Enemy 组件（可选）；便于复用 */ }
}
```

- [ ] 步骤 2：写 `CameraRig.cs`——正交相机，俯角 55°，`orthographicSize = 6`；位置 = 目标 + 朝向偏移（距离 14：y=sin55°·14，z=-cos55°·14）；`LateUpdate` 平滑跟随（`Vector3.Lerp` 系数 10·dt）

- [ ] 步骤 3：编译验证（按"验证通道优先级"）

- [ ] 步骤 4：commit `feat: bootstrap and camera rig`

### 任务 1.2：GameManager + 玩家移动 + 测试怪

**文件：**
- 创建：`Assets/Scripts/Core/GameManager.cs`
- 创建：`Assets/Scripts/Player/PlayerController.cs`
- 创建：`Assets/Scripts/Enemies/Enemy.cs`（Day 1 朴素版：追击移动；Day 3 扩展）
- 修改：`Assets/Scripts/Core/GameBootstrap.cs`（追加 BuildPlayer / BuildEnemies）

- [ ] 步骤 1：写 `GameManager.cs`（按接口约定；`ElapsedTime += Time.deltaTime`（仅 Playing 态）；`EnterLevelUp/Resume` 控制 `Time.timeScale`；`EndRun` 切状态）

- [ ] 步骤 2：写 `PlayerController.cs`——`Input.GetAxisRaw("Horizontal"/"Vertical")` → 归一化 → `transform.position += dir * MoveSpeed * Time.deltaTime`；朝向按移动方向旋转（可选）

- [ ] 步骤 3：写 `Enemy.cs` 朴素版——`OnEnable/OnDisable` 注册；`Update` 朝玩家移动（Day 1 直接 `Vector3.MoveTowards`，无碰撞）

- [ ] 步骤 4：Bootstrap 追加：玩家（蓝色圆柱/胶囊，`MoveSpeed=5`，`Radius=0.5`）、5 只红色测试怪（分散半径 8~12 处）

- [ ] 步骤 5：Play 验证——WASD 移动；怪持续追人 **[需用户或 MCP]**

- [ ] 步骤 6：commit `feat: day1 game manager, player movement, test enemies`

### 任务 1.3：Day 1 收尾

- [ ] 步骤 1：写 `docs/思路记录/Day 1-工程骨架.md`（取舍：全代码构建 vs 资产手术；坑：2D 模板重建、[RuntimeInitializeOnLoadMethod] 与场景重载的关系；数字：相机参数）
- [ ] 步骤 2：更新 `项目交接文档.md` 第 4 节"当前状态"
- [ ] 步骤 3：commit `docs: day1 notes and handover update`

---

## Day 2 — 摇杆与飞刀

### 任务 2.1：UGUI 摇杆（代码构建）+ 输入合并

**文件：**
- 创建：`Assets/Scripts/Player/FloatingJoystick.cs`
- 修改：`Assets/Scripts/Player/PlayerController.cs`（摇杆与键盘合并：`dir = stick != 0 ? stick : keyboard`）

- [ ] 步骤 1：`FloatingJoystick.cs`：`IPointerDown/Drag/Up`（EventSystem 拖拽，触屏与鼠标统一）；死区 0.1、归一化、`Radius`（px→0~1）；Bootstrap 构建：Canvas（ScreenSpaceOverlay，`CanvasScaler` 竖屏 1080×1920）+ 底板 Image + 手柄 Image（原型 Sprite 用内建 `UISprite`）
- [ ] 步骤 2：临时"操作提示文本"（Text）显示"WASD / 左摇杆移动"
- [ ] 步骤 3：Play 验证：鼠标拖动摇杆可移动；键盘同时可用 **[需用户或 MCP]**
- [ ] 步骤 4：commit `feat: ugui floating joystick`

### 任务 2.2：武器框架 + 飞刀 + 投射物（朴素版）

**文件：**
- 创建：`Assets/Scripts/Weapons/WeaponBase.cs`、`WeaponKnife.cs`
- 创建：`Assets/Scripts/Combat/Projectile.cs`
- 修改：`Assets/Scripts/Core/GameBootstrap.cs`（给玩家挂 WeaponKnife；构建子弹模板）

- [ ] 步骤 1：`WeaponBase.cs`：`Update` 推进冷却（仅 `GameState.Playing`）；`FindNearest()` 遍历敌人列表；`Fire()` 抽象
- [ ] 步骤 2：`Projectile.cs`（Day 2 朴素版）：直线移动、射程耗尽自毁；命中用 Physics：`Rigidbody(isKinematic)` + `SphereCollider(isTrigger)`，`OnTriggerEnter` 判定怪 → 伤害 → 自毁
- [ ] 步骤 3：`WeaponKnife.cs`：冷却 0.8s；`Fire()`= 取最近敌方向 → `Instantiate` 子弹 → 赋值速度/伤害
- [ ] 步骤 4：Play 验证：飞刀自动索敌、命中后怪掉血死亡（Day 2 临时：怪 `Hp<=0` 直接销毁） **[需用户或 MCP]**
- [ ] 步骤 5：commit `feat: weapon base and knife projectile (naive)`

### 任务 2.3：收尾

- [ ] 步骤 1：`docs/思路记录/Day 2-摇杆与飞刀.md`
- [ ] 步骤 2：commit `docs: day2 notes`

---

## Day 3 — 敌人完整化与反馈链路

### 任务 3.1：受击反馈（朴素：独立材质变色）

**文件：**
- 修改：`Assets/Scripts/Enemies/Enemy.cs`
- 修改：`Assets/Scripts/Combat/Projectile.cs`
- 创建：`Assets/Scripts/表现/DamageNumber.cs`

- [ ] 步骤 1：`Enemy.TakeDamage(dmg, knockDir)`：扣血、写入击退速度脉冲（0.15s 衰减）、**闪白（Day 3 朴素版：每怪实例独享材质，`material.SetColor` 变白 0.1s 后恢复——故意破坏合批，Day 6 用 MPB 修复）**；`Hp<=0` → 掉落宝石 + 回收到 Destroy（Day 5 改池）
- [ ] 步骤 2：飘字 `DamageNumber.cs`（朴素 Instantiate）：世界坐标生成、上飘 0.6s 渐隐、销毁；伤害数字 `((int)dmg).ToString()`
- [ ] 步骤 3：宝石 `ExperienceGem` 简单版（创建 `Assets/Scripts/Growth/ExperienceGem.cs`）：落地静止 + 自转；Day 3 无吸附
- [ ] 步骤 4：接触伤害：怪挂 `CapsuleCollider(isTrigger)` + `Rigidbody`；玩家受击（无敌帧 0.5s）扣血；玩家血尽 → `GameManager.EndRun(false)`
- [ ] 步骤 5：Play 验证：完整链路（打怪→掉血/闪白/击退/飘字→死亡掉宝石→怪抓玩家扣血） **[需用户或 MCP]**
- [ ] 步骤 6：commit `feat: enemy feedback loop (naive)`

### 任务 3.2：收尾

- [ ] 步骤 1：`docs/思路记录/Day 3-反馈链路.md`（含"每怪独立材质"是刻意基线这个说明）
- [ ] 步骤 2：commit `docs: day3 notes`

---

## Day 4 — 刷怪管理与性能基线

### 任务 4.1：EnemySpawner + PerfStats

**文件：**
- 创建：`Assets/Scripts/Enemies/EnemySpawner.cs`
- 创建：`Assets/Scripts/Core/PerfStats.cs`
- 修改：`Assets/Scripts/Core/GameBootstrap.cs`（挂 Spawner、PerfStats）

- [ ] 步骤 1：`EnemySpawner.cs`：视野外圆环（半径 = 相机对角 +2）随机点生成；间隔 = `max(1.0 - 0.1·minutes, 0.2)`；怪 HP = `10 × 1.1^minutes`；同屏上限 300（`GameManager.I.Enemies.Count` 判定）
- [ ] 步骤 2：`PerfStats.cs`：`ProfilerRecorder` 记录 "Main Thread" / "Draw Calls Count" / "GC Allocated In Frame"；每 5 秒 `Debug.Log($"[Perf] fps=... drawcall=... gc=...")`——供 MCP 读 Console 抓数据
- [ ] 步骤 3：压力测试（运行 3~5 分钟）：采集朴素基线（帧时间 / DrawCall / GC）→ 存档 `docs/思路记录/Day 4-性能基线.md`（表格记录，附 Profiler 截图 **[需用户]**）
- [ ] 步骤 4：commit `feat: enemy spawner + perf stats; baseline data`

### 任务 4.2：收尾

- [ ] 步骤 1：`docs/思路记录/Day 4-刷怪与基线.md`（曲线公式与理由）
- [ ] 步骤 2：commit `docs: day4 notes`

---

## Day 5 — 性能重构①（对象池 + 手算碰撞）

### 任务 5.1：ObjectPool + 四类接入

**文件：**
- 创建：`Assets/Scripts/Core/ObjectPool.cs`
- 修改：`EnemySpawner.cs`（怪）、`WeaponKnife.cs` + `Projectile.cs`（子弹）、`Enemy.cs`（宝石/飘字生成处）
- 修改：`GameBootstrap.cs`（构建四类模板并创建池）

- [ ] 步骤 1：`ObjectPool<T>`：`Stack<T>` + 预热数（怪 150 / 子弹 50 / 宝石 150 / 飘字 30）+ `Func<T> factory` 或模板 `Instantiate`；`Get()` 激活、`Release()` 失活回池
- [ ] 步骤 2：四类替换：生成处 `pool.Get()`（含出生重置）、消亡处 `Release()`（含 `OnEnable` 重置血量/位置）；删除全部 `Instantiate/Destroy` 运行时代码
- [ ] 步骤 3：Play 验证：行为不变（链路完整），GC Alloc 明显下降 **[需用户或 MCP]**
- [ ] 步骤 4：commit `refactor: object pool for enemies/bullets/gems/damage-numbers`

### 任务 5.2：手算碰撞（弃 Physics）

**文件：**
- 创建：`Assets/Scripts/Core/CircleHit.cs`
- 修改：`Projectile.cs`（移除 Rigidbody/Collider；每帧移动后遍历 `GameManager.I.Enemies` 做 `CircleHit.Hit`，命中即结算）
- 修改：`Enemy.cs`（怪→玩家接触判定改 `CircleHit.Hit`，无敌帧机制不变）

- [ ] 步骤 1：`CircleHit.cs`（20 行，`sqrMagnitude` 比较）
- [ ] 步骤 2：替换命中逻辑；删除所有 `Rigidbody/Collider/OnTrigger*` 代码（怪/子弹/玩家）
- [ ] 步骤 3：Play 验证：行为不变 + 帧率回升（对比数据①）→ 记录 `docs/思路记录/Day 5-对象池与手算碰撞.md`
- [ ] 步骤 4：commit `refactor: circle hit instead of physics`

---

## Day 6 — 性能重构②（空间哈希 + MPB）

### 任务 6.1：SpatialHashGrid + 命中查询替换

**文件：**
- 创建：`Assets/Scripts/Core/SpatialHashGrid.cs`
- 修改：`GameManager.cs`（每帧 `Rebuild`）、`Projectile.cs`（命中改 `Query`）、`Enemy.cs`（`Rebuild` 时机确认）

- [ ] 步骤 1：`SpatialHashGrid`：格子 1m；`key = ((long)cx << 32) | (uint)cy`；`Rebuild(all)`；`Query(pos, r, result)`（结果 List 复用，遍历覆盖格）
- [ ] 步骤 2：`GameManager.Update` 末尾 `Rebuild(GameManager.I.Enemies)`（Playing 态）；子弹命中查询从"全表遍历"改为 `Query(自身, 0.3, tmp)`
- [ ] 步骤 3：Play 验证：命中行为不变 **[需用户或 MCP]**
- [ ] 步骤 4：commit `perf: spatial hash for hit queries`

### 任务 6.2：MPB 闪白（恢复合批）

- [ ] 步骤 1：怪的底色材质改共享（`GameBootstrap.MakeMaterial` 缓存）；闪白用 `MaterialPropertyBlock`（`SetColor("_BaseColor", Color.white)`）→ 不改共享材质
- [ ] 步骤 2：压力测试对比②：300 怪平均 60 FPS 达标；记录数据 + 截图 **[需用户]** → `docs/思路记录/Day 6-空间哈希与合批.md`
- [ ] 步骤 3：commit `perf: mpb flash, shared materials; 300-enemy benchmark`

---

## Day 7 — 成长系统

### 任务 7.1：宝石吸附 + 经验升级

**文件：**
- 修改：`Assets/Scripts/Growth/ExperienceGem.cs`（吸附：玩家距离 < 拾取范围 → Lerp 加速飞向玩家；到达 < 0.3 拾取）
- 创建：`Assets/Scripts/Growth/LevelSystem.cs`
- 修改：`GameManager.cs`（挂 LevelSystem 引用）

- [ ] 步骤 1：`LevelSystem`：`Exp/Level`；需求 `5 + Level×3`；满经验 → `GameManager.EnterLevelUp()` + 生成 3 选项
- [ ] 步骤 2：候选池 = 未满级武器 + 未满级被动（Day 7 只有武器 + 未建成被动时按实际数量）；等权随机
- [ ] 步骤 3：commit `feat: gem magnet and level system`

### 任务 7.2：升级面板 + 大蒜 + 武器管理

**文件：**
- 创建：`Assets/Scripts/UI/LevelUpPanel.cs`（代码构建 3 个按钮：名称+等级文本；点击应用 → `Resume`）
- 创建：`Assets/Scripts/Weapons/WeaponGarlic.cs`（光环：`Query` 自身周围 2.5m → 全部扣血）
- 创建：`Assets/Scripts/Weapons/WeaponManager.cs`（持有列表、`Upgrade` 应用、候选池查询）

- [ ] 步骤 1：面板 UI：全屏半透明遮罩 + 中央标题 + 3 按钮（按钮文案 = 技能名 + Lv）
- [ ] 步骤 2：大蒜武器：玩家初始携带飞刀；Day 7 暂通过升级候选项获得（或 Bootstrap 直接挂 2 个武器——取最简：升级选卡获得）
- [ ] 步骤 3：Play 验证：割怪 → 升级 → 时停选卡 → 变强循环 **[需用户或 MCP]**
- [ ] 步骤 4：commit `feat: levelup panel, garlic, weapon manager`

### 任务 7.3：收尾

- [ ] 步骤 1：`docs/思路记录/Day 7-成长系统.md`
- [ ] 步骤 2：commit `docs: day7 notes`

---

## Day 8 — 构筑与曲线

### 任务 8.1：被动系统 + 武器升级曲线

**文件：**
- 创建：`Assets/Scripts/Player/PlayerStats.cs`（HP/移速/攻击/拾取加成；`ApplyPassive(PassiveType)`）
- 修改：`LevelSystem.cs`（被动加入候选池）、`WeaponBase.cs`（3 级数值：飞刀 +5/级、大蒜 +3/级）

- [ ] 步骤 1：`PlayerStats`：基础 HP 100；被动每级 +10%（攻击加成倍率、移速倍率、拾取范围倍率），各 3 级
- [ ] 步骤 2：刷怪/经验曲线复查（复合增长落实）；一局 10 分钟可玩性首验 **[需用户]**
- [ ] 步骤 3：commit `feat: passives and upgrade curves`

### 任务 8.2：收尾

- [ ] 步骤 1：`docs/思路记录/Day 8-构筑与曲线.md`
- [ ] 步骤 2：commit `docs: day8 notes`

---

## Day 9 — 全流程闭环

### 任务 9.1：HUD（动静分离）

- [ ] 步骤 1：三层 Canvas：① 静态层（血条底/经验条底/按钮背景——一次绘制不变）② 动态层（血条填充、经验数字、计时、击杀数——每帧可能变，脏检查跳更新）③ 面板层（升级/结算/主菜单）
- [ ] 步骤 2：HUD `Update` 轮询 GameManager/PlayerStats 数据
- [ ] 步骤 3：commit `feat: hud with static/dynamic canvas split`

### 任务 9.2：主菜单 + 结算 + 胜利条件

- [ ] 步骤 1：开场遮罩（标题"豆豆幸存者" + 开始按钮）；点击前 `timeScale = 0`
- [ ] 步骤 2：`GameOverPanel`（存活时间 / 击杀数 / 重开按钮 → `SceneManager.LoadScene("SampleScene")`）
- [ ] 步骤 3：10 分钟到时 `EndRun(true)`（胜利，文案区分）
- [ ] 步骤 4：全流程验证：开局 → 割草升级 → 死亡或 10 分钟 → 结算 → 重开，无卡死 **[需用户或 MCP]**
- [ ] 步骤 5：commit `feat: menu, game over, victory; full loop`

### 任务 9.3：收尾

- [ ] 步骤 1：`docs/思路记录/Day 9-全流程.md`
- [ ] 步骤 2：commit `docs: day9 notes`

---

## Day 10 — 交付

### 任务 10.1：构建与报告

- [ ] 步骤 1：PC 构建（`-batchmode -buildTarget Win64` 或用户操作）**[需用户]**
- [ ] 步骤 2：Android APK 构建（环境已确认）**[需用户]**
- [ ] 步骤 3：`docs/perf/性能报告.md`：基线 / 对比①（对象池+手算）/ 对比②（空间哈希+MPB）三列数据表 + Profiler 截图 + 取舍说明
- [ ] 步骤 4：`README.md`：架构图（mermaid）+ 技术点（三件套讲清）+ 运行方式
- [ ] 步骤 5：演示视频（1 分钟；录屏脚本：剪 30s 高潮段 + 30s 技术亮点）**[需用户录或协助]**
- [ ] 步骤 6：更新 `项目交接文档.md`（最终状态）；commit `docs: final deliverables`

---

## 自检记录

- 规格覆盖度：设计 §2 范围表逐项有任务（摇杆 D2 / 双武器 D2+D7 / 三选一 D7 / 刷怪 D4 / 宝石 D3+D7 / 被动 D8 / 3 级 D8 / 性能 D5-6 / 结算 D9 / 主菜单 D9）；验收 5 条对应 D4/D6/D10/D9
- 类型一致性：接口约定节已锁定 `GameManager.Enemies`、`Enemy.TakeDamage`、`WeaponBase.Upgrade` 等签名，各任务引用一致
- 占位符扫描：无 TODO / 待定；"**[需用户]**"为明确的验证回退通道，非占位符
