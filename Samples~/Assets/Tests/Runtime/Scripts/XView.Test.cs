// Copyright (c) 2025 EFramework Innovation. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

using System;
using System.Collections.Generic;
using System.Collections;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using NUnit.Framework;
using EFramework.Unity.MVVM;
using EFramework.Unity.Utility;

/// <summary>
/// TestXView 是 XView 的单元测试。
/// </summary>
public class TestXView
{
    #region 视图测试准备

    private class MyHandler : XView.IHandler
    {
        public XView.IBase lastFocusedView;
        public List<XView.IBase> viewOrder = new();
        public XView.IBase lastSetOrderView;
        public int lastSetOrderValue;
        public object[] bindingView = new object[3];

        public void Load(XView.IMeta meta, Transform parent, out XView.IBase view, out GameObject panel)
        {
            panel = new GameObject(meta.Path);
            if (parent) panel.transform.SetParent(parent, false);

            var myView = panel.AddComponent<MyView>();
            myView.Meta = meta;
            myView.Panel = panel;
            view = myView;

            viewOrder.Add(view);
        }

        public void LoadAsync(XView.IMeta meta, Transform parent, Action<XView.IBase, GameObject> callback)
        {
            var panel = new GameObject(meta.Path);
            if (parent) panel.transform.SetParent(parent, false);

            var mockView = panel.AddComponent<MyView>();
            mockView.Meta = meta;
            mockView.Panel = panel;
            viewOrder.Add(mockView);
            callback?.Invoke(mockView, panel);
        }

        public bool IsLoading(XView.IMeta meta) { return false; }

        public void SetBinding(GameObject go, object target, XView.Element[] elements)
        {
            bindingView[0] = go;
            bindingView[1] = target;
            bindingView[2] = elements;
        }

        public void SetOrder(XView.IBase view, int order)
        {
            if (!viewOrder.Contains(view)) viewOrder.Add(view);

            // 记录最后一次设置的顺序
            lastSetOrderView = view;
            lastSetOrderValue = order;
        }

        public void SetFocus(XView.IBase view, bool focus)
        {
            if (focus) lastFocusedView = view;
        }
    }

    private class MyModule : XModule.Base<MyModule> { }

    private enum MyEvent
    {
        Event1,
        Event2,
        Event3,
        Event4,
    }

    private class MyView : XView.Base
    {
        public Action OnOpenCallback;

        public override void OnOpen(params object[] args)
        {
            base.OnOpen(args);
            OnOpenCallback?.Invoke();
        }

        public Action OnFocusCallback;

        public override void OnFocus()
        {
            base.OnFocus();
            OnFocusCallback?.Invoke();
        }

        public Action OnBlurCallback;

        public override void OnBlur()
        {
            base.OnBlur();
            OnBlurCallback?.Invoke();
        }

        public Action OnCloseCallback;

        public override void OnClose(Action done)
        {
            OnCloseCallback?.Invoke();
            done?.Invoke();
            base.OnClose(done);
        }

        public bool OnEvent2Called = false;

        public int OnEvent2Param;

        [XModule.Event(MyEvent.Event2, typeof(MyModule), true)]
        public void OnEvent2(int param) { OnEvent2Called = true; OnEvent2Param = param; }
    }

    [XView.Element("MyModularView Class Attr")]
    [XView.Element("MyModularView Class Attr Extras", "Hello MyModularView")]
    private class MyModularView : XView.Base<MyModule>
    {
        public bool OnEvent3Called = false;

        public int OnEvent3Param;

        [XModule.Event(MyEvent.Event3)]
        [XView.Element("MyModularView Method Attr")]
        public void OnEvent3(int param) { OnEvent3Called = true; OnEvent3Param = param; }
    }

    [XView.Element("MySubView Class Attr")]
    [XView.Element("MySubView Class Attr Extras", "Hello MySubView")]
    private class MySubView : MyModularView
    {
        [XView.Element("MySubView Field Attr")]
        public bool OnEvent4Called = false;

        [XView.Element("MySubView Field Attr Extras", "Hello OnEvent4Param1")]
        public int OnEvent4Param1;

        public bool OnEvent4Param2;

        [XModule.Event(MyEvent.Event4)]
        [XView.Element("MySubView Method Attr Extras", "Hello OnEvent4")]
        public void OnEvent4(int param1, bool param2) { OnEvent4Called = true; OnEvent4Param1 = param1; OnEvent4Param2 = param2; }
    }

    private XView.Meta testMeta;
    private GameObject testPanel;
    private MyHandler myHandler;

    [SetUp]
    public void Setup()
    {
        testMeta = new XView.Meta("TestView");
        testPanel = new GameObject("TestPanel");
        myHandler = new MyHandler();
        XView.Initialize(myHandler);
    }

    [TearDown]
    public void Reset()
    {
        if (testPanel != null) UnityEngine.Object.Destroy(testPanel);
        XView.DestroyAll();
    }

    #endregion

    #region 基础视图测试

