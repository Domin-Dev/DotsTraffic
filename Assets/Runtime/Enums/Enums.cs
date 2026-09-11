using UnityEngine;


public enum RoadSpace
{
    [InspectorName("3D (XYZ) (default)")]
    XYZ,
    [InspectorName("2D (XY)")]
    XY, 
    [InspectorName("2D (XZ)")]
    XZ, 
    [InspectorName("2D (YZ)")]        
    YZ         
}
public enum HandleMode
{
    Free,
    Aligned,
    Mirrored,
}