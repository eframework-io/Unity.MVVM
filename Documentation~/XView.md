# XView

[![NPM](https://img.shields.io/npm/v/io.eframework.unity.mvvm?label=NPM&logo=npm)](https://www.npmjs.com/package/io.eframework.unity.mvvm)
[![UPM](https://img.shields.io/npm/v/io.eframework.unity.mvvm?label=UPM&logo=unity&registry_uri=https://package.openupm.com)](https://openupm.com/packages/io.eframework.unity.mvvm)
[![DeepWiki](https://img.shields.io/badge/DeepWiki-Explore-blue)](https://deepwiki.com/eframework-io/Unity.MVVM)
[![Discord](https://img.shields.io/discord/1422114598835851286?label=Discord&logo=discord)](https://discord.gg/XMPx2wXSz3)

XView 提供了业务开发的基础视图，通过适配器模式实现了视图的管理功能。

## 功能特性

- 业务适配器：通过适配器实例控制视图，实现了加载、绑定、显示、排序等功能
- 高可用拓展：提供了视图元素特性标记，支持 Unity UI、FairyGUI 等 UI 插件

## 使用手册

### 1. 定义视图

#### 1.1 基础视图

```csharp
// 基础视图
public class MyView : XView.Base { }

// 模块视图
public class MyModularView : XView.Base<MyModularView> { }
```

#### 1.2 视图描述

```csharp
// 创建描述
var meta = new XView.Meta(
    path: "Prefabs/MyView",           // 预制体路径
    fixedRQ: 0,                       // 固定渲染顺序
    focus: XView.EventType.Dynamic,   // 焦点类型
    cache: XView.CacheType.Scene,     // 缓存类型
    multiple: false                   // 是否支持多实例
);
```

#### 1.3 事件系统

基础示例：

```csharp
// 注册事件
Event.Register(id, callback);
Event.Register<T1>(id, callback);
Event.Register<T1, T2>(id, callback);
Event.Register<T1, T2, T3>(id, callback);

// 注销事件
Event.Unregister(id, callback);
Event.Unregister<T1>(id, callback);
Event.Unregister<T1, T2>(id, callback);
Event.Unregister<T1, T2, T3>(id, callback);

// 通知事件
Event.Notify(id, manager, args);
```

标记示例：

- 事件标记会在视图 `OnEnable` 时自动注册，在视图 `OnDisable` 时自动注销
- 若未指定模块类型，则使用当前视图关联的模块

```csharp
// 定义模块
public class MyModule : XModule.Base<MyModule> { }

// 定义事件
public enum MyEvent
{
    Event1,
    Event2,
    Event3,
}

// 事件标记
public class MyView : XView.Base<MyModule> {
    // 模块事件注册（指定模块类型）
    [XModule.Event(MyEvent.Event1, typeof(MyModule))]
    private void OnEvent1() { }

    // 模块事件注册（单次触发）
    [XModule.Event(MyEvent.Event2, typeof(MyModule), true)]
    private void OnEvent2(params object[] args) { }

    // 模块事件注册（当前视图关联的模块）
    [XModule.Event(MyEvent.Event3)]
    private void OnEvent3(int paramA, int paramB) { }
}
```

#### 1.4 视图特性

- 可用于标记视图元素的元数据，实现视图绑定和事件绑定
- 子类会继承父类的所有特性标记，可应用于类、字段和方法
- 视图特性会在视图 `Awake` 时回调 `Handler.SetBinding` 方法
- 特性标记只维护视图的元数据，业务层需要自行处理视图的绑定行为

使用示例：

```csharp
// 类特性标记
[XView.Element("类特性名称")]
[XView.Element("类特性名称带参数", "参数值")]
public class MyView : XView.Base
{
    // 字段特性标记
    [XView.Element("字段特性名称")]
    private Button button;

    // 字段特性标记带参数
    [XView.Element("字段特性名称带参数", "参数值")]
    private Text text;

    // 方法特性标记
    [XView.Element("方法特性名称")]
    private void OnButtonClick() { }

    // 方法特性标记带参数
    [XView.Element("方法特性名称带参数", "参数值")]
    private void OnTextChanged() { }
}
```

### 2. 视图管理

#### 2.1 初始化

使用示例：

```csharp
// 创建自定义Handler
public class MyHandler : XView.IHandler 
{
    public void Load(XView.IMeta meta, Transform parent, out XView.IBase view, out GameObject panel)
    {
        // 实现视图加载逻辑
    }

    public void LoadAsync(XView.IMeta meta, Transform parent, Action<XView.IBase, GameObject> callback)
    {
        // 实现异步加载逻辑
    }

    public bool Loading(XView.IMeta meta) { 
        // 是否正在加载视图
    }

    public void SetBinding(GameObject go, object target, XView.Element[] elements)
    {
        // 实现视图绑定逻辑
    }

    public void SetOrder(XView.IBase view, int order)
    {
        // 实现视图排序逻辑
    }

    public void SetFocus(XView.IBase view, bool focus)
    {
        // 实现焦点设置逻辑
    }
}

// 初始化视图系统
var handler = new MyHandler();
XView.Initialize(handler);
```

#### 2.2 打开视图

使用示例：

```csharp
// 同步打开视图
var view = XView.Open(meta, args);

// 异步打开视图
XView.OpenAsync(meta, callback, args);
```

#### 2.3 关闭视图

使用示例：

```csharp
// 关闭指定视图
XView.Close(meta, resume, destroy);

// 关闭所有视图
XView.CloseAll(exclude);

// 关闭并销毁所有视图
XView.DestroyAll(exclude);
```

#### 2.4 视图排序

使用示例：

```csharp
// 设置视图顺序
XView.Sort(view, below, above);

// 恢复默认顺序
XView.Resume();
```

## 常见问题

### 1. 视图的 EventType（焦点类型）有什么区别？

1. Dynamic（动态）：当新视图打开时，之前的视图会自动失去焦点，适合需要独占用户输入的视图，如对话框。
2. Static（静态）：打开新视图不会影响此视图的焦点状态，适合需要保持交互的持久性视图，如操作面板。
3. Silent（静默）：视图不参与焦点系统，适合纯显示性质的视图，如提示信息或背景元素。

### 2. 视图的 CacheType（缓存类型）有什么区别？

1. Scene（场景级）：视图实例在场景切换时销毁，适合与当前场景关联的UI。
2. Shared（共享级）：视图实例在应用程序生命周期内保持，适合全局共享的UI，如主菜单。
3. None（无缓存）：视图在关闭时立即销毁，适合临时性强、内存占用大的UI。

更多问题，请查阅[问题反馈](../CONTRIBUTING.md#问题反馈)。

## 项目信息

- [更新记录](../CHANGELOG.md)
- [贡献指南](../CONTRIBUTING.md)
- [许可协议](../LICENSE.md)