    [Test]
    public void Meta()
    {
        // 验证Meta基本属性
        var meta = new XView.Meta("TestView", 10, XView.EventType.Dynamic, XView.CacheType.None, true);
        Assert.AreEqual("TestView", meta.Path, "Meta的Path属性应当正确设置为指定值");
        Assert.AreEqual(10, meta.FixedRQ, "Meta的FixedRQ属性应当正确设置为指定值");
        Assert.AreEqual(XView.EventType.Dynamic, meta.Focus, "Meta的Focus属性应当正确设置为指定值");
        Assert.AreEqual(XView.CacheType.None, meta.Cache, "Meta的Cache属性应当正确设置为指定值");
        Assert.AreEqual(true, meta.Multiple, "Meta的Multiple属性应当正确设置为指定值");
    }

    [Test]
    public void Event()
    {
        var contexts = new Dictionary<XEvent.Manager, XView.Base>();

        // 基础视图
        var myView = new GameObject("MyView").AddComponent<MyView>();
        contexts[myView.Event.context] = myView;

        // 有模块视图
        var myModularView = new GameObject("MyModularView").AddComponent<MyModularView>();
        contexts[myModularView.Module.Event] = myModularView;

        // 继承视图
        var mySubView = new GameObject("MySubView").AddComponent<MySubView>();
        contexts[mySubView.Module.Event] = mySubView;

        #region 特性绑定模块
        {
            myView.OnEvent2Called = false;
            myView.OnEvent2Param = 0;
            MyModule.Instance.Event.Notify(MyEvent.Event2, 1002);

            Assert.IsTrue(myView.OnEvent2Called, "特性绑定模块注册事件后触发通知应当调用回调函数。");
            Assert.AreEqual(myView.OnEvent2Param, 1002, "特性绑定模块注册事件后触发通知调用回调函数的透传参数1应当相等。");

            myView.OnEvent2Called = false;
            MyModule.Instance.Event.Notify(MyEvent.Event2);
            Assert.IsFalse(myView.OnEvent2Called, "特性绑定模块再次触发回调一次的事件应当不调用回调函数。");
        }
        #endregion

        #region 特性默认模块
        {
            myModularView.OnEvent3Called = false;
            myModularView.OnEvent3Param = 0;
            MyModule.Instance.Event.Notify(MyEvent.Event3, 1003);

            Assert.IsTrue(myModularView.OnEvent3Called, "特性默认模块注册事件后触发通知应当调用回调函数。");
            Assert.AreEqual(myModularView.OnEvent3Param, 1003, "特性默认模块注册事件后触发通知调用回调函数的透传参数1应当相等。");

            myModularView.OnEvent3Called = false;
            MyModule.Instance.Event.Notify(MyEvent.Event3, 1003);
            Assert.IsTrue(myModularView.OnEvent3Called, "特性默认模块注册事件后再次触发通知应当调用回调函数。");
        }
        #endregion

        #region 特性继承模块
        {
            mySubView.OnEvent3Called = false;
            mySubView.OnEvent3Param = 0;
            mySubView.OnEvent4Called = false;
            mySubView.OnEvent4Param1 = 0;
            mySubView.OnEvent4Param2 = false;
            MyModule.Instance.Event.Notify(MyEvent.Event3, 1003);
            MyModule.Instance.Event.Notify(MyEvent.Event4, 1004, true);

            Assert.IsTrue(mySubView.OnEvent3Called, "特性继承模块注册事件后触发通知应当调用父类回调函数。");
            Assert.AreEqual(mySubView.OnEvent3Param, 1003, "特性继承模块注册事件后触发通知调用父类回调函数的透传参数1应当相等。");

            Assert.IsTrue(mySubView.OnEvent4Called, "特性继承模块注册事件后触发通知应当调用子类回调函数。");
            Assert.AreEqual(mySubView.OnEvent4Param1, 1004, "特性继承模块注册事件后触发通知调用子类回调函数的透传参数1应当相等。");
            Assert.AreEqual(mySubView.OnEvent4Param2, true, "特性继承模块注册事件后触发通知调用子类回调函数的透传参数2应当相等。");

            mySubView.OnEvent3Called = false;
            mySubView.OnEvent4Called = false;
            MyModule.Instance.Event.Notify(MyEvent.Event3, 1003);
            MyModule.Instance.Event.Notify(MyEvent.Event4, 1004, false);
            Assert.IsTrue(mySubView.OnEvent3Called, "特性继承模块注册事件后再次触发通知应当调用父类回调函数。");
            Assert.IsTrue(mySubView.OnEvent4Called, "特性继承模块注册事件后再次触发通知应当调用子类回调函数。");
        }
        #endregion

        foreach (var kvp in contexts)
        {
            var context = kvp.Key;
            var view = kvp.Value;

            #region 非泛型
            {
                var called = false;
                object[] param1 = null;
                void callback(params object[] args) { called = true; param1 = args; }

                Assert.IsTrue(view.Event.Register(MyEvent.Event1, callback, true), "非泛型注册事件应当成功。");
                context.Notify(MyEvent.Event1, 1001);
                Assert.IsTrue(called, "非泛型注册事件后触发通知应当调用回调函数。");
                Assert.AreEqual(param1[0], 1001, "非泛型注册事件后触发通知调用回调函数的透传参数1应当相等。");

                called = false;
                context.Notify(MyEvent.Event1);
                Assert.IsFalse(called, "非泛型再次触发回调一次的事件应当不调用回调函数。");

                Assert.IsTrue(view.Event.Register(MyEvent.Event1, callback, false), "非泛型注册事件应当成功。");
                Assert.IsTrue(view.Event.Unregister(MyEvent.Event1, callback), "非泛型注销事件应当成功。");
                called = false;
                context.Notify(MyEvent.Event1);
                Assert.IsFalse(called, "非泛型注销事件后触发通知应当不调用回调函数。");

                Assert.IsTrue(view.Event.Register(MyEvent.Event1, callback, false), "非泛型注册事件应当成功。");
                view.Event.Clear();
                called = false;
                context.Notify(MyEvent.Event1);
                Assert.IsFalse(called, "非泛型清除事件后触发通知应当不调用回调函数。");
            }
            #endregion

            #region 泛型 <T1>
            {
                var called = false;
                var param1 = 0;
                void callback(int p1) { called = true; param1 = p1; }

                Assert.IsTrue(view.Event.Register<int>(MyEvent.Event1, callback, true), "泛型 <T1> 注册事件应当成功。");
                context.Notify(MyEvent.Event1, 1001);
                Assert.IsTrue(called, "泛型 <T1> 注册事件后触发通知应当调用回调函数。");
                Assert.AreEqual(param1, 1001, "泛型 <T1> 注册事件后触发通知调用回调函数的透传参数1应当相等。");

                called = false;
                context.Notify(MyEvent.Event1);
                Assert.IsFalse(called, "泛型 <T1> 再次触发回调一次的事件应当不调用回调函数。");

                Assert.IsTrue(view.Event.Register<int>(MyEvent.Event1, callback, false), "泛型 <T1> 注册事件应当成功。");
                Assert.IsTrue(view.Event.Unregister<int>(MyEvent.Event1, callback), "泛型 <T1> 注销事件应当成功。");
                called = false;
                context.Notify(MyEvent.Event1);
                Assert.IsFalse(called, "泛型 <T1> 注销事件后触发通知应当不调用回调函数。");

                Assert.IsTrue(view.Event.Register<int>(MyEvent.Event1, callback, false), "泛型 <T1> 注册事件应当成功。");
                view.Event.Clear();
                called = false;
                context.Notify(MyEvent.Event1);
                Assert.IsFalse(called, "泛型 <T1> 清除事件后触发通知应当不调用回调函数。");
            }
            #endregion

            #region 泛型 <T1, T2>
            {
                var called = false;
                var param1 = 0;
                var param2 = 0;
                void callback(int p1, int p2) { called = true; param1 = p1; param2 = p2; }

                Assert.IsTrue(view.Event.Register<int, int>(MyEvent.Event1, callback, true), "泛型 <T1, T2> 注册事件应当成功。");
                context.Notify(MyEvent.Event1, 1001, 1002);
                Assert.IsTrue(called, "泛型 <T1, T2> 注册事件后触发通知应当调用回调函数。");
                Assert.AreEqual(param1, 1001, "泛型 <T1, T2> 注册事件后触发通知调用回调函数的透传参数1应当相等。");
                Assert.AreEqual(param2, 1002, "泛型 <T1, T2> 注册事件后触发通知调用回调函数的透传参数2应当相等。");

                called = false;
                context.Notify(MyEvent.Event1);
                Assert.IsFalse(called, "泛型 <T1, T2> 再次触发回调一次的事件应当不调用回调函数。");

                Assert.IsTrue(view.Event.Register<int, int>(MyEvent.Event1, callback, false), "泛型 <T1, T2> 注册事件应当成功。");
                Assert.IsTrue(view.Event.Unregister<int, int>(MyEvent.Event1, callback), "泛型 <T1, T2> 注销事件应当成功。");
                called = false;
                context.Notify(MyEvent.Event1);
                Assert.IsFalse(called, "泛型 <T1, T2> 注销事件后触发通知应当不调用回调函数。");

                Assert.IsTrue(view.Event.Register<int, int>(MyEvent.Event1, callback, false), "泛型 <T1, T2> 注册事件应当成功。");
                view.Event.Clear();
                called = false;
                context.Notify(MyEvent.Event1);
                Assert.IsFalse(called, "泛型 <T1, T2> 清除事件后触发通知应当不调用回调函数。");
            }
            #endregion

            #region 泛型 <T1, T2, T3>
            {
                var called = false;
                var param1 = 0;
                var param2 = 0;
                var param3 = 0;
                void callback(int p1, int p2, int p3) { called = true; param1 = p1; param2 = p2; param3 = p3; }

                Assert.IsTrue(view.Event.Register<int, int, int>(MyEvent.Event1, callback, true), "泛型 <T1, T2, T3> 注册事件应当成功。");
                context.Notify(MyEvent.Event1, 1001, 1002, 1003);
                Assert.IsTrue(called, "泛型 <T1, T2, T3> 注册事件后触发通知应当调用回调函数。");
                Assert.AreEqual(param1, 1001, "泛型 <T1, T2, T3> 注册事件后触发通知调用回调函数的透传参数1应当相等。");
                Assert.AreEqual(param2, 1002, "泛型 <T1, T2, T3> 注册事件后触发通知调用回调函数的透传参数2应当相等。");
                Assert.AreEqual(param3, 1003, "泛型 <T1, T2, T3> 注册事件后触发通知调用回调函数的透传参数3应当相等。");

                called = false;
                context.Notify(MyEvent.Event1);
                Assert.IsFalse(called, "泛型 <T1, T2, T3> 再次触发回调一次的事件应当不调用回调函数。");

                Assert.IsTrue(view.Event.Register<int, int, int>(MyEvent.Event1, callback, false), "泛型 <T1, T2, T3> 注册事件应当成功。");
                Assert.IsTrue(view.Event.Unregister<int, int, int>(MyEvent.Event1, callback), "泛型 <T1, T2, T3> 注销事件应当成功。");
                called = false;
                context.Notify(MyEvent.Event1);
                Assert.IsFalse(called, "泛型 <T1, T2, T3> 注销事件后触发通知应当不调用回调函数。");

                Assert.IsTrue(view.Event.Register<int, int, int>(MyEvent.Event1, callback, false), "泛型 <T1, T2, T3> 注册事件应当成功。");
                view.Event.Clear();
                called = false;
                context.Notify(MyEvent.Event1);
                Assert.IsFalse(called, "泛型 <T1, T2, T3> 清除事件后触发通知应当不调用回调函数。");
            }
            #endregion

            #region 删除对象
            {
                var called = false;
                var calledT1 = false;
                var calledT2 = false;
                var calledT3 = false;
                view.Event.Register(MyEvent.Event1, (_) => called = true, false);
                view.Event.Register<int>(MyEvent.Event1, (_) => calledT1 = true, false);
                view.Event.Register<int, int>(MyEvent.Event1, (_, _) => calledT2 = true, false);
                view.Event.Register<int, int, int>(MyEvent.Event1, (_, _, _) => calledT3 = true, false);
                UnityEngine.Object.DestroyImmediate(view);
                context.Notify(MyEvent.Event1);
                Assert.IsFalse(called, "非泛型删除对象后触发通知应当不调用回调函数。");
                Assert.IsFalse(calledT1, "泛型 <T1> 删除对象后触发通知应当不调用回调函数。");
                Assert.IsFalse(calledT2, "泛型 <T1, T2> 删除对象后触发通知应当不调用回调函数。");
                Assert.IsFalse(calledT3, "泛型 <T1, T2, T3> 删除对象后触发通知应当不调用回调函数。");
            }
            #endregion
        }
    }

