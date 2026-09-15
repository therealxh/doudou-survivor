using Unity.Profiling;
using UnityEngine;

/// <summary>
/// 性能数据采集（开发验证工具）：每 5 秒向 Console 输出 帧率 / DrawCall / GC Alloc / 敌人数。
/// 供外部工具（MCP）读 Console 自动采集性能基线；发布时可关闭 Enabled。
/// </summary>
public class PerfStats : MonoBehaviour
{
    public bool Enabled = true;
    public float ReportInterval = 5f;

    private ProfilerRecorder _drawCalls;
    private ProfilerRecorder _gcAlloc;

    private float _timer;
    private float _accTime;
    private int _frames;

    private void OnEnable()
    {
        _drawCalls = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Draw Calls Count");
        _gcAlloc = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Allocated In Frame");
    }

    private void OnDisable()
    {
        _drawCalls.Dispose();
        _gcAlloc.Dispose();
    }

    private void Update()
    {
        if (!Enabled)
        {
            return;
        }
        if (GameManager.I == null || GameManager.I.State != GameState.Playing)
        {
            return;
        }

        _accTime += Time.unscaledDeltaTime;
        _frames++;

        _timer += Time.unscaledDeltaTime;
        if (_timer < ReportInterval)
        {
            return;
        }

        float fps = _accTime > 0f ? _frames / _accTime : 0f;
        long dc = _drawCalls.Valid ? _drawCalls.LastValue : -1;
        float gcKb = _gcAlloc.Valid ? _gcAlloc.LastValue / 1024f : -1f;

        Debug.Log($"[Perf] fps={fps:F0} drawCalls={dc} gcKB={gcKb:F1} enemies={GameManager.I.Enemies.Count}");

        _timer = 0f;
        _accTime = 0f;
        _frames = 0;
    }
}
