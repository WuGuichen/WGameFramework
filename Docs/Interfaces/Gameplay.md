# Gameplay 接口

> Phase 11 Runtime Gameplay Foundation 已 Accepted / Closed（2026-05-09）。本页记录当前已验收的 Gameplay Runtime v0 公共接口和边界。

## 职责

Gameplay 提供最小游戏行为运行时核心：实体、技能、目标选择、效果执行和技能事件。它把 Demo 中验证过的 Entity -> Ability -> Target -> Effect -> Attributes -> Buff -> Events 闭环提升为框架 API。

## 为什么不依赖 Unity

`MxFramework.Gameplay` 是 `noEngineReferences=true` 的纯 C# 程序集，不引用 `UnityEngine` 或 `UnityEditor`。时间、输入、动画、碰撞、GameObject 绑定和场景生命周期都由外层 Unity Demo 或项目层负责传入和编排，因此同一套 Gameplay API 可以被 Unity、EditMode 测试、CLI 工具、预览服务和 Mod 数据验证共同使用。

## 公开接口

| 接口/类型 | 用途 |
|-----------|------|
| `IRuntimeEntity` | 最小运行时实体契约，组合身份、队伍、存活判断、Buff 目标和 Ability 事件 |
| `RuntimeEntity` | 默认纯 C# 组合实现，包含 `AttributeStore`、`BuffPipeline`、`ModifierPipeline` 和事件总线 |
| `GameplayWorld` | Gameplay world v0 组合根，持有实体注册表、稳定 tick 和 world snapshot |
| `RuntimeEntityRegistry` | 按 `EntityId` 注册、查询、移除和稳定排序枚举实体 |
| `GameplayWorldSnapshot` | World tick 与实体列表的只读拷贝 |
| `GameplayTeamRelation` / `GameplayTeamRelations` | SameTeam / Enemy / Neutral 判定工具 |
| `GameplayTagId` / `GameplayTagSet` | 稳定 int tag id 和升序枚举 tag set |
| `GameplayStatusId` / `GameplayStatusSet` | 稳定 int status id 和升序枚举 status set |
| `IAbility` | 技能释放接口 |
| `AbilityContext` | 单次释放上下文，包含 caster 和候选目标 |
| `AbilityCastResult` | 释放成功/失败结果和命中目标 |
| `GameplayAbilityRegistry` | 按 ability id 注册和查询运行时 Ability |
| `GameplayAbilityCastRequest` | 通过 caster id、ability id、candidate ids 和 trace id 表达释放请求 |
| `GameplayAbilityRuntimeService` | 将世界实体 / ability id 解析为 `AbilityContext` 并调用 `IAbility.Cast` |
| `GameplayAbilityRuntimeResult` / `GameplayAbilityRuntimeFailureCode` | Ability runtime adapter 的结构化结果和失败码 |
| `GameplayRuntimeModule` | `RuntimeHost` 模块入口，drain `RuntimeCommandBuffer`、执行 Gameplay command、tick world 并输出 frame event |
| `GameplayRuntimeCommandIds` / `GameplayRuntimeCommandFactory` | Gameplay command id 和 `RuntimeCommand` 构造工具 |
| `GameplayRuntimeEvent` / `GameplayRuntimeEventType` | Gameplay 按帧事件 DTO，用于 UI、Audio、Diagnostics 和 Replay 边界 |
| `ITargetSelector` | 从候选目标中选择技能目标 |
| `GameplayTargetCandidate` | 可目标选择的实体快照，包含 entity/team/alive/tag/status |
| `GameplayTargetQuery` | 通用目标查询：caster、alive、team relation、required tags、blocked statuses、max targets |
| `GameplayTargetingService` | 对候选目标执行 query/filter 并返回 selected/rejected |
| `GameplayTargetingResult` / `GameplayTargetRejectReason` | 目标选择结果和稳定拒绝原因 |
| `IAbilityEffect` | 对单个目标执行效果 |
| `AbilityEvent` / `AbilityEventType` | 技能生命周期事件 |
| `SimpleAbility` | 默认实现：选目标、按顺序执行效果、发布事件 |
| `GameplayDiagnosticSnapshot` | 纯 C# 运行时诊断快照，汇总 Entity / Attribute / Buff / Modifier / Ability / Event 状态 |
| `GameplayDiagnosticSnapshotBuilder` | 从公开 runtime 对象和事件日志构建诊断快照 |
| `GameplayWorldDiagnostics` / `GameplayWorldDiagnosticsSummary` | World/entity 诊断摘要入口 |
| `GameplayHashContributor` | 将 Gameplay entity/world 状态贡献给 Runtime hash contract |
| `AbilityGraphDefinition` / `AbilityGraphNode` / `AbilityGraphEdge` | Ability Runtime Graph v0 定义、节点和边 |
| `AbilityGraphValidator` / `AbilityGraphValidationResult` | 图定义结构化校验，覆盖入口、边、payload、cycle |
| `AbilityGraphRuntimeExecutor` | 确定性执行 Ability Graph v0 节点 |
| `AbilityGraphExecutionContext` / `AbilityGraphExecutionResult` | 单次 graph 执行输入、输出、失败码和 trace |
| `AbilityGraphRuntimeEffectRegistry` | 按 effect id 解析 `IAbilityEffect` 的最小运行时 registry |
| `AbilityGraphTimelineDefinition` / `AbilityGraphTimelineScheduler` | 纯 C# phase timeline 定义和显式 frame 推进 |
| `AbilityGraphTimelinePhaseGate` | 将 timeline state 适配到 executor phase gate |
| `AbilityGraphDiagnosticSnapshot` | 图定义、校验和执行 trace 的只读诊断快照 |
| `AbilityGraphExecutionTrace` | 节点、目标决策、事件和失败原因的稳定执行 trace |
| `AbilityGraphHashContributor` | Ability Graph definition 的 Runtime hash contributor |
| `SelfTargetSelector` | 选择 caster 自身 |
| `SingleEnemyTargetSelector` | 选择第一个不同队伍且存活的实体 |
| `DamageEffect` | `max(1, attack - defense)` 扣目标 HP |
| `ApplyBuffEffect` | 通过工厂创建 Buff 并添加到目标 |

