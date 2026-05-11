# Gameplay Ability 03：Command System

> 状态：Implemented v0（2026-05-12）

## 目标

把 `CastAbility` / `DespawnEntity` 从 `GameplayRuntimeModule` 的内置 switch 迁入 Gameplay systems，让 module 回到调度职责：

```text
Drain RuntimeCommandBuffer
Build GameplaySystemContext
Run GameplaySystemPipeline
Optional GameplayWorld.Tick
Expose RuntimeEventQueue
```

本批次继续使用 v0 `RuntimeEntity` / `GameplayWorld` API，不迁移 state source of truth。

## Public API

```csharp
public sealed class GameplayAbilityCommandSystem : IGameplaySystem
{
    public const string DefaultSystemId = "mxframework.gameplay.command.ability";
}

public sealed class GameplayEntityLifecycleCommandSystem : IGameplaySystem
{
    public const string DefaultSystemId = "mxframework.gameplay.command.entity_lifecycle";
}

public sealed class GameplayUnsupportedCommandSystem : IGameplaySystem
{
    public const string DefaultSystemId = "mxframework.gameplay.command.unsupported";
    public const string UnsupportedReason = "UnsupportedGameplayCommand";
}
```

`GameplayRuntimeModule` 在未传入 custom pipeline 时会创建 default pipeline：

```text
GameplayAbilityCommandSystem
GameplayEntityLifecycleCommandSystem
GameplayUnsupportedCommandSystem
```

如果调用方传入 custom pipeline，则由调用方负责注册需要的 command systems。Module 不再额外执行 built-in command switch。

## Command Flow

Default module 每帧执行顺序：

```text
Drain RuntimeCommandBuffer
-> Pipeline PreCommand
-> Pipeline Command
   - GameplayAbilityCommandSystem handles CastAbility
   - GameplayEntityLifecycleCommandSystem handles DespawnEntity
   - GameplayUnsupportedCommandSystem rejects unsupported command ids
-> Pipeline Simulation
-> Pipeline Resolution
-> Pipeline Diagnostics
-> Optional GameplayWorld.Tick
-> Enqueue WorldTicked
```

`GameplaySystemContext.Commands` 仍是帧内临时只读 view，command systems 不能持有列表引用。

## Ability Results

`GameplayAbilityCommandSystem` 通过 result sink 把 `GameplayAbilityRuntimeResult` 写回 module 的 recent result ring buffer。`GameplayRuntimeModule.AbilityResults` 仍只表示最近 N 条诊断结果，不是完整历史。

## 测试

本批次更新 / 新增测试覆盖：

- default module 仍能 drain `CastAbility` 并输出 success event。
- cast failure 仍输出结构化 failure event。
- default module 仍能处理 `DespawnEntity`。
- unsupported gameplay command 输出 `CommandRejected`。
- custom pipeline 中 `PreCommand` 在 `Command` system 前运行。
- `AbilityResults` recent ring buffer 行为保持不变。