    [Test]
    public void Element()
    {
        #region 未标记元素特性
        {
            Assert.IsNull(XView.Element.Get(null), "对空类型获取元素特性应当返回空。");
            Assert.AreEqual(XView.Element.Get(typeof(MyView)).Length, 0, "对空类型获取元素特性应当返回空。");
        }
        #endregion

        #region 父类标记元素特性
        {
            var type = typeof(MyModularView);
            var elements = XView.Element.Get(type);

            Assert.NotNull(elements, "对标记的父类型获取元素特性不应当为空。");
            Assert.AreEqual(3, elements.Length, "对标记的父类型获取元素特性数量应当为 2。");

            Assert.AreEqual("MyModularView Class Attr", elements[0].Name, "父类标记在类上的元素特性的名称应当和设置的相等。");
            Assert.AreEqual(type, elements[0].Reflect, "父类标记在类上的元素特性的反射信息应当和所属类相等。");

            Assert.AreEqual("MyModularView Class Attr Extras", elements[1].Name, "父类标记在类上的元素特性的名称应当和设置的相等。");
            Assert.AreEqual(type, elements[1].Reflect, "父类标记在类上的元素特性的反射信息应当和所属类相等。");
            Assert.AreEqual("Hello MyModularView", elements[1].Extras[0], "父类标记在类上的元素特性的参数应当和设置的相等。");

            Assert.AreEqual("MyModularView Method Attr", elements[2].Name, "父类标记在方法上的元素特性的名称应当和设置的相等。");
            Assert.AreEqual(type.GetMember("OnEvent3")[0], elements[2].Reflect, "父类标记在方法上的元素特性的反射信息应当和所属方法相等。");
        }
        #endregion

        #region 子类标记元素特性
        {
            var type = typeof(MySubView);
            var elements = XView.Element.Get(type);

            Assert.NotNull(elements, "对标记的子类型获取元素特性不应当为空。");
            Assert.AreEqual(8, elements.Length, "对标记的子类型获取元素特性数量应当为 5（父类 3 + 子类 5）。");

            Assert.AreEqual("MySubView Class Attr", elements[0].Name, "子类标记在类上的元素特性的名称应当和设置的相等。");
            Assert.AreEqual(type, elements[0].Reflect, "子类标记在类上的元素特性的反射信息应当和所属类相等。");

            Assert.AreEqual("MySubView Class Attr Extras", elements[1].Name, "子类标记在类上的元素特性的名称应当和设置的相等。");
            Assert.AreEqual(type, elements[1].Reflect, "子类标记在类上的元素特性的反射信息应当和所属类相等。");
            Assert.AreEqual("Hello MySubView", elements[1].Extras[0], "子类标记在类上的元素特性的参数应当和设置的相等。");

            Assert.AreEqual("MySubView Method Attr Extras", elements[4].Name, "子类标记在方法上的元素特性的名称应当和设置的相等。");
            Assert.AreEqual(type.GetMember("OnEvent4")[0], elements[4].Reflect, "子类标记在方法上的元素特性的反射信息应当和所属方法相等。");
            Assert.AreEqual("Hello OnEvent4", elements[4].Extras[0], "子类标记在方法上的元素特性的参数应当和设置的相等。");

            Assert.AreEqual("MySubView Field Attr", elements[6].Name, "子类标记在字段上的元素特性的名称应当和设置的相等。");
            Assert.AreEqual(type.GetMember("OnEvent4Called")[0], elements[6].Reflect, "子类标记在字段上的元素特性的反射信息应当和所属字段相等。");

            Assert.AreEqual("MySubView Field Attr Extras", elements[7].Name, "子类标记在字段上的元素特性的名称应当和设置的相等。");
            Assert.AreEqual(type.GetMember("OnEvent4Param1")[0], elements[7].Reflect, "子类标记在字段上的元素特性的反射信息应当和所属字段相等。");
            Assert.AreEqual("Hello OnEvent4Param1", elements[7].Extras[0], "子类标记在字段上的元素特性的参数应当和设置的相等。");
        }
        #endregion
    }

