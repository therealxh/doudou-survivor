using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 通用对象池（三件套①）：Stack 复用对象，运行时零 Instantiate/Destroy。
/// 供怪 / 子弹 / 宝石 / 飘字四类共用；factory 决定"新对象怎么造"。
/// </summary>
public class ObjectPool<T> where T : Component
{
    private readonly Stack<T> _idle = new Stack<T>();
    private readonly Func<T> _factory;

    /// <param name="factory">构建新对象的方式（通常从模板实例化）</param>
    /// <param name="prewarm">预热数量：提前建好 N 个，避免游戏中途卡顿</param>
    public ObjectPool(Func<T> factory, int prewarm)
    {
        _factory = factory;
        for (int i = 0; i < prewarm; i++)
        {
            T o = _factory();
            o.gameObject.SetActive(false);
            _idle.Push(o);
        }
    }

    /// <summary>取出一个对象（自动激活）。</summary>
    public T Get()
    {
        T o = _idle.Count > 0 ? _idle.Pop() : _factory();
        o.gameObject.SetActive(true);
        return o;
    }

    /// <summary>回收一个对象（自动失活）。</summary>
    public void Release(T o)
    {
        o.gameObject.SetActive(false);
        _idle.Push(o);
    }
}
