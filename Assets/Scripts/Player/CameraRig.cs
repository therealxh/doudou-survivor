using UnityEngine;

/// <summary>
/// 正交俯视相机：按固定角度与距离跟随目标，位置平滑插值。
/// </summary>
public class CameraRig : MonoBehaviour
{
    [Header("目标")]
    public Transform Target;

    [Header("偏移参数")]
    public float Distance = 14f;    // 相机到目标的直线距离
    public float Pitch = 55f;       // 俯角
    public float FollowSharp = 10f; // 跟随平滑系数（越大越紧）

    private Vector3 _offset;

    private void Start()
    {
        // 把“俯角 + 距离”换算为固定偏移向量：back 方向旋转 55° 后放大距离
        _offset = Quaternion.Euler(Pitch, 0f, 0f) * Vector3.back * Distance;
        TryFindTarget();
        transform.position = GetTargetPos() + _offset;
    }

    private void LateUpdate()
    {
        if (Target == null)
        {
            TryFindTarget();
            return;
        }
        // 手写平滑跟随：位置向“目标 + 偏移”插值
        transform.position = Vector3.Lerp(transform.position, GetTargetPos() + _offset, Time.deltaTime * FollowSharp);
    }

    private Vector3 GetTargetPos()
    {
        return Target != null ? Target.position : Vector3.zero;
    }

    private void TryFindTarget()
    {
        if (GameBootstrap.Player != null)
        {
            Target = GameBootstrap.Player.transform;
        }
    }
}
