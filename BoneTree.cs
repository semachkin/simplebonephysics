using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoneTree : MonoBehaviour
{
    public BonesPhysic.Bone root;
    public BonePhysicsConfig config;

    public bool disabled;
    public bool Gravity;

    void Awake()
    {
        root = new BonesPhysic.Bone(transform, this);
    }

    void FixedUpdate()
    {
        float fdt = Time.fixedDeltaTime;

        bool _disabled = false;

        Vector3 viewportPoint = Camera.main.WorldToViewportPoint(root.bone.position);

        if (viewportPoint.z > BonesPhysic.singleton.maxCameraDistance || 
            viewportPoint.x < 0 || viewportPoint.x > 1 || viewportPoint.y < 0 || viewportPoint.y > 1)
            _disabled = true;
        
        if (_disabled)
        {
            if (!disabled)
            {
                BonesPhysic.ResetBone(root, fdt);
                disabled = true;
            }
            return;
        }
        else if (disabled)
        {
            disabled = false;
        }

        BonesPhysic.UpdateBone(root, fdt);
    }
}
