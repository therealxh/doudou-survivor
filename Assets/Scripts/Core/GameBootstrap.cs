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

    /// <summary>浮动摇杆（Bootstrap 创建后注入 PlayerController）。</summary>
    public static FloatingJoystick Joystick { get; private set; }

    private static readonly Dictionary<Color, Material> MatCache = new Dictionary<Color, Material>();

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
        BuildTemplates(); // 子弹模板
        BuildPlayer();   // 玩家 + 武器 + 摇杆引用
        BuildTestEnemies(); // Day 1 临时：5 只测试怪（Day 4 起由 Spawner 接管）
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
        cam.transform.rotation = Quaternion.Euler(55f, 0f, 0f); // 俯角 55°

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
            new GameObject("GameManager").AddComponent<GameManager>();
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
    }

    // ---------- 对象模板 ----------
    private static void BuildTemplates()
    {
        // 子弹模板：Rigidbody(kinematic) + Trigger Collider，供朴素版 Physics 命中（Day 5 弃用 Physics）
        KnifeTemplate = MakeBox("KnifeTemplate", new Vector3(0.4f, 0.25f, 0.4f), new Color(0.95f, 0.92f, 0.6f), keepCollider: true);
        KnifeTemplate.GetComponent<Collider>().isTrigger = true;
        var rb = KnifeTemplate.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        KnifeTemplate.AddComponent<Projectile>();
        KnifeTemplate.SetActive(false); // 模板不参与游戏，仅供实例化
    }

    // ---------- 玩家 ----------
    private static void BuildPlayer()
    {
        Player = MakeBox("Player", new Vector3(1f, 1f, 1f), new Color(0.30f, 0.55f, 0.95f), keepCollider: true);
        Player.transform.position = new Vector3(0f, 0.5f, 0f);

        var pc = Player.AddComponent<PlayerController>();
        pc.Radius = 0.5f;
        pc.MoveSpeed = 5f;
        pc.Joystick = Joystick;

        Player.AddComponent<WeaponKnife>();
    }

    // ---------- 测试怪（Day 1 临时） ----------
    private static void BuildTestEnemies()
    {
        for (int i = 0; i < 5; i++)
        {
            float ang = i * Mathf.PI * 2f / 5f;
            var e = MakeBox("Enemy_" + i, new Vector3(0.8f, 0.8f, 0.8f), new Color(0.85f, 0.32f, 0.30f), keepCollider: true);
            e.transform.position = new Vector3(Mathf.Cos(ang) * 10f, 0.5f, Mathf.Sin(ang) * 10f);
            e.AddComponent<Enemy>();
        }
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