## 最小关系

```text
RuntimeEntity
  owns AttributeStore / BuffPipeline / ModifierPipeline / AbilityEvents
  implements IBuffTarget

AbilityContext
  Caster + Candidates

IAbility.Cast(context)
  -> ITargetSelector.SelectTargets(context)
  -> IAbilityEffect.Apply(context, target)
  -> AttributeStore / BuffPipeline
  -> AbilityEvent

GameplayWorld
  owns RuntimeEntityRegistry
  -> stable Tick(deltaTime)
  -> world snapshot / diagnostics / hash contributor

GameplayRuntimeModule
  drains RuntimeCommandBuffer
  -> CastAbility / DespawnEntity command
  -> GameplayWorld + GameplayAbilityRuntimeService
  -> RuntimeEventQueue<GameplayRuntimeEvent>

AbilityGraphRuntimeExecutor
  validates AbilityGraphDefinition
  -> TargetQuery via GameplayTargetingService
  -> ApplyEffect via IAbilityEffect
  -> EmitEvent via AbilityEvent
  -> optional PhaseGate via AbilityGraphTimelinePhaseGate
  -> execution trace / diagnostics / hash
```

## Gameplay World v0

`GameplayWorld` 是纯 C# world root，不读取 Unity 时间，也不持有场景对象。它只管理 `IRuntimeEntity` 的注册关系、稳定 tick 和 world snapshot。

最小能力：

- `RuntimeEntityRegistry.Register(entity)`：拒绝 null、非法 `EntityId <= 0` 和重复 id。
- `TryGet(entityId, out entity)` / `Remove(entityId)`：提供可诊断查询与移除。
- Registry 枚举和 snapshot 按 `EntityId` 升序稳定输出。
- `GameplayWorld.Tick(deltaTime)`：校验 finite non-negative delta，并按 registry snapshot tick 每个实体的 `BuffPipeline`。
- `GameplayWorldSnapshot`：复制 tick count 和实体列表，后续 registry 变化不改变旧 snapshot。

Team / Tag / Status 边界：

- Team 只表达 SameTeam / Enemy / Neutral，不包含项目阵营文案。
- `GameplayTagId` / `GameplayStatusId` 使用稳定 int id；`0/default` 为 None，负数构造非法。
- Tag / Status set 的 add/remove/contains 幂等，枚举按 id 升序稳定。

Targeting service 边界：

- `GameplayTargetingService` 只做实体集合上的逻辑过滤，不做 Unity Physics、Combat range 或 NavMesh。
- `GameplayTargetQuery` 支持 alive、self/same-team/enemy relation、required tags、blocked statuses 和 max targets。
- `GameplayTargetingResult` 保留 selected targets 和 rejected reasons，便于 Debug、AI 和 Authoring Preview 使用。

Ability runtime adapter：

- `GameplayAbilityRegistry` 按 ability id 注册 `IAbility`，拒绝 null / duplicate。
- `GameplayAbilityRuntimeService.Cast(request)` 解析 caster、ability 和 optional candidate ids，再复用现有 `IAbility.Cast(AbilityContext)`。
- 缺 caster、缺 ability、空候选目标都会返回结构化 failure code，不抛难以诊断的空引用。