    [Test]
    public void Base()
    {
        // 测试Base类的基本方法 
        var myView = testPanel.AddComponent<MyView>();
        myView.Meta = testMeta;
        myView.Panel = testPanel;

        Assert.IsNotNull(myView.Event, "视图的Event属性不应为空");
        Assert.IsNotNull(myView.Tags, "视图的Tags属性不应为空");

        // 测试生命周期方法
        var openCalled = false;
        var focusCalled = false;
        var blurCalled = false;
        var closeCalled = false;
        myView.OnOpenCallback = () => openCalled = true;
        myView.OnFocusCallback = () => focusCalled = true;
        myView.OnBlurCallback = () => blurCalled = true;
        myView.OnCloseCallback = () => closeCalled = true;

        myView.OnOpen();
        Assert.IsTrue(openCalled, "OnOpen方法应当调用OnOpenCallback");
        myView.OnFocus();
        Assert.IsTrue(focusCalled, "OnFocus方法应当调用OnFocusCallback");
        myView.OnBlur();
        Assert.IsTrue(blurCalled, "OnBlur方法应当调用OnBlurCallback");

        var closeDoneCalled = false;
        myView.OnClose(() => closeDoneCalled = true);
        Assert.IsTrue(closeCalled, "OnClose方法应当调用OnCloseCallback");
        Assert.IsTrue(closeDoneCalled, "OnClose方法应当执行传入的done回调");
    }

