# Gameplay ECS-style 00：Design Contract

> 状态：Planned / Contract（2026-05-11）

## 定位

本路线不把完整 ECS 引擎作为当前交付目标。底层存储和查询形态可以随着真实 Gameplay 需求演进到 SparseSet / Archetype / Chunk；当前优先把 `MxFramework.Gameplay` 收束为：

```text
Command-driven Gameplay ECS-style Runtime
= Component Store + System Pipeline + Command-driven Flow + EventQueue Output
```

重点是组件化状态、系统化逻辑、明确 command 输入、稳定 runtime 调度、按帧 event 输出，以及 Replay / SaveState / Hash / Diagnostics 可接入。

## 核心定义

| 概念 | 定义 |
| --- | --- |
| Entity | 只表达身份和生命周期，不承载业务逻辑。后续使用 generation id 防止 stale reference 命中新实体。 |
| Component | 纯 gameplay 状态容器，不引用 `UnityEngine`、`UnityEditor`、Combat、UI、Demo 或 WGame 私有数据。 |
| System | 处理明确 command 或组件状态的逻辑单元。System 不直接 drain `RuntimeCommandBuffer`。 |
| World | 组合根，持有 entity lifecycle、component stores、system pipeline、event queue、diagnostics/hash/save 入口。 |
| Command | 权威输入。Input / AI / Timer / SceneFlow / Combat bridge 只能 enqueue command，不直接改 Gameplay 状态。 |
| Event | 结果输出。UI / Audio / Diagnostics / Replay 通过 `RuntimeEventQueue<GameplayRuntimeEvent>` 或 snapshot 观察。 |

## 硬规则

1. 不把完整 ECS 引擎作为当前交付目标；底层存储和查询形态由真实 Gameplay 需求驱动演进。
2. 同一类状态只能有一个 source of truth。
3. `RuntimeCommandBuffer` 是单 drain owner 资源。`GameplayRuntimeModule` 持有的 buffer 只能由该 module drain。
4. `GameplayRuntimeModule` 长期只保留调度职责：drain command、构造 system context、运行 pipeline、暴露 event queue。
5. 业务逻辑逐步迁移到 system：Ability command、entity lifecycle、effect、buff tick、cooldown、death、cleanup。
6. Gameplay runtime 代码保持 `noEngineReferences=true`，不得引用 Unity、Editor、Demo、UI、Combat 实现层或项目私有数据。
7. 新增 component / system / command handler 必须补测试和接口/任务文档。

## Source of Truth

迁移过程中最容易出问题的是双状态。例如：

```text
RuntimeEntity.TeamId = 1
TeamComponent.TeamId = 2
```

或：

```text
AttributeStore HP = 500
HealthComponent HP = 600
```

禁止双写同一状态。迁移采用三段式：

```text
Stage A: Component store 作为 RuntimeEntity / AttributeStore / BuffPipeline 的 view 或 adapter，不复制权威值。
Stage B: 某类状态迁移到 component store 后，旧 RuntimeEntity API 只做 facade。
Stage C: 迁移完成后移除旧字段或标记 obsolete，并由测试锁定 source of truth。
```

第一批组件不做过碎的 `HealthComponent`、`AttackComponent`、`DefenseComponent`。这些继续通过 `AttributeComponent` 包装 `AttributeStore` 表达。

## 初始组件建议

```text
IdentityComponent
TeamComponent
AttributeComponent
BuffComponent
ModifierComponent
AbilityComponent
TagComponent
StatusComponent
LifecycleComponent
```

## System Phase

第一版 system phase 贴近 Runtime 主线，不发明复杂调度：

```text
PreCommand
Command
Simulation
Resolution
Diagnostics
```

推荐流程：

```text
Input / AI / Timer enqueue command
-> GameplayRuntimeModule drain command
-> Command Systems
   - AbilityCommandSystem
   - EntityLifecycleCommandSystem
   - InteractionCommandSystem
-> Simulation Systems
   - EffectSystem
   - BuffTickSystem
   - CooldownSystem
   - StatusSystem
-> Resolution Systems
   - DeathSystem
   - CleanupSystem
-> Systems enqueue GameplayRuntimeEvent
-> UI / Audio / Diagnostics drain events by frame
-> Hash / Snapshot / SaveState read stable world state
```

EventQueue 不由 Gameplay 内部强制 flush。外部观察者按 frame drain，避免错过事件。

## 后续 Batch

```text
GAMEPLAY_ECS_STYLE_01_COMPONENT_STORE
  - GameplayEntityId / generation
  - IGameplayComponent
  - GameplayComponentStore<T>
  - GameplayComponentRegistry
  - stable query / snapshot
  - no UnityEngine
  - tests

GAMEPLAY_ECS_STYLE_02_SYSTEM_PIPELINE
  - IGameplaySystem
  - GameplaySystemPhase
  - GameplaySystemContext
  - GameplaySystemPipeline
  - stable order / enable-disable / exception policy
  - tests

GAMEPLAY_ECS_STYLE_03_RUNTIME_ENTITY_ADAPTER
  - RuntimeEntity 继续可用
  - AttributeStore / BuffPipeline / ModifierPipeline 包成 component
  - 避免双状态
  - adapter / facade tests

GAMEPLAY_ABILITY_03_COMMAND_SYSTEM
  - AbilityCommandSystem
  - EntityLifecycleSystem
  - GameplayRuntimeModule 只做 drain + pipeline runner
  - RuntimeEventQueue 输出结果
  - tests
```

## Agent 约束

Agent 新增玩法时优先选择以下工作单元：

- 新增 component。
- 新增 system。
- 新增 command factory / handler。
- 新增 effect。
- 新增 config mapper。
- 新增 test / hash / save 验证。

Agent 不应在 Demo、MonoBehaviour、RuntimeEntity 或 `GameplayRuntimeModule` 中直接塞特例业务逻辑。

## 验收标准

- 设计文档和 `Docs/Interfaces/Gameplay.md` 对 ECS-style 定义一致。
- 后续代码任务必须引用本契约并说明是否影响 source of truth。
- 后续新增 system 不直接调用 `RuntimeCommandBuffer.DrainForFrame`。
- 后续新增 component store snapshot 按 entity id / generation 稳定排序。