Runtime module / command loop：

- `GameplayRuntimeModule` 可注册到 `RuntimeHost`，默认 `Simulation` stage、priority `100`，让 timer 等更早 priority 的模块先投递 command。
- Module 每帧调用 `RuntimeCommandBuffer.DrainForFrame(frame)`，按 `RuntimeCommandBuffer` 的稳定排序执行 Gameplay command。
- v0 command 包含 `CastAbility` 和 `DespawnEntity`。`CastAbility` payload 约定为 `payload0=casterEntityId`、`payload1=abilityId`、`payload2=optional single candidateEntityId`。
- `GameplayRuntimeCommandFactory` 提供 command 构造入口，避免 Demo / 项目层手写 command id 和 payload 位序。
- Module 默认在 command 后调用 `GameplayWorld.Tick(deltaTime)`；需要外部手动 tick 时可关闭 `tickWorldAutomatically`。
- Module 将结果写入 `RuntimeEventQueue<GameplayRuntimeEvent>`，事件包含 frame、command、caster、ability、target、failure code、reason 和 traceId。UI / Audio / Diagnostics 应消费事件队列，而不是直接监听内部私有状态。

Hash / diagnostics：

- `GameplayHashContributor` 实现 `IRuntimeHashContributor`，可接 entity list 或 `GameplayWorld`。
- Hash 输入按 entity id、attribute id、buff id、modifier id 等稳定顺序写入。
- `GameplayWorldDiagnostics` 复用 `GameplayDiagnosticSnapshotBuilder`，并提供 entity/alive/attribute/buff/modifier 计数摘要。

## Ability Runtime Graph v0

Ability Runtime Graph 是 `SimpleAbility` 之上的数据驱动运行时表达。它不替代现有 `IAbility`，而是给后续编辑器、配置映射、Replay 和 Preview 提供稳定图模型。

最小能力：

- `AbilityGraphDefinition` 持有 graph id、version、entry node、稳定排序 nodes / edges。
- v0 节点：`Entry`、`Sequence`、`TargetQuery`、`ApplyEffect`、`EmitEvent`、`PhaseGate`。
- `AbilityGraphValidator` 返回结构化 error code、node id、edge index、field path，不把 authoring 错误变成空引用异常。
- `AbilityGraphRuntimeExecutor` 按稳定 edge 顺序执行图，并用 step budget 防止损坏图自旋。
- `TargetQuery` 复用 `GameplayTargetingService`；`ApplyEffect` 复用 `IAbilityEffect.Apply(AbilityContext, target)`。
- `AbilityGraphTimelineScheduler` 通过显式 frame 推进 phase，不读取 Unity time、coroutine、Animator 或 Timeline asset。
- `AbilityGraphTimelinePhaseGate` 可以把 timeline state 接入 executor 的 `IAbilityGraphPhaseGate`。
- `AbilityGraphDiagnosticSnapshot` / `AbilityGraphExecutionTrace` / `AbilityGraphHashContributor` 支持诊断、Replay golden fixture 和 Runtime hash。

边界：

- v0 不包含可视化编辑器、GraphView、Unity Inspector authoring。
- v0 不包含 cooldown、cost、cast time、interrupt、公式 DSL 或条件 DSL。
- v0 不绑定 projectile、physics、range、navigation、animation event 或资源引用。
- v0 不迁移 WGame 真实 Ability JSON；项目层应先映射到 Config.Runtime 的 synthetic graph config。

## 最小示例

```csharp
using MxFramework.Gameplay;

const int AttrHp = 1;
const int AttrAttack = 2;
const int AttrDefense = 3;

var caster = new RuntimeEntity(entityId: 1, teamId: 1, hpAttributeId: AttrHp);
var enemy = new RuntimeEntity(entityId: 2, teamId: 2, hpAttributeId: AttrHp);

caster.AttributeStore.RegisterAttribute(AttrHp, 1000);
caster.AttributeStore.RegisterAttribute(AttrAttack, 120);
caster.AttributeStore.RegisterAttribute(AttrDefense, 20);

enemy.AttributeStore.RegisterAttribute(AttrHp, 600);
enemy.AttributeStore.RegisterAttribute(AttrAttack, 80);
enemy.AttributeStore.RegisterAttribute(AttrDefense, 10);

var ability = new SimpleAbility(
    abilityId: 1,
    targetSelector: new SingleEnemyTargetSelector(),
    effects: new IAbilityEffect[]
    {
        new DamageEffect(AttrAttack, AttrDefense, AttrHp)
    });

var context = new AbilityContext(caster, new IRuntimeEntity[] { caster, enemy });
AbilityCastResult result = ability.Cast(context);
```

