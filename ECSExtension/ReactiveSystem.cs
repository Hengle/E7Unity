using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace E7.Entities
{
    public abstract class ReactiveCSBase<ReactiveComponent> : ComponentSystem
    where ReactiveComponent : struct, IComponentData, IReactive
    {
        private protected abstract IReactiveInjectGroup<ReactiveComponent> ReactiveGroup { get; }

        protected abstract void OnReaction();
        protected override void OnUpdate()
        {
            OnReaction();
            for (int i = 0; i < ReactiveGroup.entities.Length; i++)
            {
                PostUpdateCommands.EndTagResponse<ReactiveComponent>(ReactiveGroup.entities[i]);
            }
        }
    }

    /// <summary>
    /// When you want to make a reactive system that removes that component at the end, this is a nice start.
    /// You can send the whole InjectGroup into the job with [ReadOnly]
    /// Use `InjectedGroup` to get the data.
    /// </summary>
    public abstract class ReactiveCS<ReactiveComponent> : ReactiveCSBase<ReactiveComponent>
    where ReactiveComponent : struct, IComponentData, IReactive
    {
        protected struct InjectGroup : IReactiveInjectGroup<ReactiveComponent>
        {
            public ComponentDataArray<ReactiveComponent> reactiveComponents { get; }
            public EntityArray entities { get; }
            public int Length;
        }
        [Inject] private protected InjectGroup injectedGroup;
        protected InjectGroup InjectedGroup => injectedGroup;

        private protected override IReactiveInjectGroup<ReactiveComponent> ReactiveGroup => injectedGroup;
    }

    /// <summary>
    /// Works with mono things that you have attached `GameObjectEntity` to.
    /// Use the `IReactive` as a way to simulate "method call" to that object.
    /// </summary>
    public abstract class ReactiveMonoCS<ReactiveComponent, MonoComponent> : ReactiveCSBase<ReactiveComponent>
    where ReactiveComponent : struct, IComponentData, IReactive
    where MonoComponent : Component
    {
        protected struct InjectGroup : IReactiveMonoInjectGroup<ReactiveComponent, MonoComponent>
        {
            public ComponentDataArray<ReactiveComponent> reactiveComponents { get; }
            public ComponentArray<MonoComponent> monoComponents { get; }
            public EntityArray entities { get; }
            public int Length;
        }
        [Inject] private protected InjectGroup injectedGroup;
        protected InjectGroup InjectedGroup => injectedGroup;

        /// <summary>
        /// Get the first component captured. Useful when you know there's only one.
        /// Should not be index out of range given the system must have some elements to run.
        /// </summary>
        protected MonoComponent FirstInjected => injectedGroup.monoComponents[0];

        private protected override IReactiveInjectGroup<ReactiveComponent> ReactiveGroup => injectedGroup;
    }

    /// <summary>
    /// When you want to make a reactive system that removes that component at the end, this is a nice start.
    /// You can send the whole InjectGroup into the job with [ReadOnly]
    /// Use `InjectedGroup` to get the data.
    /// </summary>
    public abstract class TagResponseJCS<ReactiveComponent> : JobComponentSystem
    where ReactiveComponent : struct, IComponentData,ITag 
    {
        protected struct InjectGroup : ITagResponseInjectGroup<ReactiveComponent>
        {
            public ComponentDataArray<ReactiveComponent> reactiveComponents { get; }
            public EntityArray entities { get; }
            public int Length;
        }
        [Inject] private protected InjectGroup injectedGroup;
        protected InjectGroup InjectedGroup => injectedGroup;
    }

    /// <summary>
    /// When you want to make a reactive system with additional data on that entity.
    /// Take the content out before sending them to the job so that `data` can be written to.
    /// Use `InjectedGroup` to get the data.
    /// </summary>
    public abstract class TagResponseDataJCS<ReactiveComponent, DataComponent> : JobComponentSystem
    where ReactiveComponent : struct, IComponentData,ITag 
    where DataComponent : struct, IComponentData
    {
        protected struct InjectGroup : ITagResponseDataInjectGroup<ReactiveComponent, DataComponent>
        {
            public ComponentDataArray<ReactiveComponent> reactiveComponents { get; }
            public EntityArray entities { get; }
            public ComponentDataArray<DataComponent> datas { get; }
            public int Length;
        }
        [Inject] private protected InjectGroup injectedGroup;
        protected InjectGroup InjectedGroup => injectedGroup;
    }
}