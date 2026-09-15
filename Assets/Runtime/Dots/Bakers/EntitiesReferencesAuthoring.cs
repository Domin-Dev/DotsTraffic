
using System.Collections.Generic;
using System.Diagnostics;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

public class EntitiesReferencesAuthoring : MonoBehaviour
{
    public GameObject vehiclePrefab;    
    public class Baker : Baker<EntitiesReferencesAuthoring>
    {
        public override void Bake(EntitiesReferencesAuthoring authoring)
        {    
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);    
            AddComponent(entity, new EntitiesReferences()
            {
                VehicleEntity = GetEntity(authoring.vehiclePrefab, TransformUsageFlags.Dynamic)
            });
        }
    }
}

public struct EntitiesReferences : IComponentData
{
    public Entity VehicleEntity; 
}