# GAMEPLAY_COMPONENT_RUNTIME_V0_CLOSEOUT

## 目标

收口 Component Gameplay Runtime v0，把 09-19 批次形成的 component runtime 能力整理成一个可提交、可验证、可被 agent 继续使用的稳定版本。

本任务不是新增玩法功能，而是做阶段验收：

```text
component schema
-> component world hash / SaveState
-> lifecycle cleanup
-> spawn definitions
-> attribute runtime
-> component ability command / targeting / rules
-> vertical slice
-> docs / interfaces / capabilities
-> clean commit
```

## 背景

当前 component gameplay runtime v0 已具备最小闭环：

```text
RuntimeHost
-> RuntimeCommandBuffer
-> GameplayRuntimeModule
-> GameplaySystemPipeline
-> SpawnComponentEntity
-> GameplayAttributeSetComponent
-> CastComponentAbilityRequest
-> component targeting
-> cooldown / cost rules
-> attribute delta effect
-> lifecycle cleanup
-> event queue
-> hash
-> SaveState roundtrip
```

下一步不应继续扩 Buff、Combat、cast time 或 UI。先把当前阶段的代码、测试和文档对齐，避免后续 agent 在脏工作树和半同步文档上继续叠功能。

## 范围

本任务只做 closeout：

- 确认新增源码、测试、`.meta` 文件都纳入 SVN。
- 确认 `Docs/Interfaces/Gameplay.md`、`Docs/README.md`、`Docs/CAPABILITIES.md` 同步当前 v0 能力。
- 运行 focused tests 和 build。
- 处理明显命名 / 文档 / 测试不一致。
- 提交一个或少数几个按范围拆分的 SVN revision。
- 生成简短 closeout 记录。

## 不做

本任务不要做：

- Buff / Modifier component runtime。
- Combat bridge。
- cast time / interrupt / timeline。
- UI / playable demo scene。
- Config / authoring import。
- API 大重命名。
- 默认 pipeline 行为大调整。

如果 closeout 过程中发现这些需求，只记录为后续任务，不混进本任务。

## 必查文件组

### Runtime source

需要纳入或确认的主要文件组：

- Component schema / hash / SaveState:
  - `GameplayComponentWorldHashContributor`
  - `GameplayComponentWorldSaveState`
  - `GameplayComponentWorldSaveStateProvider`
  - schema registry capability updates
- Lifecycle:
  - `GameplayLifecycleCleanupSystem`
  - `GameplayLifecycleEvents`
- Spawn:
  - `GameplayComponentSpawnDefinition`
  - `GameplayComponentSpawnRegistry`
  - `GameplayComponentSpawnInitializer`
  - `GameplayComponentSpawnCommandSystem`
  - `GameplayComponentSpawnEvents`
- Attribute:
  - `GameplayAttributeValue`
  - `GameplayAttributeSetComponent`
  - `GameplayAttributeCommandSystem`
  - `GameplayAttributeEvents`
  - `GameplayAttributeComponentSchemaDescriptors`
- Component ability:
  - `GameplayComponentAbility`
  - `GameplayComponentAbilityRegistry`
  - `GameplayComponentAbilityCommandSystem`
  - `GameplayComponentAttributeDeltaAbility`
  - `GameplayComponentAbilityEvents`
  - `GameplayComponentAbilityRequest`
  - `GameplayComponentAbilityRequestStore`
- Component targeting:
  - `GameplayComponentTargetCandidate`
  - `GameplayComponentTargetCandidates`
  - `GameplayComponentTargetQuery`
  - `GameplayComponentTargetingResult`
  - `GameplayComponentTargetingService`
- Rules:
  - `GameplayAbilityCooldownComponent`
  - `GameplayAbilityCooldownEntry`
  - `GameplayAbilityCooldownComponentSchemaDescriptors`
  - `GameplayAbilityCost`
  - `GameplayComponentAbilityRuleSet`
  - `GameplayComponentAbilityRuleResult`
  - `GameplayComponentAbilityRules`
- Shared command / event updates:
  - `GameplayRuntimeCommandIds`
  - `GameplayRuntimeCommandFactory`
  - `GameplayRuntimeEvent`
  - `GameplayComponentRegistry`
  - `GameplayEntityLifecycle`

### Tests

需要纳入或确认的测试：

- `GameplayComponentSchemaRegistryTests`
- `GameplayComponentWorldHashContributorTests`
- `GameplayComponentWorldSaveStateTests`
- `GameplayLifecycleCleanupSystemTests`
- `GameplayComponentSpawnDefinitionTests`
- `GameplayAttributeRuntimeTests`
- `GameplayComponentAbilityCommandSystemTests`
- `GameplayComponentAbilityTargetingTests`
- `GameplayComponentAbilityRulesTests`
- `GameplayComponentRuntimeSliceTests`

### Docs

需要确认：

- `Docs/Interfaces/Gameplay.md`
- `Docs/README.md`
- `Docs/CAPABILITIES.md`
- task docs 09-19

## 验证命令

至少运行：

```bash
git diff --check
Tools/GitNexus/gitnexus.sh detect-changes
dotnet build MxFramework.Gameplay.csproj --no-restore
dotnet build MxFramework.Tests.csproj --no-restore
dotnet test MxFramework.Tests.csproj --no-build --filter GameplayComponentRuntimeSliceTests
```

如果 build/test 环境不可用，必须在 closeout 记录里写明原因和替代验证。

建议额外运行：

```bash
dotnet test MxFramework.Tests.csproj --no-build --filter GameplayComponent
```

## 验收标准

- `svn status` 中没有本阶段相关的未纳入 `?` 文件。
- Runtime source、tests、docs、`.meta` 成对提交。
- Component runtime v0 focused tests 通过。
- `GameplayComponentRuntimeSliceTests` 覆盖：
  - spawn
  - attribute
  - targeting
  - ability rules
  - cooldown reject
  - lifecycle cleanup
  - hash
  - SaveState roundtrip
  - request store 不进入 SaveState
- 文档明确：
  - 当前 component gameplay runtime v0 已 closed。
  - 旧 `RuntimeEntity` route 仍保留。
  - 下一阶段才考虑 Buff / Modifier、Combat、cast time、UI / playable demo。
- 提交消息能看出是 v0 closeout，而不是继续扩功能。

## 建议提交策略

如果改动很多，建议拆成 2-3 个 SVN revision：

1. Runtime source + `.meta`
2. Tests + `.meta`
3. Docs closeout

如果当前工作树已经是同一批完整实现，也可以单次提交，但提交前必须确认没有混入无关文件。

## 后续阶段候选

Closeout 完成后，再选择下一阶段：

1. `GAMEPLAY_COMPONENT_BUFF_MODIFIER_01`
   - component-native buff / modifier state。
2. `GAMEPLAY_COMPONENT_COMBAT_BRIDGE_01`
   - damage / hit result / combat event bridge。
3. `GAMEPLAY_COMPONENT_RUNTIME_SHOWCASE_01`
   - playable or diagnostic showcase scene。
4. `GAMEPLAY_COMPONENT_CAST_TIMELINE_01`
   - cast time / interrupt / pending operation。

推荐优先顺序：

```text
Closeout
-> Runtime Showcase
-> Buff / Modifier or Combat bridge
```

原因：当前 runtime v0 已经能跑通测试闭环，下一步最好先做一个可观察、可演示、可诊断的 showcase，再决定继续补 Buff/Combat 哪条业务链。
