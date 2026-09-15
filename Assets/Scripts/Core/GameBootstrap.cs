using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 运行时入口：全部游戏对象由代码构建，不依赖场景摆放，也无需 prefab。
/// 每次进入 SampleScene（含“重开”时的场景重载）都会重新 Build 一次。
/// </summary>
public static class GameBootstrap
{
    /// <summary>当前玩家对象（供敌人、相机读取）。</summary>
    public static GameObject Player { get; private set; }

    /// <summary>飞刀子弹模板（未激活，供 Instantiate / Day 5 对象池使用）。</summary>
    public static GameObject KnifeTemplate { get; private set; }

    /// <summary>经验宝石模板。</summary>
    public static GameObject GemTemplate { get; private set; }

    /// <summary>伤害飘字模板（TextMesh）。</summary>
    public static GameObject DamageNumberTemplate { get; private set; }

    /// <summary>手雷模板（Day 7 升级获取的武器）。</summary>
    public static GameObject GrenadeTemplate { get; private set; }

    /// <summary>爆炸视觉模板（Day 8：手雷爆炸圈）。</summary>
    public static GameObject BlastTemplate { get; private set; }

    /// <summary>敌人模板（Spawner 批量生成用）。</summary>
    public static GameObject EnemyTemplate { get; private set; }

    /// <summary>对象池（三件套①）：怪 / 子弹 / 宝石 / 飘字。</summary>
    public static ObjectPool<Enemy> EnemyPool { get; private set; }
    public static ObjectPool<Projectile> KnifePool { get; private set; }
    public static ObjectPool<ExperienceGem> GemPool { get; private set; }
    public static ObjectPool<DamageNumber> DamageNumberPool { get; private set; }
    public static ObjectPool<Grenade> GrenadePool { get; private set; } // Day 7：手雷
    public static ObjectPool<BlastEffect> BlastPool { get; private set; } // Day 8：爆炸视觉

    /// <summary>浮动摇杆（Bootstrap 创建后注入 PlayerController）。</summary>
    public static FloatingJoystick Joystick { get; private set; }

