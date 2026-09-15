
using System.Collections.Generic;
using System.Diagnostics;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

public class VehicleAuthoring : MonoBehaviour
{
    public float Speed;
    public class Baker : Baker<VehicleAuthoring>
    {
        public override void Bake(VehicleAuthoring authoring)
        {    
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);    
            AddComponent(entity, new VehicleComponent());
            AddComponent(entity, new VehicleStats()
            {
                Speed = authoring.Speed
            });
        }
    }
}