    #endregion

    #region 视图管理测试

    [UnityTest]
    public IEnumerator Init()
    {
        // 创建三种不同缓存类型的视图
        var sceneCachedMeta = new XView.Meta("SceneCachedView", 0, XView.EventType.Dynamic, XView.CacheType.Scene);
        var sharedCachedMeta = new XView.Meta("SharedCachedView", 0, XView.EventType.Dynamic, XView.CacheType.Shared);
        var nonCachedMeta = new XView.Meta("NonCachedView", 0, XView.EventType.Dynamic, XView.CacheType.None);

        // 创建视图并添加到缓存列表
        var sceneCachedView = XView.Open(sceneCachedMeta);
        var sharedCachedView = XView.Open(sharedCachedMeta);
        var nonCachedView = XView.Open(nonCachedMeta);

        // 确保视图的GameObject存在
        Assert.IsNotNull(sceneCachedView.Panel, "SceneCached视图的GameObject应当存在");
        Assert.IsNotNull(sharedCachedView.Panel, "SharedCached视图的GameObject应当存在");
        Assert.IsNotNull(nonCachedView.Panel, "NonCached视图的GameObject应当存在");
        // 添加到缓存视图列表
        XView.cachedView.Add(sceneCachedView);
        XView.cachedView.Add(sharedCachedView);
        XView.cachedView.Add(nonCachedView);

        // 创建一个测试场景并卸载，触发sceneUnloaded事件
        var scene = SceneManager.CreateScene("MSVTestScene");
        SceneManager.SetActiveScene(scene);
        yield return SceneManager.UnloadSceneAsync(scene);

        // 验证Scene类型的缓存视图被移除，而Shared类型的视图保留
        Assert.AreEqual(2, XView.cachedView.Count, "缓存视图数量应当为2");
        Assert.IsFalse(XView.cachedView.Contains(sceneCachedView), "SceneCached视图应当被移除");
        Assert.IsTrue(XView.cachedView.Contains(sharedCachedView), "SharedCached视图应当保留");
        Assert.IsTrue(XView.cachedView.Contains(nonCachedView), "NonCached视图只在关闭时销毁");
        Assert.IsTrue(sceneCachedView.Panel == null && !sceneCachedView.Panel, "SceneCached视图的GameObject应当被销毁");
        Assert.IsTrue(sharedCachedView.Panel != null && sharedCachedView.Panel, "SharedCached视图的GameObject应当仍然存在");
        Assert.IsTrue(nonCachedView.Panel != null && nonCachedView.Panel, "NonCached视图只在关闭时销毁");
    }

