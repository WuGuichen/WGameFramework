# Gameplay ECS-style 02：System Pipeline

> 状态：Implemented v0（2026-05-12）

## 目标

在 component store 之后补 Gameplay 运行时系统管线：

```text
IGameplaySystem
GameplaySystemPhase
GameplaySystemContext
GameplaySystemPipeline
RuntimeCommandBuffer still drained only by GameplayRuntimeModule
```

本批次不迁移 Ability / Despawn 业务逻辑。`GameplayRuntimeModule` 仍保留 v0 command handling；pipeline 作为后续 command systems、simulation systems 和 resolution systems 的调度底座先接入。

## Public API

```csharp
public enum GameplaySystemPhase
{
    PreCommand,
    Command,
    Simulation,
    Resolution,
    Diagnostics
}

public interface IGameplaySystem
{
    string SystemId { get; }
    GameplaySystemPhase Phase { get; }
    int Priority { get; }
    bool IsEnabled { get; }
    void Tick(GameplaySystemContext context);
}

public readonly struct GameplaySystemContext
{
    public RuntimeFrame Frame { get; }
    public double DeltaTime { get; }
    public double ElapsedTime { get; }
    public GameplayWorld World { get; }
    public IReadOnlyList<RuntimeCommand> Commands { get; }
    public RuntimeEventQueue<GameplayRuntimeEvent> Events { get; }
}

public sealed class GameplaySystemPipeline
{
    public int Count { get; }
    public void Add(IGameplaySystem system);
    public bool Remove(string systemId);
    public bool Contains(string systemId);
    public void Tick(GameplaySystemContext context);
    public GameplaySystemPipelineSnapshot CreateSnapshot();
    public void Clear();
}
```

## 执行顺序

Pipeline 按以下顺序稳定执行：

```text
GameplaySystemPhase asc
Priority asc
Registration sequence asc
```

`IsEnabled == false` 的 system 会被跳过，但仍保留在 pipeline 和 snapshot 中。

## CommandBuffer Ownership

`RuntimeCommandBuffer` 仍只能由 `GameplayRuntimeModule` drain。System 不拿 command buffer，也不调用 `DrainForFrame`。

`GameplayRuntimeModule` 每帧：

```text
Drain RuntimeCommandBuffer
Execute current v0 built-in command handlers
Run GameplaySystemPipeline with drained commands
Optional GameplayWorld.Tick
```

后续 `GAMEPLAY_ABILITY_03_COMMAND_SYSTEM` 会把 Ability / Despawn command handling 从 module 迁到 system；本批次只提供调度能力，不扩大业务逻辑。

## 异常策略

System 抛异常时，pipeline 用 `GameplaySystemPipelineException` 包装 system id 和 phase 后重新抛出。第一版不吞异常，不做自动降级，避免隐藏权威 runtime 错误。

## 测试

新增 `GameplaySystemPipelineTests` 覆盖：

- phase / priority / registration order 稳定执行。
- disabled system 跳过，snapshot 统计 enabled count。
- add null / empty id / duplicate id 校验。
- system exception 包装 system id 和 phase。
- `GameplayRuntimeModule` 可选接入 pipeline，并把模块已 drain 的 commands 传给 systems。
