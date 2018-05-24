using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Jobs;

namespace E7.Entities
{
    public static class EntityCommandBufferExtension
    {
        /// <summary>
        /// Use the system with IReactiveInjectGroup to pick it up and act.
        /// </summary>
        public static void Issue<T>(this EntityCommandBuffer ecb, T component)
        where T : struct, IComponentData, IReactive
        {
            ecb.AddComponent<T>(component);
        }

        public static void AddTag<T>(this EntityCommandBuffer ecb,Entity addToEntity)
        where T : struct, IComponentData, ITag
        {
            ecb.AddComponent<T>(addToEntity, default);
        }

        /// <summary>
        /// End a reactive routine by removing a component from an entity. You must specify a reactive component type manually.
        /// </summary>
        public static void EndReactive<ReactiveComponent>(this EntityCommandBuffer ecb, EntityArray entityArray, int entityArrayIndex)
        where ReactiveComponent : struct, IComponentData, IReactive
        {
            ecb.RemoveComponent<ReactiveComponent>(entityArray[entityArrayIndex]);
        }

        public static void EndReactive<ReactiveComponent>(this EntityCommandBuffer ecb, Entity entity)
        where ReactiveComponent : struct, IComponentData, IReactive
        {
            ecb.RemoveComponent<ReactiveComponent>(entity);
        }

        /// <summary>
        /// Destroys the entity, not just removing a component. Use with `Issue` because that creates a new entity.
        /// Just use the IReactiveInjectGroup and it knows what to do.
        /// </summary>
        public static void EndReactive<T>(this EntityCommandBuffer ecb, IReactiveInjectGroup<T> injectGroup, int entityArrayIndex)
        where T : struct, IComponentData, IReactive
        {
            ecb.DestroyEntity(injectGroup.entities[entityArrayIndex]);
        }
    }
}