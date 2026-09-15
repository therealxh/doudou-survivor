using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 空间哈希（三件套②）：敌人按位置分格存储，命中查询只遍历附近格子。
/// 设计取舍：每帧全量重建（300 次插入/帧成本可忽略），换取"移动时维护格子归属"的复杂度免除。
/// </summary>
public class SpatialHashGrid
{
    public const float CellSize = 1f;

    private readonly Dictionary<long, List<Enemy>> _cells = new Dictionary<long, List<Enemy>>();

    /// <summary>每帧重建：清空所有格子的 List（复用、零分配）后把敌人全量插入。</summary>
    public void Rebuild(IReadOnlyList<Enemy> enemies)
    {
        foreach (var list in _cells.Values)
        {
            list.Clear();
        }

        for (int i = 0; i < enemies.Count; i++)
        {
            var e = enemies[i];
            if (e == null)
            {
                continue;
            }
            long key = KeyOf(e.transform.position);
            if (!_cells.TryGetValue(key, out var list))
            {
                list = new List<Enemy>();
                _cells[key] = list;
            }
            list.Add(e);
        }
    }

    /// <summary>
    /// 查询：把中心点 ± r 覆盖到的格子里的敌人收集进 result（先 Clear）。
    /// result 是"候选集"（可能含圆外的），调用方需再用 CircleHit 精筛。
    /// </summary>
    public void Query(Vector3 center, float r, List<Enemy> result)
    {
        result.Clear();

        int minX = Mathf.FloorToInt((center.x - r) / CellSize);
        int maxX = Mathf.FloorToInt((center.x + r) / CellSize);
        int minZ = Mathf.FloorToInt((center.z - r) / CellSize);
        int maxZ = Mathf.FloorToInt((center.z + r) / CellSize);

        for (int x = minX; x <= maxX; x++)
        {
            for (int z = minZ; z <= maxZ; z++)
            {
                long key = ((long)x << 32) | (uint)z;
                if (_cells.TryGetValue(key, out var list))
                {
                    result.AddRange(list);
                }
            }
        }
    }

    /// <summary>格子坐标编码：高 32 位放 x、低 32 位放 z（无碰撞）。</summary>
    private static long KeyOf(Vector3 p)
    {
        int cx = Mathf.FloorToInt(p.x / CellSize);
        int cz = Mathf.FloorToInt(p.z / CellSize);
        return ((long)cx << 32) | (uint)cz;
    }
}
