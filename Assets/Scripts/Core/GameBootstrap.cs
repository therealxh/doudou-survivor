using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 运行时入口：全部游戏对象由代码构建，不依赖场景摆放，也无需 prefab。
/// 每次进入 SampleScene（含“重开”时的场景重载）都会重新 Build 一次。
/// </summary>
public static class GameBootstrap
{
    /// <summary>当前玩家对象（供敌人、相机读取）。</summary>
    public static GameObject Player { get; private set; }

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
        BuildPlayer();
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

    // ---------- 玩家 ----------
    private static void BuildPlayer()
    {
        Player = MakeBox("Player", new Vector3(1f, 1f, 1f), new Color(0.30f, 0.55f, 0.95f));
        Player.transform.position = new Vector3(0f, 0.5f, 0f);

        var pc = Player.AddComponent<PlayerController>();
        pc.Radius = 0.5f;
        pc.MoveSpeed = 5f;
    }

    // ---------- 测试怪（Day 1 临时） ----------
    private static void BuildTestEnemies()
    {
        for (int i = 0; i < 5; i++)
        {
            float ang = i * Mathf.PI * 2f / 5f;
            var e = MakeBox("Enemy_" + i, new Vector3(0.8f, 0.8f, 0.8f), new Color(0.85f, 0.32f, 0.30f));
            e.transform.position = new Vector3(Mathf.Cos(ang), 0.5f, Mathf.Sin(ang)) * 10f;
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

    /// <summary>快速创建一个纯色方块（自动移除碰撞体）。</summary>
    public static GameObject MakeBox(string name, Vector3 scale, Color color)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.localScale = scale;
        go.GetComponent<Renderer>().sharedMaterial = MakeMaterial(color);
        Object.Destroy(go.GetComponent<Collider>());
        return go;
    }
}
