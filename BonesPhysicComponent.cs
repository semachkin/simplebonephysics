using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "BonePhysics", menuName = "Configs/BonePhysics")]
public class BonePhysicsConfig : ScriptableObject
{
    [Range(0f, 1f)]
    public float Dumping;
    [Range(0, 100f)]
    public float Stiffness;
    [Range(0, 100f)]
    public float Mass;
}
