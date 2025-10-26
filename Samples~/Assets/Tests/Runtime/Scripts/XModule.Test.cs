// Copyright (c) 2025 EFramework Innovation. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

using NUnit.Framework;
using EFramework.Unity.MVVM;
using UnityEngine.TestTools;
using UnityEngine;
using System.Text.RegularExpressions;

/// <summary>
/// TestXModule 是 XModule 的单元测试。
/// </summary>
public class TestXModule
{
    private enum MyModuleEventType
    {
        Event1,
        Event2,
        Event3,
        Event4,
        Event5,
    }

    private class MyModule : XModule.Base { }

    private class MyInvalidModule { }

    private class MySingletonModule : XModule.Base<MySingletonModule> { }

    private class MyModuleEventRegister
    {
        [XModule.Event(MyModuleEventType.Event1, typeof(MyModule), false)]
        public void Func1() { }

        [XModule.Event(MyModuleEventType.Event2, typeof(MyInvalidModule), true)]
        protected void Func2() { }
    }

    private class MySubModuleEventRegister : MyModuleEventRegister
    {
        [XModule.Event(MyModuleEventType.Event3, typeof(MySingletonModule), false)]
        public bool Func3(int a, bool b) { return false; }

        [XModule.Event(MyModuleEventType.Event4, typeof(MySingletonModule), true)]
        public static void Func4() { }

        [XModule.Event(MyModuleEventType.Event5, null, true)]
        public void Func5(object[] args) { }
    }

    private XModule.Base testModule;

    [SetUp]
    public void Setup() { testModule = new XModule.Base(); }

    [TearDown]
    public void Reset() { testModule = null; }

    [Test]
    public void Property()
    {
        // 验证基本属性
        Assert.AreEqual(testModule.Name, testModule.GetType().Name, "模块名称默认应当为类型名称");
        testModule.name = "TestModule";
        Assert.AreEqual(testModule.Name, "TestModule", "设置name后Name属性应当正确反映");

        Assert.AreEqual(testModule.Enabled, false, "模块初始状态应当为未启用");
        Assert.IsNotNull(testModule.Event, "模块的事件系统不应为空");
        Assert.IsNotNull(testModule.Tags, "模块的标签不应为空");
        Assert.AreEqual(testModule.Tags.Get("Name"), "TestModule", "模块标签中的Name应当与模块名称一致");
        Assert.IsNotNull(testModule.Tags.Get("Hash"), "模块标签中应当包含Hash值");
    }

    [Test]
    public void Lifecycle()
    {
        // 验证基础生命周期方法
        LogAssert.Expect(LogType.Log, new Regex(@"Module has been awaked\."));
        LogAssert.Expect(LogType.Log, new Regex(@"Module has been started\."));
        LogAssert.Expect(LogType.Log, new Regex(@"Module has been reseted\."));
        LogAssert.Expect(LogType.Log, new Regex(@"Module has been stopped\."));

        testModule.Awake();
        testModule.Start();
        Assert.IsTrue(testModule.Enabled, "调用Start后模块应当处于启用状态");
        testModule.Reset();
        testModule.Stop();
        Assert.IsFalse(testModule.Enabled, "调用Stop后模块应当处于未启用状态");
    }

    [Test]
    public void Event()
    {
        var eventTriggered = false;
        void callback() => eventTriggered = true;

        // 验证注册事件
        testModule.Event.Register(1, callback);
        testModule.Event.Notify(1);
        Assert.IsTrue(eventTriggered, "注册事件后触发通知应当调用回调函数");

        // 验证注销事件
        testModule.Event.Unregister(1, callback);
        eventTriggered = false;
        testModule.Event.Notify(1);
        Assert.IsFalse(eventTriggered, "注销事件后触发通知不应当调用回调函数");

        // 验证事件特性
        LogAssert.Expect(LogType.Error, new Regex(Regex.Escape("XModule.Event: unable to find instance of module TestXModule+MyModule.")));
        LogAssert.Expect(LogType.Error, new Regex(Regex.Escape("XModule.Event: module TestXModule+MyInvalidModule does not implements EFramework.Unity.MVVM.XModule+IBase.")));

        var events = XModule.Event.Get(typeof(MySubModuleEventRegister));
        Assert.AreEqual(3, events.Count, "MySubModuleEventRegister 类型标记的事件特性数量应为 3。");

        Assert.AreEqual(events[0].ID, MyModuleEventType.Event3.GetHashCode(), "事件特性 Func3 的事件标识应当为：{0}。", MyModuleEventType.Event3);
        Assert.AreEqual(events[0].Callback.Name, "Func3", "事件特性 Func3 应当支持带返回值及有参签名的函数。");
        Assert.AreEqual(events[0].Target, MySingletonModule.Instance, "事件特性 Func3 指定的模块实例应当和 {0} 相等。", MySingletonModule.Instance);
        Assert.AreEqual(events[0].Once, false, "事件特性 Func3 指定的回调一次参数应当为 {0}。", false);

        Assert.AreEqual(events[1].ID, MyModuleEventType.Event4.GetHashCode(), "事件特性 Func4 的事件标识应当为：{0}。", MyModuleEventType.Event4);
        Assert.AreEqual(events[1].Callback.Name, "Func4", "事件特性 Func4 应当支持无返回值及无参签名的静态函数。");
        Assert.AreEqual(events[1].Target, MySingletonModule.Instance, "事件特性 Func4 指定的模块实例应当和 {0} 相等。", MySingletonModule.Instance);
        Assert.AreEqual(events[1].Once, true, "事件特性 Func4 指定的回调一次参数应当为 {0}。", true);

        Assert.AreEqual(events[2].ID, MyModuleEventType.Event5.GetHashCode(), "事件特性 Func4 的事件标识应当为：{0}。", MyModuleEventType.Event5);
        Assert.AreEqual(events[2].Callback.Name, "Func5", "事件特性 Func5 应当支持无返回值及无参签名的静态函数。");
        Assert.AreEqual(events[2].Target, null, "事件特性 Func5 指定的模块实例应当为空。");
        Assert.AreEqual(events[2].Once, true, "事件特性 Func5 指定的回调一次参数应当为 {0}。", true);
    }

    [Test]
    public void Singleton()
    {
        // 验证单例模式
        var instance1 = MySingletonModule.Instance;
        var instance2 = MySingletonModule.Instance;

        Assert.IsNotNull(instance1, "单例实例1不应为空");
        Assert.IsNotNull(instance2, "单例实例2不应为空");
        Assert.AreSame(instance1, instance2, "两次获取的单例实例应当是同一个对象");
    }
}
