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
    private BufferLookup<RoadConnections> roadConnections;
    private ComponentLookup<RoadConnectionComponent> connectionLookup;
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<VehicleComponent>();
        pointsA = SystemAPI.GetBufferLookup<ConnectionPointsA>(true);
        pointsB = SystemAPI.GetBufferLookup<ConnectionPointsB>(true);
        connectionLookup = SystemAPI.GetComponentLookup<RoadConnectionComponent>(true);
        roadConnections = SystemAPI.GetBufferLookup<RoadConnections>(true);
    }
    public void OnUpdate(ref SystemState state)
    {
        pointsA.Update(ref state);
        pointsB.Update(ref state);
        connectionLookup.Update(ref state);
        roadConnections.Update(ref state);

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
                if(distanceToNext > distance)
                {
                    position.ValueRW.Position += math.normalizesafe(nextPosition - position.ValueRO.Position) * distance;
                    break;
                }
                else
                {
                    distance -= distanceToNext;
                    position.ValueRW.Position = nextPosition;
                    if((vehicle.ValueRO.directionA ? pointsA[vehicle.ValueRO.CurrentRoadConnection].Length : pointsB[vehicle.ValueRO.CurrentRoadConnection].Length) <= nextPointIndex + 1)
                    {
                        var connection = connectionLookup[vehicle.ValueRO.CurrentRoadConnection];
                        Entity nodeEntity = vehicle.ValueRO.directionA ? connection.nodeA : connection.nodeB;
                        nextPointIndex = 0;
                        
                        var buffer = roadConnections[nodeEntity];
                        var newConnection =  buffer[UnityEngine.Random.Range(0,buffer.Length)];
                        vehicle.ValueRW.CurrentRoadConnection =  newConnection.Connection;
                        vehicle.ValueRW.directionA = connectionLookup[newConnection.Connection].nodeB == nodeEntity;
                    }
                    else
                        nextPointIndex++;


                    if(vehicle.ValueRO.directionA)
                        nextPosition = pointsA[vehicle.ValueRO.CurrentRoadConnection][nextPointIndex].Position;
                    else
                        nextPosition = pointsB[vehicle.ValueRO.CurrentRoadConnection][nextPointIndex].Position;

                    position.ValueRW.Rotation = quaternion.LookRotationSafe(math.normalizesafe(nextPosition -  position.ValueRW.Position), math.up());
                }
            }
            vehicle.ValueRW.PointIndex = nextPointIndex-1;
        }

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
}

