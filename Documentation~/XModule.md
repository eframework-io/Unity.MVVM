# XModule

[![NPM](https://img.shields.io/npm/v/io.eframework.unity.mvvm?label=NPM&logo=npm)](https://www.npmjs.com/package/io.eframework.unity.mvvm)
[![UPM](https://img.shields.io/npm/v/io.eframework.unity.mvvm?label=UPM&logo=unity&registry_uri=https://package.openupm.com)](https://openupm.com/packages/io.eframework.unity.mvvm)
[![DeepWiki](https://img.shields.io/badge/DeepWiki-Explore-blue)](https://deepwiki.com/eframework-io/Unity.MVVM)
[![Discord](https://img.shields.io/discord/1422114598835851286?label=Discord&logo=discord)](https://discord.gg/XMPx2wXSz3)

XModule 提供了业务开发的基础模块，支持模块的生命周期管理和事件系统集成。

## 功能特性

- 生命周期管理：提供 Awake、Start、Reset、Stop 状态控制
- 事件系统集成：通过事件系统对模块与视图间的交互进行解耦合

## 使用手册

### 1. 定义模块

```csharp
// 基础模块
public class MyModule : XModule.Base
{
    public override string Name => "MyModule";
}

// 单例模块
public class MySingletonModule : XModule.Base<MySingletonModule> 
{
    public override string Name => "MySingletonModule";
}
```

### 2. 模块管理

#### 2.1 模块状态

```csharp
// 获取模块实例（自动调用 Awake 方法）
var module = MySingletonModule.Instance;

// 获取模块名称
var name = module.Name;

// 获取模块标签
var tags = module.Tags;

// 开始运行模块
module.Start();

// 停止运行模块
module.Stop();

// 重置模块状态
module.Reset();
```

#### 2.2 事件系统

```csharp
// 注册事件
module.Event.Register(id, callback);

// 注销事件
module.Event.Unregister(id, callback);

// 通知事件
module.Event.Notify(id, args);
```

#### 2.3. 事件特性

- 支持事件标识（`ID`）、模块类型（字段名必须为 `Instance`）及单次回调（`Once`）等标记选项
- 支持实例方法和静态方法等种方法签名（无参、有参、返值），支持继承类中的事件特性
- 特性标记只维护事件的元数据，业务层需自行实现事件注册/注销的行为，可参考 `XView` 模块

使用示例：

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

// 标记事件
public class MyListener {
    // 在类中使用事件特性标记方法
    [XModule.Event(MyEvent.Event1, typeof(MyModule), false)]
    public void OnEvent1() { }

    // 支持单次回调的事件（通知一次后自动注销）
    [XModule.Event(MyEvent.Event2, typeof(MyModule), true)]
    public void OnEvent2(params object[] args) { }

    // 支持无模块绑定的事件
    [XModule.Event(MyEvent.Event3, null, false)]
    public void OnEvent3(int param1, bool param2) { }

    // 支持静态方法
    [XModule.Event(MyEvent.Event3, typeof(MyModule), true)]
    public static void OnEvent3Static() { }
}

// 获取类型中标记的所有事件特性
var events = XModule.Event.Get(typeof(MyListener));

// TODO: 根据标记的事件特性注册事件
```

## 常见问题

更多问题，请查阅[问题反馈](../CONTRIBUTING.md#问题反馈)。

## 项目信息

- [更新记录](../CHANGELOG.md)
- [贡献指南](../CONTRIBUTING.md)
- [许可协议](../LICENSE.md)