    [Test]
    public void Load()
    {
        var parentTransform = new GameObject("Parent").transform;
        // 1. 测试加载普通视图
        var normalMeta = new XView.Meta("NormalView", 0, XView.EventType.Dynamic, XView.CacheType.None, false);
        var normalView = XView.Load(normalMeta, parentTransform, false);
        Assert.IsNotNull(normalView, "加载的视图不应为空");
        Assert.AreEqual(normalMeta, normalView.Meta, "加载的视图Meta属性应当与传入的Meta一致");
        Assert.IsNotNull(normalView.Panel, "加载的视图Panel不应为空");
        Assert.IsTrue(normalView.Panel.activeSelf, "加载的视图Panel应当处于激活状态");

        // 2. 测试加载多实例视图
        var multipleMeta = new XView.Meta("MultipleView", 0, XView.EventType.Dynamic, XView.CacheType.None, true);
        var multipleView1 = XView.Load(multipleMeta, parentTransform, false);
        var multipleView2 = XView.Load(multipleMeta, parentTransform, false);
        Assert.IsNotNull(multipleView1, "第一个多实例视图不应为空");
        Assert.IsNotNull(multipleView2, "第二个多实例视图不应为空");
        Assert.AreNotSame(multipleView1, multipleView2, "多实例视图应当创建不同的实例");
        Assert.IsTrue(multipleView1.Panel.activeSelf);

        // 3. 测试加载已存在的非多实例视图 (closeIfOpened = false)
        // 先打开视图使其进入openedView列表
        var openedView = XView.Open(normalMeta);
        Assert.IsNotNull(openedView, "打开的视图不应为空");
        Assert.IsTrue(openedView.Panel.activeSelf, "打开的视图Panel应当处于激活状态");
        // 尝试再次加载
        var existingView = XView.Load(normalMeta, parentTransform, false);
        Assert.IsNotNull(existingView, "加载已存在视图不应为空");
        Assert.AreSame(openedView, existingView, "closeIfOpened为false时应当返回已存在的视图实例");

        // 4. 测试加载已存在的非多实例视图 (closeIfOpened = true)
        XView.Close(normalMeta); // 先关闭之前的视图
        var openedViewAgain = XView.Open(normalMeta);
        Assert.IsNotNull(openedViewAgain, "重新打开的视图不应为空");
        // 尝试再次加载
        var newView = XView.Load(normalMeta, parentTransform, true);
        Assert.IsNotNull(newView, "新加载的视图不应为空");
        Assert.AreNotSame(openedViewAgain, newView, "closeIfOpened为true时应当创建新的视图实例");
        Assert.IsTrue(openedViewAgain.Panel == null, "原视图实例应当被关闭并销毁");

        // 5. 测试从缓存加载视图
        var cachedMeta = new XView.Meta("CachedView", 0, XView.EventType.Dynamic, XView.CacheType.Scene, false);
        var cachedView = XView.Open(cachedMeta);
        Assert.IsNotNull(cachedView, "缓存类型视图应当成功创建");
        // 关闭视图使其进入缓存
        XView.Close(cachedMeta);
        // 再次加载，应该从缓存获取
        var reloadedView = XView.Load(cachedMeta, parentTransform, false);
        Assert.IsNotNull(reloadedView, "从缓存加载的视图不应为空");
        Assert.AreSame(cachedView, reloadedView, "应当从缓存中获取到相同的视图实例");

        if (parentTransform != null) UnityEngine.Object.Destroy(parentTransform.gameObject);
    }

    [Test]
    public void Binding()
    {
        myHandler.bindingView = new object[3];
        var myView = testPanel.AddComponent<MyView>();
        Assert.AreEqual(testPanel, myHandler.bindingView[0], "绑定回调的 go 对象应当和挂载的相等。");
        Assert.AreEqual(myView, myHandler.bindingView[1], "绑定回调的 target 对象应当和挂载的相等。");
        Assert.AreEqual(XView.Element.Get(typeof(MyView)), myHandler.bindingView[2], "绑定回调的 elements 列表应当和 XView.Element 获取的相等。");
    }

