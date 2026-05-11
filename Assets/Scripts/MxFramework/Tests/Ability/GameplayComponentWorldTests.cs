using System.Collections.Generic;
using MxFramework.Gameplay;
using MxFramework.Runtime;
using NUnit.Framework;

namespace MxFramework.Tests.Ability
{
    public sealed class GameplayComponentWorldTests
    {
        [Test]
        public void DestroyEntity_RemovesRegisteredComponentsAndSnapshotReportsCounts()
        {
            var world = new GameplayComponentWorld();
            GameplayComponentStore<TestStatComponent> stats = world.CreateStore<TestStatComponent>();
            GameplayEntityId entity = world.CreateEntity();
            stats.Set(entity, new TestStatComponent(10));

            GameplayComponentWorldSnapshot before = world.CreateSnapshot();

            Assert.AreEqual(1, before.AliveEntityCount);
            Assert.AreEqual(1, before.ComponentStoreCount);
            Assert.IsTrue(world.DestroyEntity(entity));

            GameplayComponentWorldSnapshot after = world.CreateSnapshot();

            Assert.AreEqual(0, after.AliveEntityCount);
            Assert.AreEqual(1, after.ComponentStoreCount);
            Assert.IsFalse(stats.Contains(entity));
        }

        [Test]
        public void Events_DrainThroughWorldAndClearWithComponentState()
        {
            var world = new GameplayComponentWorld();
            GameplayComponentStore<TestStatComponent> stats = world.CreateStore<TestStatComponent>();
            GameplayEntityId entity = world.CreateEntity();
            stats.Set(entity, new TestStatComponent(10));
            world.EnqueueEvent(new GameplayRuntimeEvent(
                RuntimeFrame.Zero,
                GameplayRuntimeEventType.WorldTicked,
                commandId: 0,
                casterEntityId: 0,
                abilityId: 0,
                targetEntityId: 0,
                failureCode: GameplayAbilityRuntimeFailureCode.None,
                reason: string.Empty,
                traceId: "component-world"));

            Assert.AreEqual(1, world.PendingEventCount);
            var events = new List<GameplayRuntimeEvent>();
            Assert.AreEqual(1, world.DrainEvents(RuntimeFrame.Zero, events));
            Assert.AreEqual("component-world", events[0].TraceId);

            world.EnqueueEvent(events[0]);
            world.Clear();

            Assert.AreEqual(0, world.CountAlive);
            Assert.AreEqual(0, stats.Count);
            Assert.AreEqual(0, world.PendingEventCount);
        }

        [Test]
        public void RuntimeModule_PassesComponentWorldToSystemsAndSharesEventQueue()
        {
            var componentWorld = new GameplayComponentWorld();
            var pipeline = new GameplaySystemPipeline();
            var observed = new List<GameplayComponentWorld>();
            pipeline.Add(new ComponentWorldRecordingSystem(observed));
            var module = new GameplayRuntimeModule(
                new GameplayWorld(),
                new GameplayAbilityRegistry(),
                new RuntimeCommandBuffer(),
                tickWorldAutomatically: false,
                systemPipeline: pipeline,
                componentWorld: componentWorld);

            module.Tick(new RuntimeTickContext(0, 0d, 0d, RuntimeTickStage.Simulation));

            Assert.AreEqual(1, observed.Count);
            Assert.AreSame(componentWorld, observed[0]);
            var events = new List<GameplayRuntimeEvent>();
            Assert.AreEqual(1, module.DrainEvents(RuntimeFrame.Zero, events));
            Assert.AreEqual("component-system", events[0].TraceId);
            Assert.AreEqual(0, componentWorld.PendingEventCount);
        }

        private readonly struct TestStatComponent : IGameplayComponent
        {
            public TestStatComponent(int value)
            {
                Value = value;
            }

            public int Value { get; }
        }

        private sealed class ComponentWorldRecordingSystem : IGameplaySystem
        {
            private readonly List<GameplayComponentWorld> _observed;

            public ComponentWorldRecordingSystem(List<GameplayComponentWorld> observed)
            {
                _observed = observed;
            }

            public string SystemId => "test.component.world";
            public GameplaySystemPhase Phase => GameplaySystemPhase.Simulation;
            public int Priority => 0;
            public bool IsEnabled => true;

            public void Tick(GameplaySystemContext context)
            {
                _observed.Add(context.ComponentWorld);
                context.ComponentWorld.EnqueueEvent(new GameplayRuntimeEvent(
                    context.Frame,
                    GameplayRuntimeEventType.WorldTicked,
                    commandId: 0,
                    casterEntityId: 0,
                    abilityId: 0,
                    targetEntityId: 0,
                    failureCode: GameplayAbilityRuntimeFailureCode.None,
                    reason: string.Empty,
                    traceId: "component-system"));
            }
        }
    }
}
