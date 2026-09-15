using System;
using System.Diagnostics;
using System.Linq;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine.Pool;

public partial struct VehicleMoveSystem : ISystem
{
    private BufferLookup<ConnectionPointsA> pointsA;
    private BufferLookup<ConnectionPointsB> pointsB;
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<VehicleComponent>();
        pointsA = SystemAPI.GetBufferLookup<ConnectionPointsA>(true);
        pointsB = SystemAPI.GetBufferLookup<ConnectionPointsB>(true);
    }
    public void OnUpdate(ref SystemState state)
    {
        pointsA.Update(ref state);
        pointsB.Update(ref state);

        var prefabs = SystemAPI.GetSingleton<EntitiesReferences>();
        var ecb = new EntityCommandBuffer(Allocator.Temp);
        float deltaTime = SystemAPI.Time.DeltaTime;

        foreach ((RefRW<LocalTransform> position,RefRO<VehicleStats> stats,RefRW<VehicleComponent> vehicle) in
        SystemAPI.Query<RefRW<LocalTransform>,RefRO<VehicleStats>,RefRW<VehicleComponent>>())
        {
            float distance = stats.ValueRO.Speed * deltaTime;
            int nextPointIndex = vehicle.ValueRO.PointIndex + 1;

            while(distance > 0f)
            {
                float3 nextPosition;
                if(vehicle.ValueRO.directionA)
                    nextPosition = pointsA[vehicle.ValueRO.CurrentRoadConnection][nextPointIndex].Position;
                else
                    nextPosition = pointsB[vehicle.ValueRO.CurrentRoadConnection][nextPointIndex].Position;
                
                float distanceToNext = math.distance(position.ValueRO.Position,nextPosition);

                UnityEngine.Debug.Log(distance + " " + distanceToNext);

                           
                if(distanceToNext > distance)
                {
                    UnityEngine.Debug.Log("test");
                    position.ValueRW.Position += math.normalizesafe(nextPosition - position.ValueRO.Position) * distance;
                    break;
                }
                else
                {
                    distance -= distanceToNext;
                    nextPointIndex++;
                   // quaternion rotation = quaternion.LookRotationSafe(math.normalizesafe(pointsA[vehicle.ValueRO.CurrentRoadConnection][nextPointIndex].Position - nextPosition), math.up());
                    position.ValueRW.Position = nextPosition;
                    //position.ValueRW.Rotation = rotation;
                }
            }
            vehicle.ValueRW.PointIndex = nextPointIndex-1;
        }

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
}