    [UnityTest]
    public IEnumerator Open()
    {
        // 测试不同缓存类型
        var meta1 = new XView.Meta("View1", 0, XView.EventType.Dynamic, XView.CacheType.None);
        var meta2 = new XView.Meta("View2", 0, XView.EventType.Dynamic, XView.CacheType.Scene);
        var meta3 = new XView.Meta("View3", 0, XView.EventType.Dynamic, XView.CacheType.Shared);

        // 测试Open方法
        var view1 = XView.Open(meta1);
        var view2 = XView.Open(meta2);
        var view3 = XView.Open(meta3);
        Assert.IsNotNull(view1, "视图1应当成功创建且不为空");
        Assert.IsNotNull(view2, "视图2应当成功创建且不为空");
        Assert.IsNotNull(view3, "视图3应当成功创建且不为空");
        Assert.IsTrue(view1.Panel.activeSelf, "视图1的面板应当处于激活状态");
        Assert.IsTrue(view2.Panel.activeSelf, "视图2的面板应当处于激活状态");
        Assert.IsTrue(view3.Panel.activeSelf, "视图3的面板应当处于激活状态");

        // 测试Close方法
        XView.Close(view1);
        XView.Close(view2);
        XView.Close(view3);
        Assert.IsTrue(view1.Panel == null, "CacheType.None类型的视图关闭后Panel应当被销毁");
        Assert.IsFalse(view2.Panel.activeSelf, "CacheType.Scene类型的视图关闭后Panel应当设为非活动状态");
        Assert.IsFalse(view3.Panel.activeSelf, "CacheType.Shared类型的视图关闭后Panel应当设为非活动状态");

        // 测试异步操作
        var asyncMeta = new XView.Meta("AsyncView");
        var callbackCalled = false;

        yield return XView.OpenAsync(asyncMeta, (view) =>
        {
            callbackCalled = true;
            Assert.IsNotNull(view, "异步加载的视图不应为空");
            Assert.AreEqual(asyncMeta.Path, view.Meta.Path, "异步加载的视图Meta路径应当与传入的Meta一致");
        });

        Assert.IsTrue(callbackCalled, "异步加载完成后应当调用回调函数");
    }

    [Test]
    public void Close()
    {
        // 测试CloseAll方法
        var meta1 = new XView.Meta("View1");
        var meta2 = new XView.Meta("View2");
        var meta3 = new XView.Meta("View3");

        var view1 = XView.Open(meta1);
        var view2 = XView.Open(meta2);
        var view3 = XView.Open(meta3);

        Assert.IsNotNull(view1);
        Assert.IsNotNull(view2);
        Assert.IsNotNull(view3);

        XView.Close(meta1);
        Assert.IsFalse(view1.Panel.activeSelf, "视图1应当被关闭且处于非激活状态");

        XView.Open(meta1);
        XView.Close(view1);
        Assert.IsFalse(view1.Panel.activeSelf, "视图1应当被关闭且处于非激活状态");

        XView.Open(meta1);
        XView.CloseAll(meta1); // 关闭除meta1外的所有界面
        Assert.IsTrue(view1.Panel.activeSelf, "排除的视图1应当保持激活状态");
        Assert.IsFalse(view2.Panel.activeSelf, "视图2应当被关闭且处于非激活状态");
        Assert.IsFalse(view3.Panel.activeSelf, "视图3应当被关闭且处于非激活状态");

        XView.DestroyAll(meta1);
        Assert.IsNotNull(view1.Panel, "排除的视图1应当不为空");
        Assert.IsTrue(view2.Panel == null, "销毁的视图2应当为空");
        Assert.IsTrue(view2.Panel == null, "销毁的视图3应当为空");

        XView.DestroyAll();
        Assert.IsTrue(view1.Panel == null, "销毁的视图1应当为空");
    }

