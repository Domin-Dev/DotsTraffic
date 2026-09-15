using System;
using System.Diagnostics;
using System.Linq;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

public partial struct VehicleSpawnSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        EntityQueryBuilder entityQueryBuilder = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<VehicleSpawner,SpawnNewVehicles>();
        state.RequireForUpdate(state.GetEntityQuery(entityQueryBuilder));
        entityQueryBuilder.Dispose();
    }
    public void OnUpdate(ref SystemState state)
    {
        var prefabs = SystemAPI.GetSingleton<EntitiesReferences>();
        var ecb = new EntityCommandBuffer(Allocator.Temp);

        foreach ((RefRO<VehicleSpawner> spawner, EnabledRefRW<SpawnNewVehicles> spawnNewVehicles) in
        SystemAPI.Query<RefRO<VehicleSpawner>,EnabledRefRW<SpawnNewVehicles>>())
        {
            int counter = spawner.ValueRO.count;
            foreach ((RefRO<RoadConnectionComponent> connection,DynamicBuffer<ConnectionVehicles> vehicles,DynamicBuffer<ConnectionPointsA> pointsA,Entity connectionEntity) in
            SystemAPI.Query<RefRO<RoadConnectionComponent>,DynamicBuffer<ConnectionVehicles>,DynamicBuffer<ConnectionPointsA>>().WithEntityAccess())
            {
                if(pointsA.Length < 2) continue;
                Entity entity = ecb.Instantiate(prefabs.VehicleEntity);
                float3 startPosition = pointsA[0].Position;
                float3 nextPosition = pointsA[1].Position;

                float3 direction = math.normalizesafe(nextPosition - startPosition);
                quaternion rotation = quaternion.LookRotationSafe( direction, math.up());
                ecb.SetComponent(entity,LocalTransform.FromPositionRotation(pointsA[0].Position,rotation));       
                ecb.SetComponent(entity,new VehicleComponent()
                {
                    PointIndex = 0,
                    CurrentRoadConnection = connectionEntity,
                    directionA = true,
                });

                vehicles.Append(new ConnectionVehicles(){ entity = entity });
                counter--;
                if(counter <= 0)
                    break;
            }
            spawnNewVehicles.ValueRW = false;
        }

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
}