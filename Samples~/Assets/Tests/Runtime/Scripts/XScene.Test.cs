// Copyright (c) 2025 EFramework Innovation. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

using System;
using NUnit.Framework;
using EFramework.Unity.MVVM;
using UnityEngine.TestTools;
using System.Collections;

/// <summary>
/// TestXScene 是 XScene 的单元测试。
/// </summary>
public class TestXScene
{
    private class MySingletonScene : XScene.Base<MySingletonScene> { }

    [Test]
    public void Singleton()
    {
        // 验证单例模式
        var instance1 = MySingletonScene.Instance;
        var instance2 = MySingletonScene.Instance;

        Assert.IsNotNull(instance1, "单例实例1不应为空。");
        Assert.IsNotNull(instance2, "单例实例2不应为空。");
        Assert.AreSame(instance1, instance2, "两次获取的单例实例应当是同一个对象。");
    }

    [UnityTest]
    public IEnumerator Manage()
    {
        var onSwapCalled = false;
        XScene.OnSwap += () => onSwapCalled = true;
        var testScene1 = new XScene.Base();
        var testScene2 = new XScene.Base();
        XScene.Current = testScene1;

        // 切换到新场景
        XScene.Goto(testScene2, "test");
        Assert.IsNull(XScene.Last, "Last 场景在第一次切换前应当为空。");
        Assert.AreSame(XScene.Current, testScene1, "Current 场景应当仍为初始场景。");
        Assert.AreSame(XScene.Next, testScene2, "Next 场景应当已设置为目标场景");
        Assert.AreEqual("test", XScene.Args[0], "场景参数应当正确传递。");

        // 等一帧Update调用
        yield return null;
        Assert.AreSame(XScene.Current, testScene2, "更新后 Current 场景应当切换到目标场景。");
        Assert.AreSame(XScene.Last, testScene1, "更新后 Last 场景应当为之前的 Current 场景。");
        Assert.IsNull(XScene.Next, "更新后 Next 场景应当为空。");
        Assert.IsNull(XScene.Args, "更新后 Args 应当为空。");
        Assert.IsTrue(onSwapCalled, "场景切换后应当触发 OnSwap 事件。");

        // 测试异常
        var ex = Assert.Throws<Exception>(() => XScene.Goto(new object()), "OnProxy 为空时，传入非 IBase 对象应当抛出异常。");
        Assert.AreEqual("OnProxy is null", ex.Message, "异常消息应当指示 OnProxy 为空。");

        XScene.OnProxy = (obj) => obj as XScene.IBase;
        Assert.DoesNotThrow(() => XScene.Goto(new object()));
    }
}