    private static readonly Dictionary<Color, Material> MatCache = new Dictionary<Color, Material>();
    private static readonly Dictionary<string, Sprite> SpriteCache = new Dictionary<string, Sprite>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Init()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        Build();
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Build();
    }

    private static void Build()
    {
        SetupCamera();
        BuildManagers(); // 必须先于敌人生成：敌人 OnEnable 时要向 GameManager 注册
        BuildGround();
        BuildUI();       // Canvas + EventSystem + 摇杆 + 提示
        BuildTemplates(); // 各类对象模板
        BuildPools();     // 对象池（四类）
        BuildPlayer();   // 玩家 + 武器 + 摇杆引用
    }

    // ---------- 相机 ----------
    private static void SetupCamera()
    {
        var cam = Camera.main;
        if (cam == null)
        {
            var go = new GameObject("Main Camera");
            go.tag = "MainCamera";
            cam = go.AddComponent<Camera>();
        }

        cam.orthographic = true;
        cam.orthographicSize = 6f; // 垂直半高 6 世界单位
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.10f, 0.12f, 0.16f);
        cam.transform.rotation = Quaternion.Euler(40f, 0f, 0f); // 俯角 40°（2.5D 斜视角，减少直立 sprite 的纵向压缩）

        if (cam.GetComponent<CameraRig>() == null)
        {
            cam.gameObject.AddComponent<CameraRig>();
        }
    }

    // ---------- 管理器 ----------
    private static void BuildManagers()
    {
        if (GameManager.I == null)
        {
            var gmGo = new GameObject("GameManager");
            var gm = gmGo.AddComponent<GameManager>();
            gmGo.AddComponent<EnemySpawner>(); // Day 4 起由刷怪管理器接管敌人生成
            gmGo.AddComponent<PerfStats>();    // 性能数据采集（每 5s 输出 Console）
            gm.Levels = gmGo.AddComponent<LevelSystem>(); // Day 7：成长系统（经验/升级三选一）
        }
    }

    // ---------- 地面 ----------
    private static void BuildGround()
    {
        var ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ground.name = "Ground";
        ground.transform.position = Vector3.zero;
        ground.transform.localScale = new Vector3(60f, 0.5f, 60f);
        ground.GetComponent<Renderer>().sharedMaterial = MakeMaterial(new Color(0.16f, 0.30f, 0.20f));
        Object.Destroy(ground.GetComponent<Collider>()); // 本游戏全手算碰撞，不需要物理体
    }

    // ---------- UI（全代码构建：摇杆 + 提示） ----------
    private static void BuildUI()
    {
        // EventSystem：UGUI 事件必需（模板场景里没有）
        if (Object.FindObjectOfType<EventSystem>() == null)
        {
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        }

        var canvasGo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f); // 竖屏参考分辨率
        scaler.matchWidthOrHeight = 0.5f;

        // 全屏摇杆接收层（透明，负责接收按下/拖动）
        var areaGo = NewUIObject("JoystickArea", canvasGo.transform);
        StretchFull((RectTransform)areaGo.transform);
        var areaImg = areaGo.AddComponent<Image>();
        areaImg.color = new Color(0f, 0f, 0f, 0f);

        // 摇杆底座（方块风格，与几何体美术统一）
        var bgGo = NewUIObject("JoystickBg", areaGo.transform);
        var bgRt = (RectTransform)bgGo.transform;
        bgRt.sizeDelta = new Vector2(300f, 300f);
        var bgImg = bgGo.AddComponent<Image>();
        bgImg.color = new Color(1f, 1f, 1f, 0.15f);
        bgGo.SetActive(false);

        // 摇杆手柄
        var handleGo = NewUIObject("JoystickHandle", areaGo.transform);
        var handleRt = (RectTransform)handleGo.transform;
        handleRt.sizeDelta = new Vector2(120f, 120f);
        var handleImg = handleGo.AddComponent<Image>();
        handleImg.color = new Color(1f, 1f, 1f, 0.55f);
        handleGo.SetActive(false);

        var joy = areaGo.AddComponent<FloatingJoystick>();
        joy.Background = bgRt;
        joy.Handle = handleRt;
        Joystick = joy;

        // 操作提示（临时，Day 9 主菜单替换）
        var hintGo = NewUIObject("Hint", canvasGo.transform);
        var hintRt = (RectTransform)hintGo.transform;
        hintRt.anchorMin = new Vector2(0.5f, 1f);
        hintRt.anchorMax = new Vector2(0.5f, 1f);
        hintRt.pivot = new Vector2(0.5f, 1f);
        hintRt.anchoredPosition = new Vector2(0f, -80f);
        hintRt.sizeDelta = new Vector2(900f, 100f);
        var hint = hintGo.AddComponent<Text>();
        hint.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        hint.fontSize = 44;
        hint.alignment = TextAnchor.UpperCenter;
        hint.color = new Color(1f, 1f, 1f, 0.75f);
        hint.text = "WASD / 按住屏幕拖动 移动";

        BuildLevelUpPanel(canvasGo.transform); // Day 7：升级三选一面板（默认隐藏）
    }

    // ---------- 升级三选一面板（Day 7） ----------
    private static void BuildLevelUpPanel(Transform canvas)
    {
        var panelRoot = NewUIObject("LevelUpPanel", canvas);
        StretchFull((RectTransform)panelRoot.transform);
        var mask = panelRoot.AddComponent<Image>();
        mask.color = new Color(0f, 0f, 0f, 0.62f); // 半透明遮罩（同时阻挡下层摇杆输入）

        var panel = panelRoot.AddComponent<LevelUpPanel>();
        var btns = new Button[3];
        var titles = new Text[3];
        var descs = new Text[3];

        NewText(panelRoot.transform, "升级！选择一项强化", 54, new Vector2(0f, 470f), new Color(1f, 0.95f, 0.6f));

        for (int i = 0; i < 3; i++)
        {
            var bGo = NewUIObject("Option" + i, panelRoot.transform);
            var bRt = (RectTransform)bGo.transform;
            bRt.sizeDelta = new Vector2(780f, 200f);
            bRt.anchoredPosition = new Vector2(0f, 230f - i * 240f);
            var img = bGo.AddComponent<Image>();
            img.color = new Color(0.13f, 0.18f, 0.28f, 0.96f);
            var btn = bGo.AddComponent<Button>();
            btns[i] = btn;

            titles[i] = NewText(bGo.transform, "", 46, new Vector2(0f, 40f), Color.white);
            descs[i] = NewText(bGo.transform, "", 32, new Vector2(0f, -42f), new Color(0.75f, 0.80f, 0.85f));

            int idx = i;
            btn.onClick.AddListener(() => panel.OnPick(idx));
        }

        panel.Wire(panelRoot, btns, titles, descs);
        panelRoot.SetActive(false);
    }

    /// <summary>快速创建一个居中 UI 文本。</summary>
    private static Text NewText(Transform parent, string content, int size, Vector2 anchoredPos, Color color)
    {
        var go = NewUIObject("Text", parent);
        var rt = (RectTransform)go.transform;
        rt.sizeDelta = new Vector2(760f, 90f);
        rt.anchoredPosition = anchoredPos;
        var t = go.AddComponent<Text>();
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.fontSize = size;
        t.alignment = TextAnchor.MiddleCenter;
        t.color = color;
        t.text = content;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        return t;
    }

    // ---------- 对象模板 ----------
    private static void BuildTemplates()
    {
        // 子弹模板（Day 5 起全手算碰撞，无需 Collider/Rigidbody）
        KnifeTemplate = MakeSprite("arrow", 0.6f, false);
        KnifeTemplate.AddComponent<Projectile>();
        KnifeTemplate.SetActive(false); // 模板不参与游戏，仅供池实例化

        // 敌人模板（Spawner 批量生成用）
        EnemyTemplate = MakeSprite("enemy", 1.0f, true);
        EnemyTemplate.AddComponent<Enemy>();
        EnemyTemplate.SetActive(false);

        // 经验宝石模板
        GemTemplate = MakeBox("GemTemplate", new Vector3(0.3f, 0.3f, 0.3f), new Color(0.25f, 0.85f, 0.90f));
        GemTemplate.AddComponent<ExperienceGem>();
        GemTemplate.SetActive(false);

        // 手雷模板（Day 7：升级获取的武器，飞行体到达后范围爆炸）
        GrenadeTemplate = MakeSprite("bomb", 0.5f, false);
        GrenadeTemplate.AddComponent<Grenade>();
        GrenadeTemplate.SetActive(false);

        // 爆炸视觉模板（Day 8：手雷爆炸圈）
        BlastTemplate = MakeSprite("explosion", 1f, false);
        BlastTemplate.AddComponent<BlastEffect>();
        BlastTemplate.SetActive(false);

        // 伤害飘字模板（TextMesh 世界空间文本）
        DamageNumberTemplate = new GameObject("DamageNumberTemplate");
        var tm = DamageNumberTemplate.AddComponent<TextMesh>();
        tm.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        tm.fontSize = 40;
        tm.characterSize = 0.08f;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.alignment = TextAlignment.Center;
        tm.color = Color.white;
        DamageNumberTemplate.GetComponent<MeshRenderer>().sharedMaterial = tm.font.material;
        DamageNumberTemplate.transform.rotation = Quaternion.Euler(40f, 0f, 0f); // 面向相机
        DamageNumberTemplate.AddComponent<DamageNumber>();
        DamageNumberTemplate.SetActive(false);
    }

    // ---------- 对象池 ----------
    private static void BuildPools()
    {
        EnemyPool = new ObjectPool<Enemy>(() => Object.Instantiate(EnemyTemplate).GetComponent<Enemy>(), 150);
        KnifePool = new ObjectPool<Projectile>(() => Object.Instantiate(KnifeTemplate).GetComponent<Projectile>(), 50);
        GemPool = new ObjectPool<ExperienceGem>(() => Object.Instantiate(GemTemplate).GetComponent<ExperienceGem>(), 150);
        DamageNumberPool = new ObjectPool<DamageNumber>(() => Object.Instantiate(DamageNumberTemplate).GetComponent<DamageNumber>(), 30);
        GrenadePool = new ObjectPool<Grenade>(() => Object.Instantiate(GrenadeTemplate).GetComponent<Grenade>(), 10);
        BlastPool = new ObjectPool<BlastEffect>(() => Object.Instantiate(BlastTemplate).GetComponent<BlastEffect>(), 8);
    }

    // ---------- 玩家 ----------
    private static void BuildPlayer()
    {
        Player = MakeSprite("player", 1.5f, true);
        Player.transform.position = new Vector3(0f, 0.5f, 0f);

        var pc = Player.AddComponent<PlayerController>();
        pc.Radius = 0.5f;
        pc.MoveSpeed = 5f;
        pc.Joystick = Joystick;

        var stats = Player.AddComponent<PlayerStats>();
        stats.Radius = 0.5f; // 手算接触判定半径

        Player.AddComponent<WeaponKnife>();
        // 大蒜/手雷改为成长系统获取（Day 7：升级三选一）
    }

    // ---------- 贴图与 Sprite（美术资源） ----------
    /// <summary>运行时加载贴图并创建 Sprite（走 Resources，不依赖导入设置）。</summary>
    public static Sprite LoadSprite(string name)
    {
        if (SpriteCache.TryGetValue(name, out var cached) && cached != null)
        {
            return cached;
        }
        var tex = Resources.Load<Texture2D>("Sprites/" + name);
        if (tex == null)
        {
            Debug.LogWarning("[Bootstrap] 未找到贴图: Sprites/" + name);
            return null;
        }
        // PPU = 纹理宽：贴图世界宽度 = 1 单位（再按 size 参数缩放）
        var sp = Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height), new Vector2(0.5f, 0.5f), tex.width);
        SpriteCache[name] = sp;
        return sp;
    }

    /// <summary>
    /// 创建 2.5D Sprite 实体；size = 最长边的世界尺寸（米）。
    /// standing=true：直立小人（脚踩地面；朝向用 flipX，不旋转）；false：中心锚平躺（如箭矢，由整体旋转控制）。
    /// </summary>
    public static GameObject MakeSprite(string spriteName, float size, bool standing = false)
    {
        var root = new GameObject(spriteName);
        var visual = new GameObject("Visual");
        visual.transform.SetParent(root.transform, false);

        var sr = visual.AddComponent<SpriteRenderer>();
        sr.sprite = LoadSprite(spriteName);

        float h = 1f;
        if (sr.sprite != null)
        {
            float longest = Mathf.Max(sr.sprite.bounds.size.x, sr.sprite.bounds.size.y);
            float s = size / longest;
            visual.transform.localScale = new Vector3(s, s, s);
            h = sr.sprite.bounds.size.y * s; // 缩放后的世界高
        }

        if (standing)
        {
            // 直立：图的底边贴地面顶（实体 y=0.5、地面顶 y=0.25 → 底边 local y = -0.25）
            visual.transform.localPosition = new Vector3(0f, -0.25f + h * 0.5f, 0f);
            // 脚下投影阴影（强化 2.5D 立体感）
            var shadow = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            shadow.name = "Shadow";
            Object.Destroy(shadow.GetComponent<Collider>());
            shadow.transform.SetParent(root.transform, false);
            shadow.transform.localPosition = new Vector3(0f, -0.245f, 0f);
            shadow.transform.localScale = new Vector3(0.62f, 0.004f, 0.62f);
            shadow.GetComponent<Renderer>().sharedMaterial = MakeMaterial(new Color(0.05f, 0.10f, 0.07f));
        }
        return root;
    }

    /// <summary>平躺 Sprite 的朝向旋转：图“上”指向 dir 的水平方向。</summary>
    public static Quaternion FlatRotation(Vector3 dir)
    {
        float yaw = Mathf.Atan2(-dir.x, -dir.z) * Mathf.Rad2Deg;
        return Quaternion.Euler(0f, yaw, 0f) * Quaternion.Euler(-90f, 0f, 0f);
    }

    // ---------- 公共工具 ----------
    /// <summary>按颜色缓存材质：同色共享同一实例（利于合批）。</summary>
    public static Material MakeMaterial(Color c)
    {
        if (MatCache.TryGetValue(c, out var m) && m != null)
        {
            return m;
        }
        m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        m.SetColor("_BaseColor", c);
        MatCache[c] = m;
        return m;
    }

    /// <summary>快速创建一个纯色方块；keepCollider=false 时移除碰撞体。</summary>
    public static GameObject MakeBox(string name, Vector3 scale, Color color, bool keepCollider = false)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.localScale = scale;
        go.GetComponent<Renderer>().sharedMaterial = MakeMaterial(color);
        if (!keepCollider)
        {
            Object.Destroy(go.GetComponent<Collider>());
        }
        return go;
    }

    private static GameObject NewUIObject(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go;
    }

    private static void StretchFull(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}
