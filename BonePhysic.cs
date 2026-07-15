using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BonesPhysic : MonoBehaviour
{
    public float G = 9.81f;
    public float refFdt = 0.02f;
    [Range(0, 100f)]
    public float windStrength;
    public Vector3 wind;
    public float maxCameraDistance;

    public static BonesPhysic singleton;

    static List<GizmosNode> gizmosPoints = new List<GizmosNode>();
    public class GizmosNode 
    {
        public Transform node;
        public Transform nodeStart;
        public Color color;

        public GizmosNode(Bone boneChild, Color colorR) 
        {
            node = boneChild.bone;
            nodeStart = boneChild.parent.bone;
            color = colorR;
        }
    }

    public class Bone
    {
        public Transform bone;

        public Bone parent;
        public List<Bone> children;

        public Vector3 prevPos;
        public Vector3 curPos;
        public Vector3 localPos;
        public Quaternion localRot;

        public int id;

        public BoneTree tree;

        public Bone(Transform node, BoneTree treePtr, Bone parentBone = null) 
        {
            bone = node;

            children = new List<Bone>();
            parent = parentBone;

            localPos = node.localPosition;
            curPos = node.position;
            prevPos = curPos;
            localRot = node.localRotation;
            tree = treePtr;

            id = parentBone is null ? 1 : parentBone.id + 1;

            foreach (Transform child in node) {
                Bone boneChild = new Bone(child, tree, this);

                children.Add(boneChild);

#if BONEPHYS_DEBUG
                gizmosPoints.Add(new GizmosNode(boneChild, Color.red));
#endif
            }
        }
    }

    void Awake()
    {
        singleton = this;
    }

#if BONEPHYS_DEBUG
    void OnDrawGizmos() {
        if (gizmosPoints.Count == 0)
            return;
        gizmosPoints.ForEach(x => {
            Gizmos.color = x.color;
            Gizmos.DrawSphere(x.node.position, 0.1f);
            Gizmos.DrawLine(x.nodeStart.position, x.node.position);
        });
    }
    void OnDestroy() 
    {
        gizmosPoints.Clear();
    }
#endif

    public static void UpdateBone(Bone bone, float fdt) 
    {
        if (bone.parent is not null)
        { 
            float stiffness = bone.tree.config.Stiffness * (singleton.refFdt / (fdt * fdt));
            float mass = bone.tree.config.Mass;

            Transform boneParent = bone.parent.bone;

            Vector3 curPos = bone.curPos;

            float windNoise = Mathf.PerlinNoise(Time.time * (singleton.windStrength * 0.5f), bone.id);
            Vector3 windForce = singleton.wind.normalized * windNoise * singleton.windStrength;

            Vector3 scaledLocalPos = Vector3.Scale(bone.localPos, boneParent.lossyScale);

            Quaternion parentRotation = boneParent.parent.rotation * bone.parent.localRot;

            Vector3 baseDir, desiredDir, normalPos;

            if (bone.tree.Gravity) {
                Vector3 target = boneParent.position + Vector3.down * scaledLocalPos.magnitude;

                Vector3 inertia = (curPos - bone.prevPos) * (1 - bone.tree.config.Dumping);
                Vector3 acceleration = (stiffness / mass) * (target - curPos) + mass * singleton.G * Vector3.down + windForce/mass;
                Vector3 newPos = curPos + inertia + acceleration * (fdt * fdt);

                Vector3 dir = (newPos - boneParent.position).normalized;

                normalPos = boneParent.position + dir * scaledLocalPos.magnitude;

                baseDir = parentRotation * Vector3.up;
                desiredDir = (normalPos - boneParent.position).normalized;
            }
            else {
                Vector3 target = boneParent.position + parentRotation * scaledLocalPos;

                Vector3 inertia = (curPos - bone.prevPos) * (1 - bone.tree.config.Dumping);
                Vector3 acceleration = (stiffness / mass) * (target - curPos) + windForce/mass;
                Vector3 newPos = curPos + inertia + acceleration * (fdt * fdt);

                Vector3 dir = (newPos - boneParent.position).normalized;

                normalPos = boneParent.position + dir * scaledLocalPos.magnitude;

                baseDir = (target - boneParent.position).normalized;
                desiredDir = (normalPos - boneParent.position).normalized;
            }

            Quaternion rotation = Quaternion.FromToRotation(baseDir, desiredDir);
            Vector3 rotationVectorUp = rotation * (parentRotation * Vector3.up);
            Vector3 rotationVectorForward = rotation * (parentRotation * Vector3.forward);

            boneParent.rotation = Quaternion.LookRotation(rotationVectorForward, rotationVectorUp);

            bone.prevPos = curPos;
            bone.curPos = normalPos;
        } 
        foreach(Bone child in bone.children) {
            UpdateBone(child, fdt);
        }
    }
    public static void ResetBone(Bone bone, float fdt) 
    {
        bone.bone.rotation = bone.bone.parent.rotation * bone.localRot;
        foreach(Bone child in bone.children)
            UpdateBone(child, fdt);
    }
}