## Diagnostic Snapshot

`GameplayDiagnosticSnapshotBuilder` 用于把一次 Gameplay 运行片段整理成可测试、可展示、可给 AI 上下文读取的数据对象。它不读取 Unity 场景，也不做 JSON 序列化，只基于调用方传入的实体、属性 ID、最后一次释放结果和事件日志构建快照。

最小输入：

```csharp
var builder = new GameplayDiagnosticSnapshotBuilder();
GameplayDiagnosticSnapshot snapshot = builder.Build(
    sourceName: "ability-slice",
    abilitySource: "BasicAbilityConfig -> ConfigAbilityFactory",
    entities: new[] { caster, enemy },
    attributeIds: new[] { AttrHp, AttrAttack, AttrDefense },
    lastCastResult: result,
    abilityEvents: abilityEvents,
    attributeEvents: attributeEvents);
```

快照内容：

- `Entities`：每个实体的 `EntityId`、`TeamId`、`IsAlive`、指定属性的 `FinalValue`、当前 Buff 快照和 Modifier 快照。
- `AbilitySource` / `LastCast`：最后一次释放的来源、是否成功、失败原因和最后目标实体 ID 列表；常用字段也可通过 `LastCastSuccess`、`LastFailureReason`、`LastTargetEntityIds` 直接读取。
- `AbilityEvents`：按输入顺序保留技能事件，用于验证 `CastStarted -> TargetSelected -> EffectApplied -> CastFinished` 等生命周期。
- `AttributeEvents`：按输入顺序保留属性变化事件，用于追踪 HP、攻击、防御等属性变化。

边界：

- Snapshot 是只读诊断视图，不是存档格式、网络协议或回放系统。
- Builder 只消费公开 API，不访问 `AttributeStore`、`BuffPipeline`、`ModifierPipeline` 的私有字段。
- Builder 不负责事件订阅；调用方需要在运行时自行收集 `AbilityEvent` 和 `AttributeChangedEvent`。
- `attributeIds` 决定哪些属性进入快照；未传入的属性不会被枚举。

## v0 支持

- 默认实体组合实现：属性、Buff、Modifier、技能事件。
- GameplayWorld v0：实体 registry、稳定 tick、world snapshot。
- Team / Tag / Status 基础数据结构。
- GameplayTargetingService 逻辑目标过滤与 rejected reasons。
- GameplayAbilityRuntimeService 世界级 Ability cast adapter。
- GameplayRuntimeModule：RuntimeHost / RuntimeCommandBuffer 驱动的 Gameplay command loop。
- GameplayRuntimeEvent：按帧 drain 的 Gameplay runtime event queue。
- Ability Runtime Graph v0：图契约、确定性执行、phase timeline、diagnostics、hash。
- 自身目标和单敌方目标选择。
- 直接伤害效果。
- 添加 Buff 效果。
- 技能生命周期事件顺序：`CastStarted`、`TargetSelected`、`EffectApplied`、`CastFinished`，失败时为 `CastStarted`、`CastFailed`。
- 运行时诊断快照：Entity / Attribute / Buff / Modifier / Ability / Event 状态汇总。
- Runtime hash contributor：Gameplay entity/world 状态可接入 `RuntimeHashCombiner`。
- 纯 C# EditMode 测试覆盖。

## v0 不支持

- Cooldown、Cost、Mana、CastTime、Interrupt。
- Range、Projectile、Physics、Navigation。
- Animation、Input、Localization、AssetKey。
- WGame Ability JSON 导入或配置表绑定。
- 可视化 Ability Graph 编辑器、GraphView、Timeline asset、Animation Event 绑定。
- 物理范围、Combat bridge、复杂多目标规则库、公式系统、战斗判定优先级。
- 通用 Gameplay SaveState restore、完整 Ability cooldown/cost/cast/interruption 管线、Snapshot JSON 序列化、编辑器面板和 Runtime Preview 协议接入。

## Config Driven Ability

`MxFramework.Config.Runtime` 提供最小配置桥接，不把配置逻辑放进 `MxFramework.Gameplay` 本体：

