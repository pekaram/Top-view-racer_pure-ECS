﻿using UnityEngine;
using Unity.Entities;
using Unity.Transforms;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Burst;

/// <summary>
/// Handles wheel rotation.
/// </summary>
public class WheelRotationSystem : FixedUpdateSystem
{
    private BeginInitializationEntityCommandBufferSystem entityCommandBufferSystem;

    private EntityQuery streetCarsGroup;

    [BurstCompile]
    struct WheelRotationJob : IJobForEach<WheelComponent, Rotation>
    {
        public float DeltaTime;

        [ReadOnly] public ArchetypeChunkComponentType<CarComponent> CarComponentType;
        [ReadOnly] [DeallocateOnJobCompletion] public NativeArray<ArchetypeChunk> Chunks;

        public void Execute(ref WheelComponent wheelComponent, ref Rotation rotation)
        {
            for (var i = 0; i < Chunks.Length; i++)
            {
                var cars = Chunks[i].GetNativeArray(this.CarComponentType);
                for (var j = 0; j < cars.Length; j++)
                {
                    if(wheelComponent.ParentID == cars[j].ID)
                    {
                        wheelComponent.Speed = cars[j].Speed;
                    }
                }
            }
            // Fixed value for now, update based on car speed.
            rotation.Value = math.mul(math.normalize(rotation.Value), quaternion.AxisAngle(new float3(Vector3.left), wheelComponent.Speed));
        }
    }

    protected override void OnCreate()
    {
        this.entityCommandBufferSystem = World.GetOrCreateSystem<BeginInitializationEntityCommandBufferSystem>();
        EntityQueryDesc carsQuery = new EntityQueryDesc
        {
            All = new ComponentType[] { typeof(CarComponent), typeof(Translation) },
        };

        // Get the ComponentGroup
        this.streetCarsGroup = GetEntityQuery(carsQuery);
    }

    protected override JobHandle OnFixedUpdate(JobHandle inputDeps)
    {
        var carComponentType = GetArchetypeChunkComponentType<CarComponent>(true);
        var entityType = GetArchetypeChunkEntityType();
        var chunks = streetCarsGroup.CreateArchetypeChunkArray(Allocator.TempJob);
        WheelRotationJob rotationJob = new WheelRotationJob
        {
            DeltaTime = Time.DeltaTime,
            CarComponentType = carComponentType,
            Chunks = chunks
        };
        return rotationJob.Schedule(this, inputDeps);
    }
}