    [Test]
    public void Sort()
    {
        // 创建测试视图
        var meta1 = new XView.Meta("View1", 0, XView.EventType.Dynamic);
        var meta2 = new XView.Meta("View2", 0, XView.EventType.Dynamic);
        var meta3 = new XView.Meta("View3", 0, XView.EventType.Dynamic);
        var meta4 = new XView.Meta("View4", 0, XView.EventType.Dynamic);
        var view1 = XView.Open(meta1);
        var view2 = XView.Open(meta2);
        var view3 = XView.Open(meta3);
        XView.openedView.Clear();

        // 测试view被添加到below视图之前
        XView.Sort(view2, view1, null);
        XView.Sort(view1, null, null);
        Assert.AreEqual(1, XView.openedView.IndexOf(view1), "view1应当位于索引1的位置");
        Assert.AreEqual(0, XView.openedView.IndexOf(view2), "view2应当位于索引0的位置");

        // 测试view被添加到above视图之后
        XView.openedView.Clear();
        XView.Sort(view1, null, null);
        XView.Sort(view2, null, view1);
        Assert.AreEqual(0, XView.openedView.IndexOf(view1), "view1应当位于索引0的位置");
        Assert.AreEqual(1, XView.openedView.IndexOf(view2), "view2应当位于索引1的位置");

        // 测试below和above都为null时，view被添加到末尾
        XView.openedView.Clear();
        XView.Sort(view1, null, null);
        XView.Sort(view2, null, null);
        Assert.AreEqual(0, XView.openedView.IndexOf(view1), "view1应当位于索引0的位置");
        Assert.AreEqual(1, XView.openedView.IndexOf(view2), "view2应当位于索引1的位置");

        // 渲染顺序测试
        // 测试FixedRQ的渲染顺序
        XView.openedView.Clear();
        myHandler.lastSetOrderView = null;
        myHandler.lastSetOrderValue = 0;
        XView.Sort(view1, null, null);
        Assert.AreSame(view1, myHandler.lastSetOrderView, "SetOrder方法应当使用正确的视图参数");
        Assert.AreEqual(500, myHandler.lastSetOrderValue, "普通视图的渲染顺序应当按公式计算");

        // 焦点状态测试
        // 测试EventType.Slience类型视图
        var silenceMeta = new XView.Meta("SilenceView", 0, XView.EventType.Slience);
        var silenceView = XView.Open(silenceMeta);
        var blurCalled = false;
        (silenceView as MyView).OnBlurCallback = () => blurCalled = true;

        // 确保视图先获得焦点
        XView.focusedView[silenceView] = true;
        myHandler.lastFocusedView = null;
        // Sort应该使Slience类型视图失去焦点
        XView.Sort(silenceView, null, null);
        Assert.IsNull(myHandler.lastFocusedView, "Slience类型视图不应调用SetFocus方法");
        Assert.IsTrue(blurCalled, "Slience类型视图应当调用OnBlur方法");
        Assert.IsFalse(XView.focusedView[silenceView], "Slience类型视图在focusedView中的标记应为false");

        // 测试EventType.Static和Dynamic类型视图
        XView.openedView.Clear();
        XView.focusedView.Clear();
        var staticMeta = new XView.Meta("StaticView", 0, XView.EventType.Static);
        var dynamicMeta = new XView.Meta("DynamicView", 0, XView.EventType.Dynamic);
        var staticView = XView.Open(staticMeta);
        var dynamicView = XView.Open(dynamicMeta);
        var staticFocusCalled = false;
        var dynamicFocusCalled = false;
        (staticView as MyView).OnFocusCallback = () => staticFocusCalled = true;
        (dynamicView as MyView).OnFocusCallback = () => dynamicFocusCalled = true;

        // 先清空焦点状态
        XView.openedView.Clear();
        XView.focusedView.Clear();

        // Sort后，Static类型视图应该获得焦点
        XView.Sort(staticView, null, null);
        Assert.IsTrue(staticFocusCalled, "Static类型视图应当调用OnFocus方法");
        Assert.IsTrue(XView.focusedView[staticView], "Static类型视图在focusedView中的标记应为true");

        // 添加Dynamic类型视图后，由于lastFocused已为true，Dynamic视图不应获得焦点
        XView.Sort(dynamicView, staticView, null);
        Assert.IsFalse(dynamicFocusCalled, "当lastFocused为true时，Dynamic类型视图不应调用OnFocus方法");
        Assert.IsFalse(XView.focusedView.ContainsKey(dynamicView) && XView.focusedView[dynamicView], "Dynamic类型视图在focusedView中的标记应为false");

        // 测试Panel为null的情况
        var testView = XView.Open(new XView.Meta("TestView"));
        UnityEngine.Object.DestroyImmediate(testView.Panel);
        int initialCount = XView.openedView.Count;
        LogAssert.Expect(LogType.Error, new Regex("XView.Sort: view .* has already been destroyed."));
        XView.Sort(null, null, null); // 调用Sort应该清理无效视图
        Assert.AreEqual(initialCount - 1, XView.openedView.Count, "Panel为null的视图应当从openedView列表中移除");
    }

    [Test]
    public void Focus()
    {
        var view = XView.Open(testMeta);
        myHandler.lastFocusedView = null;
        XView.Focus(view);
        Assert.AreSame(view, myHandler.lastFocusedView, "Focus方法应当正确设置视图的焦点状态");
    }

    [Test]
    public void Find()
    {
        var parentTransform = new GameObject("Parent").transform;
        // 通过Load加载，不会自动加入openedView列表
        XView.Load(testMeta, parentTransform, false);
        var foundView = XView.Find(testMeta);
        Assert.IsNull(foundView);

        // 通过Open加载，会自动加入openedView列表
        var openedView = XView.Open(testMeta);
        foundView = XView.Find(testMeta);
        Assert.IsNotNull(foundView);
        Assert.AreSame(openedView, foundView);

        if (parentTransform != null) UnityEngine.Object.Destroy(parentTransform.gameObject);
    }

    #endregion
}
