using Unity.Entities;
using UnityEngine;

namespace E7.Entities
{
    // /// <summary>
    // /// The same as IReactiveInjectGroup but with additional data inside the entity that contains the reactive component.
    // /// It has effect on methods like commandBuffer.EndReactive where it will just remove the component, not destroying the whole entity.
    // /// </summary>
    // public interface IReactiveDataInjectGroup<ReactiveComponent, DataComponent> : IReactiveInjectGroup<ReactiveComponent> 
    // where DataComponent : struct, IComponentData 
    // where ReactiveComponent : struct, IComponentData, IReactive
    // {
    //     ComponentDataArray<DataComponent> datas { get; }
    // }


    // /// <summary>
    // /// Designed to work with GameObjectEntity attached MonoBehaviours
    // /// </summary>
    // public interface IReactiveMonoInjectGroup<ReactiveComponent, MonoComponent> : IReactiveInjectGroup<ReactiveComponent>
    // where MonoComponent : Component
    // where ReactiveComponent : struct, IComponentData, IReactive
    // {
    //     ComponentArray<MonoComponent> monoComponents { get; }
    // }

    /// <summary>
    /// An inject struct that has a reactive component to be removed at the end of system function,
    /// plus an injected array of entities so that we knows which one to remove a component from.
    /// We need one more component data array with IReactive but it is not enforced in this interface.
    /// </summary>
    public interface IReactiveInjectGroup<RxGroup>
    where RxGroup : struct, IReactiveGroup
    {
        SharedComponentDataArray<RxGroup> ReactiveGroups { get; }
        EntityArray Entities { get; }
    }

    public interface ITagResponseDataInjectGroup<ReactiveComponent, DataComponent> : ITagResponseInjectGroup<ReactiveComponent>
    where DataComponent : struct, IComponentData
    where ReactiveComponent : struct, IComponentData, ITag
    {
        ComponentDataArray<DataComponent> datas { get; }
    }

    public interface ITagResponseInjectGroup<RxComponent> 
    where RxComponent : struct, IComponentData, ITag
    {
        ComponentDataArray<RxComponent> reactiveComponents { get; }
        EntityArray entities { get; }
    }

    /// <summary>
    /// Use when an IComponentData is to be picked up by some system and immediately remove them without condition.
    /// </summary>
    public interface IReactive : ITag { }

    public interface IReactiveGroup : ISharedComponentData { }

    /// <summary>
    /// Use when an `IComponentData` is to stick around and dictates behaviour, or use with `SubtractiveComponent` for example.
    /// Or use when a removal is optional unlike `IReactive`.
    /// </summary>
    public interface ITag : IComponentData { }
}