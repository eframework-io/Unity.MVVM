# XScene

[![NPM](https://img.shields.io/npm/v/io.eframework.unity.mvvm?label=NPM&logo=npm)](https://www.npmjs.com/package/io.eframework.unity.mvvm)
[![UPM](https://img.shields.io/npm/v/io.eframework.unity.mvvm?label=UPM&logo=unity&registry_uri=https://package.openupm.com)](https://openupm.com/packages/io.eframework.unity.mvvm)
[![DeepWiki](https://img.shields.io/badge/DeepWiki-Explore-blue)](https://deepwiki.com/eframework-io/Unity.MVVM)
[![Discord](https://img.shields.io/discord/1422114598835851286?label=Discord&logo=discord)](https://discord.gg/XMPx2wXSz3)

提供了业务开发的基础场景，支持业务场景的切换及其生命周期管理。

## 功能特性

- 业务场景切换：通过状态机模式实现业务场景的切换功能
- 生命周期管理：提供了 Start、Update、Stop 等状态控制

## 使用手册

### 1. 定义场景

```csharp
// 基础场景
public class MyScene : XScene.Base
{
    public override string Name => "MyScene";
}

// 单例场景
public class MySingletonScene : XScene.Base<MySingletonScene>
{
    public override string Name => "MySingletonScene";
}
```

### 2. 场景管理

#### 2.1 场景切换

```csharp
// 切换业务场景
XScene.Goto(scene, args);

// 设置代理实例
XScene.OnProxy = proxy => new MyScene(proxy);
```

#### 2.2 场景状态

```csharp
// 获取当前场景
var current = XScene.Current;

// 获取上一场景
var last = XScene.Last;

// 获取下一场景
var next = XScene.Next;

// 监听场景切换
XScene.OnSwap += () => {
    // 场景切换完成后的处理逻辑
};
```

## 常见问题

### 1. XScene 与 Unity 场景系统有什么区别？

1. XScene 是业务场景管理框架，而 Unity 的 SceneManager 是渲染场景管理系统。
2. XScene 可以与 Unity 场景系统集成，实现业务场景与渲染场景的对应关系。

更多问题，请查阅[问题反馈](../CONTRIBUTING.md#问题反馈)。

## 项目信息

- [更新记录](../CHANGELOG.md)
- [贡献指南](../CONTRIBUTING.md)
- [许可协议](../LICENSE.md)
