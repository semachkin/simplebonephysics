using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityExtens;

public class BoneTreesHub : MonoBehaviour
{
    [HideInInspector]
    public List<BoneTree> Trees;

    [SerializeField]
    bool active;

    public bool Active
    {
        get => active;
        set
        {
            if (!value)
            {
                foreach (BoneTree tree in Trees)
                    BonePhysics.ResetBone(tree.root, Time.fixedDeltaTime);
            }

            active = value;
        }
    }

    void Awake() 
    {
        Trees = transform.GetDescendants<BoneTree>().Where(t => t.transform.FirstAncestor<BoneTreesHub>() == transform).ToList(); 

        foreach (BoneTree tree in Trees)
            tree.hub = this;
    }

    void FixedUpdate()
    {
        float fdt = Time.fixedDeltaTime;

        bool active;

        float distance = (Camera.main.transform.position - transform.position).magnitude;

        if (distance <= BonePhysics.Instance.minCameraDistance)
        {
            active = true;
            goto UpdateActive;
        }

        Vector3 viewportPoint = Camera.main.WorldToViewportPoint(transform.position);

        bool onScreen = viewportPoint.x > 0 && viewportPoint.x < 1 && viewportPoint.y > 0 && viewportPoint.y < 1;
        
        active = distance <= BonePhysics.Instance.maxCameraDistance && onScreen;
        
        UpdateActive:

        if (active != Active)
            Active = active;
    }
}
