using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 浮动摇杆：按住屏幕任意位置拖动即出现（触屏与鼠标统一）。
/// 输出归一化的 Value（-1..1），供 PlayerController 读取。
/// </summary>
public class FloatingJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    public RectTransform Background; // 摇杆底座（按下时移到手指处）
    public RectTransform Handle;     // 手柄
    public float Radius = 140f;      // 手柄可移动半径（UI 像素）

    /// <summary>当前摇杆输出：方向 × 强度（0..1）。</summary>
    public Vector2 Value { get; private set; }

    private RectTransform _area; // 全屏接收层（本组件所在对象）
    private bool _held;

    private void Awake()
    {
        _area = (RectTransform)transform;
    }

    public void OnPointerDown(PointerEventData e)
    {
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_area, e.position, e.pressEventCamera, out var lp))
        {
            return;
        }

        _held = true;
        Background.gameObject.SetActive(true);
        Handle.gameObject.SetActive(true);
        // 底座与手柄归位到按下的位置
        Background.anchoredPosition = lp;
        Handle.anchoredPosition = lp;
        Value = Vector2.zero;
    }

    public void OnDrag(PointerEventData e)
    {
        if (!_held)
        {
            return;
        }
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_area, e.position, e.pressEventCamera, out var lp))
        {
            return;
        }

        Vector2 delta = lp - Background.anchoredPosition;
        if (delta.sqrMagnitude > Radius * Radius)
        {
            delta = delta.normalized * Radius; // 限制在手柄可移动半径内
        }
        Handle.anchoredPosition = Background.anchoredPosition + delta;

        // 归一化输出 + 死区
        Value = delta / Radius;
        if (Value.sqrMagnitude < 0.01f)
        {
            Value = Vector2.zero;
        }
    }

    public void OnPointerUp(PointerEventData e)
    {
        _held = false;
        Value = Vector2.zero;
        Background.gameObject.SetActive(false);
        Handle.gameObject.SetActive(false);
    }
}