| 类型 | 用途 |
|------|------|
| `BasicAbilityConfig` | Ability 配置行，ID 范围 `300000-399999` |
| `AbilityTargetSelectorKind` | 目标选择枚举，当前支持 `Self`、`SingleEnemy` |
| `AbilityEffectConfig` | 单个效果配置，包含 `Kind` 和命名化 `AbilityEffectParameters`；旧 `Parameters` 数组保留兼容 |
| `AbilityEffectKind` | 效果枚举，当前支持 `DamageByAttackDefense`、`ApplyBuff` |
| `ConfigAbilityFactory` | 从 `IConfigProvider` 创建 `IAbility` |
| `RuntimeAbilityConfigResolver` | 配置变更后的 Ability 重建入口 |
| `RuntimeConfigChangeSummary` | source、changed ids、rebuilt ids、failed ids 和错误摘要 |
| `AbilityGraphConfig` | synthetic Ability Graph config DTO，不绑定真实项目 JSON |
| `AbilityGraphConfigMapper` | 将 config DTO 映射为 `AbilityGraphDefinition` 并返回带 config path 的 diagnostics |

### Ability Authoring Contract

`AbilityAuthoringContract` 是面向 AI、编辑器表单和 JSON 的工具输入层；`BasicAbilityConfig` 仍然是运行时配置入口。当前链路是：

```text
AI / Editor / JSON
  -> AbilityAuthoringContract
  -> AbilityAuthoringContractValidator
  -> AbilityAuthoringContractMapper
  -> BasicAbilityConfig
  -> ConfigAbilityFactory
  -> SimpleAbility
```

Authoring contract 使用独立枚举和命名字段：

| 类型 | 用途 |
|------|------|
| `AbilityAuthoringContract` | 版本化 Ability 输入，包含 `AbilityId`、`DisplayName`、`Description`、`TargetSelectorKind`、`Effects` |
| `AbilityAuthoringEffectContract` | 单个效果输入，使用 `AttackAttributeId`、`DefenseAttributeId`、`HpAttributeId`、`BuffId` 命名参数 |
| `AbilityAuthoringTargetSelectorKind` | 工具层目标选择，当前支持 `Self`、`SingleEnemy` |
| `AbilityAuthoringEffectKind` | 工具层效果类型，当前支持 `DamageByAttackDefense`、`ApplyBuff` |
| `AbilityAuthoringValidationCode` | 稳定错误码，供测试、AI 修复和编辑器提示使用 |
| `AbilityAuthoringValidationIssue` / `AbilityAuthoringValidationReport` | 结构化校验结果，包含 code、字段路径和 message |
| `AbilityAuthoringSchema` / `AbilityAuthoringSchemaSummary` | 纯 C# schema summary，列出字段中文名、类型、说明、允许值和错误码 |

稳定错误码包括 `MissingAbilityId`、`InvalidAbilityId`、`MissingDisplayName`、`UnknownTargetSelector`、`MissingEffect`、`UnknownEffectKind`、`MissingEffectParameter`、`InvalidAttributeId`、`InvalidBuffId`、`UnsupportedContractVersion`。测试应断言 code，不依赖本地化 message。

新代码应优先使用命名化工厂：

- `AbilityEffectConfig.DamageByAttackDefense(attackAttributeId, defenseAttributeId, hpAttributeId)`
- `AbilityEffectConfig.ApplyBuff(buffId)`

`AbilityEffectConfig.Parameters` 是兼容旧 Demo/测试和早期配置导入器的位序数组：

- `DamageByAttackDefense`：`Parameters[0]=attackAttributeId`，`Parameters[1]=defenseAttributeId`，`Parameters[2]=hpAttributeId`。
- `ApplyBuff`：`Parameters[0]=buffId`。

`ApplyBuff` 必须通过传入 `IBuffFactory` 创建 Buff。`ConfigAbilityFactory` 不硬编码 Demo Buff，也不会静默跳过未知 selector、未知 effect 或参数不足的配置；这些情况会在 `TryCreate` 中返回 `false` 和明确 error。

运行时配置变更使用重建语义：`RuntimeAbilityConfigResolver` 每次通过当前 `IConfigProvider` 创建新的 Ability，旧 `SimpleAbility` 不在原对象上热替换 selector、effects 或 parameters。已挂载 Buff / Modifier 不根据新配置回溯重算；新配置只影响后续新创建实例。`RuntimeConfigChangeSummary` 用于 Demo、测试和后续 Runtime Preview 展示 config source、变更数量、重建结果和失败原因。

## 后续配置接入

配置层不应直接依赖 Demo。`BasicAbilityConfig` 是 v0 配置入口。后续 WGame Ability 迁移应先映射到这套稳定 API，或在项目组合根中扩展自定义 `ITargetSelector` / `IAbilityEffect`，不要让 `MxFramework.Gameplay` 直接依赖 WGame 数据结构。

## 测试入口

`Assets/Scripts/MxFramework/Tests/Ability/AbilitySliceTests.cs`
