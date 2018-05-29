//#define I_AM_WORRIED_ABOUT_EXECEPTION_PERFORMANCE

using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Experimental.PlayerLoop;
using System.Collections.Generic;

namespace E7.ECS
{
    /// <summary>
    /// `ReactiveJCS` has a different approach from `ReactiveCS`.
    /// Because you usually wants to bring all the stuffs to do together inside the job, use `GetReactions<T>`
    /// to get a `ComponentDataArray<T>` of that reactive type.
    /// </summary>
    public abstract class ReactiveJCS<ReactiveGroup> : JobComponentSystem
    where ReactiveGroup : struct, IReactiveGroup 
    {
        /// <summary>
        /// Determines how many inject groups you will have. 
        /// Use the same type with `GetReactions<T>` to get the `ComponentDataArray<T>` of reactives.
        /// </summary>
        protected abstract ComponentType[] ReactsTo { get; }

        private Dictionary<ComponentType, ComponentGroup> allInjects;

        /// <summary>
        /// Dynamically inject reactive entities.
        /// </summary>
        protected override void OnCreateManager(int capacity)
        {
            var types = ReactsTo;
            allInjects = new Dictionary<ComponentType, ComponentGroup>();
            for (int i = 0; i < types.Length; i++)
            {
                allInjects.Add(types[i], GetComponentGroup(types[i], ComponentType.ReadOnly<ReactiveGroup>()));
            }
        }

        protected ComponentDataArray<T> GetReactions<T>() where T : struct, IReactive
        {
            return allInjects[ComponentType.Create<T>()].GetComponentDataArray<T>();
        }

        protected abstract JobHandle OnReaction(JobHandle inputDeps);

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            var jobHandle = OnReaction(inputDeps);
            return jobHandle;
        }
    }

    /// <summary>
    /// A reactive entity could be handled by multiple systems, but they will not persist across frame.
    /// This system will clean them up on each frame.
    /// </summary>
    [UpdateBefore(typeof(Initialization))]
    public class DestroyReactivesSystem : ComponentSystem
    {
        /// <summary>
        /// All entities created from `.Issue` has this shared component data, so they can be cleaned up together at the end frame.
        /// </summary>
        public struct ReactiveEntity : ISharedComponentData { }

        struct AllReactives
        {
            [ReadOnly] public SharedComponentDataArray<ReactiveEntity> reactiveEntities;
            [ReadOnly] public EntityArray entities;
        }
        [Inject] AllReactives allReactives;
        protected override void OnUpdate()
        {
            for (int i = 0; i < allReactives.entities.Length; i++)
            {
                PostUpdateCommands.DestroyEntity(allReactives.entities[i]);
            }
        }
    }

    public abstract class ReactiveCSBase<ReactiveGroup> : ComponentSystem
    where ReactiveGroup : struct, IReactiveGroup 
    {
        private protected abstract IReactiveInjectGroup<ReactiveGroup> InjectedReactivesInGroup { get; }

        /// <summary>
        /// Use `if(ReactsTo<IReactive>...` given that `IReactive` belongs to the group.
        /// </summary>
        protected abstract void OnReaction();
        protected override void OnUpdate()
        {
            //There is a possibility that we have a mono entity but not any reactive entities in `ReactiveMonoCS`.
            for (int i = 0; i < InjectedReactivesInGroup.Entities.Length; i++)
            {
                iteratingEntity = InjectedReactivesInGroup.Entities[i];
                OnReaction();
            }
        }

        private protected Entity iteratingEntity;
        protected bool ReactsTo<T>(out T reactiveComponent) where T : struct, IReactive
        {
            if (EntityManager.HasComponent<T>(iteratingEntity))
            {
                reactiveComponent = EntityManager.GetComponentData<T>(iteratingEntity);
                return true;
            }
            reactiveComponent = default;
            return false;
        }
    }

    /// <summary>
    /// Get all of entity made from `MonoECS.Issue` and `EntityCommandBuffer.Issue` with reactive components.
    /// 
    /// Process each reactive entities captured in this frame one by one with 
    /// `OnReaction`, all of them will be destroyed automatically. (Runs only once)
    /// </summary>
    public abstract class ReactiveCS<ReactiveGroup> : ReactiveCSBase<ReactiveGroup>
    where ReactiveGroup : struct, IReactiveGroup
    {
        /// <summary>
        /// Captures reactive entities ready to be destroy after the task.
        /// </summary>
        protected struct ReactiveInjectGroup : IReactiveInjectGroup<ReactiveGroup>
        {
            [ReadOnly] public SharedComponentDataArray<ReactiveGroup> reactiveGroups;
            public EntityArray entities;
            public int Length;

            public SharedComponentDataArray<ReactiveGroup> ReactiveGroups => reactiveGroups;
            public EntityArray Entities => entities;
        }
        [Inject] private protected ReactiveInjectGroup injectedReactivesInGroup;

        private protected override IReactiveInjectGroup<ReactiveGroup> InjectedReactivesInGroup => injectedReactivesInGroup;
    }

    /// <summary>
    /// Get all of one type of your `MonoBehaviour` that you have `GameObjectEntity` attached. 
    /// Then also get all of entity made from `MonoECS.Issue` and `EntityCommandBuffer.Issue` with reactive components.
    /// Your `MonoBehaviour` can then take action on them.
    /// 
    /// Process each reactive entities captured in this frame one by one with
    /// `OnReaction`, all of them will be destroyed automatically. (Runs only once)
    /// </summary>
    public abstract class ReactiveMonoCS<ReactiveGroup, MonoComponent> : ReactiveCS<ReactiveGroup>
    where ReactiveGroup : struct, IReactiveGroup
    where MonoComponent : Component
    {
        /// <summary>
        /// Captures your `MonoBehaviour`s
        /// </summary>
        protected struct MonoGroup
        {
            [ReadOnly] public ComponentArray<MonoComponent> monoComponents;
            public int Length;
        }
        [Inject] private protected MonoGroup monoGroup;

        /// <summary>
        /// Get the first `MonoBehaviour` captured. Useful when you know there's only one in the scene to take all the reactive actions.
        /// </summary>
        protected MonoComponent FirstMono 
#if !I_AM_WORRIED_ABOUT_EXECEPTION_PERFORMANCE
        => monoGroup.Length > 0 ? monoGroup.monoComponents[0] : throw new System.Exception($"You don't have any {typeof(MonoComponent).Name} which has GameObjectEntity attached...");
#else
        => monoGroup.monoComponents[0];
#endif

        /// <summary>
        /// Iterate on all `MonoBehaviour` captured.
        /// </summary>
        protected IEnumerable<MonoComponent> MonoComponents
        {
            get
            {
                for (int i = 0; i < monoGroup.Length; i++)
                {
                    yield return monoGroup.monoComponents[i];
                }
            }
        }
    }

    /// <summary>
    /// Not really reactive but nice to have... basically get a `MonoBehaviour` entities and an another set of unrelated entities with `IComponentData` that you want.
    /// </summary>
    public abstract class MonoDataCS<MonoComponent,DataComponent> : ComponentSystem
    where MonoComponent : Component
    where DataComponent : struct, IComponentData
    {
        /// <summary>
        /// Captures your `MonoBehaviour`s
        /// </summary>
        protected struct MonoGroup
        {
            [ReadOnly] public ComponentArray<MonoComponent> monoComponents;
            public int Length;
        }
        [Inject] private protected MonoGroup monoGroup;

        protected struct DataGroup
        {
            public ComponentDataArray<DataComponent> dataComponents;
            public int Length;
        }
        [Inject] private protected DataGroup dataGroup;

        /// <summary>
        /// Get the first `MonoBehaviour` captured. 
        /// </summary>
        protected MonoComponent FirstMono 
#if !I_AM_WORRIED_ABOUT_EXECEPTION_PERFORMANCE
        => monoGroup.Length > 0 ? monoGroup.monoComponents[0] : throw new System.Exception($"You don't have any {typeof(MonoComponent).Name} which has GameObjectEntity attached...");
#else
        => monoGroup.monoComponents[0];
#endif

        /// <summary>
        /// Iterate on all `MonoBehaviour` captured.
        /// </summary>
        protected IEnumerable<MonoComponent> MonoComponents
        {
            get
            {
                for (int i = 0; i < monoGroup.Length; i++)
                {
                    yield return monoGroup.monoComponents[i];
                }
            }
        }

        /// <summary>
        /// Iterate on all `IComponentData` captured.
        /// </summary>
        protected IEnumerable<DataComponent> DataComponents
        {
            get
            {
                for (int i = 0; i < dataGroup.Length; i++)
                {
                    yield return dataGroup.dataComponents[i];
                }
            }
        }
    }

    /// <summary>
    /// When you want to make a reactive system that removes that component at the end, this is a nice start.
    /// You can send the whole InjectGroup into the job with [ReadOnly]
    /// Use `InjectedGroup` to get the data.
    /// </summary>
    public abstract class TagResponseJCS<TagComponent> : JobComponentSystem
    where TagComponent : struct, IComponentData, ITag
    {
        protected struct InjectGroup : ITagResponseInjectGroup<TagComponent>
        {
            [ReadOnly] public ComponentDataArray<TagComponent> reactiveComponents;
            [ReadOnly] public EntityArray entities;
            public int Length;

            public ComponentDataArray<TagComponent> ReactiveComponents => reactiveComponents;
            public EntityArray Entities => entities;
        }
        [Inject] private protected InjectGroup injectedGroup;
        protected InjectGroup InjectedGroup => injectedGroup;
    }

    /// <summary>
    /// When you want to make a reactive system with additional data on that entity.
    /// Take the content out before sending them to the job so that `data` can be written to.
    /// Use `InjectedGroup` to get the data.
    /// </summary>
    public abstract class TagResponseDataJCS<TagComponent, DataComponent> : JobComponentSystem
    where TagComponent : struct, IComponentData, ITag
    where DataComponent : struct, IComponentData
    {
        protected struct InjectGroup : ITagResponseDataInjectGroup<TagComponent, DataComponent>
        {
            [ReadOnly] public ComponentDataArray<TagComponent> reactiveComponents;
            [ReadOnly] public EntityArray entities;
            public ComponentDataArray<DataComponent> datas { get; }
            public int Length;

            public ComponentDataArray<TagComponent> ReactiveComponents => reactiveComponents;
            public EntityArray Entities => entities;
        }
        [Inject] private protected InjectGroup injectedGroup;
        protected InjectGroup InjectedGroup => injectedGroup;
    }
}