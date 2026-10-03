using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#nullable enable annotations

public class BoneTree : MonoBehaviour
{
    public BonePhysics.Bone root;
    public BonePhysicsConfig config;

    [HideInInspector]
    public BoneTreesHub hub;

    public bool Gravity;

    public bool Collision = true;

    [Range(0, 1)]
    public float influence = 1;

    public bool? Active => hub?.Active;

    [HideInInspector]
    public float[] maxAnglesCos;

    void Awake()
    {
        root = new BonePhysics.Bone(transform, this);

        maxAnglesCos = new float[config.maxAngles.Length];

        for (int i = 0; i < config.maxAngles.Length; i++)
        {
            maxAnglesCos[i] = Mathf.Cos(config.maxAngles[i] * Mathf.Deg2Rad);
        }
    }

    void FixedUpdate()
    {
        if (Active == true)
            BonePhysics.UpdateBone(root, Time.fixedDeltaTime);
    }
}
