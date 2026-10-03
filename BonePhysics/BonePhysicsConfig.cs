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
    [Range(1, 200f)]
    public float Elasticity;
    [Range(0, 0.1f)]
    public float Slipping;

    public bool Elastic = false;

    public bool ConstMaxAngle = true;

    [Range(0, 90)]
    public float[] maxAngles = {0};

    [Range(0, 1)] 
    public float minStretch = 1;
    [Range(1, 2)]
    public float maxStretch = 1;
}
