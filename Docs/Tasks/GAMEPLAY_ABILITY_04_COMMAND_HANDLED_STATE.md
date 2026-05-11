# GAMEPLAY_ABILITY_04_COMMAND_HANDLED_STATE

## 目标

修正 command system 扩展边界：`GameplayUnsupportedCommandSystem` 不再用硬编码 command id 白名单判断 command 是否支持，而是依赖帧内 handled 状态。

这样项目或后续框架模块可以在 default pipeline 之上新增 command system，而不会被 unsupported system 误报 `CommandRejected / UnsupportedGameplayCommand`。

## 新增 API

```csharp
public sealed class GameplayCommandExecutionState
{
    public int HandledCount { get; }
    public bool MarkHandled(RuntimeCommand command);
    public bool IsHandled(RuntimeCommand command);
    public void Clear();
}
```

`GameplaySystemContext` 新增：

```csharp
public GameplayCommandExecutionState CommandState { get; }
```

## 规则

- `CommandState` 是 frame-local / pipeline-local 状态。
- 处理 command 的 system 必须调用 `context.CommandState.MarkHandled(command)`。
- 明确拒绝 command 的 system 也应标记 handled，避免后续 unsupported system 再次拒绝。
- `GameplayUnsupportedCommandSystem` 只拒绝未 handled command。
- `GameplayUnsupportedCommandSystem` 不维护 supported command id 白名单。

## Default Pipeline 扩展

`GameplayRuntimeModule.CreateDefaultSystemPipeline(...)` 公开为默认 pipeline factory。调用方需要在默认 Gameplay command systems 之外扩展时，可以：

```csharp
var pipeline = GameplayRuntimeModule.CreateDefaultSystemPipeline(abilityRegistry);
pipeline.Add(new InteractionCommandSystem());
var module = new GameplayRuntimeModule(world, abilityRegistry, commandBuffer, systemPipeline: pipeline);
```

如果调用方完全传入自定义 pipeline，则仍由调用方负责注册所有需要的 command systems。

## 验收

- CastAbility / DespawnEntity 继续由对应 command system 处理。
- unsupported command 仍输出 `CommandRejected / UnsupportedGameplayCommand`。
- 自定义 command system 标记 handled 后，default unsupported system 不再误报。
- `GameplayRuntimeModule` 仍只负责 drain command、构造 context、运行 pipeline、event queue 和 world tick。
