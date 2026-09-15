using Unity.Entities;

public struct VehicleSpawner : IComponentData
{
    public int count;
}
public struct SpawnNewVehicles : IComponentData, IEnableableComponent {} 