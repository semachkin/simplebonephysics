using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Chunks3D;

public class BonePhysics : MonoBehaviour
{
    public float G = 9.81f;
    public float refFdt = 0.02f;
    [Range(0, 100f)]
    public float windStrength;
    public Vector3 wind;
    public bool windEnabled = true;

    public float maxCameraDistance;
    public float minCameraDistance;

    public float collisionChunkSize;
    public float collisionGarbageCollectPeriod;

    public static BonePhysics Instance;

    static List<GizmosNode> gizmosPoints = new List<GizmosNode>();
    public class GizmosNode 
    {
        public Bone bone;
        public Transform node;
        public Transform nodeStart;
        public Color color;

        public Vector3 prevPos => bone.prevPos;

        public GizmosNode(Bone boneChild, Color colorR) 
        {
            node = boneChild.bone;
            nodeStart = boneChild.parent.bone;
            bone = boneChild;
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

        public int id = 0;

        public BoneTree tree;

        public void UpdateDescendants(float fdt)
        {
            foreach (Bone child in children)
                BonePhysics.UpdateBone(child, fdt);
        }

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

            if (parentBone != null)
            {
                id = parentBone.id + 1; 
            }
            else 
            {
                id = -1;
            }

            foreach (Transform child in node) 
            {
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
        Instance = this;
    }

#if BONEPHYS_DEBUG
    void OnDrawGizmos() 
    {
        if (gizmosPoints.Count == 0)
            return;

        gizmosPoints.ForEach(x => {
            Gizmos.color = x.color;
            Gizmos.DrawSphere(x.node.position, 0.01f);
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
        if (bone.parent != null)
        { 
            BonePhysicsConfig config = bone.tree.config;

            float stiffness = config.Stiffness * (BonePhysics.Instance.refFdt / (fdt * fdt));
            float mass = config.Mass;

            Transform boneParent = bone.parent.bone;
            Vector3 curPos = bone.curPos;

            Vector3 windForce = Vector3.zero;
            if (BonePhysics.Instance.windEnabled)
            {
                float windNoise = Mathf.PerlinNoise(Time.time * (BonePhysics.Instance.windStrength * 0.5f), bone.id);
                windForce = BonePhysics.Instance.wind.normalized * windNoise * BonePhysics.Instance.windStrength;
            }

            Vector3 scaledLocalPos = Vector3.Scale(bone.localPos, boneParent.lossyScale);
            float scaledLocalPosMagnitude = scaledLocalPos.magnitude;

            Quaternion parentRotation = boneParent.parent.rotation * bone.parent.localRot;

            Vector3 target;
            if (bone.tree.Gravity)
            {
                target = boneParent.position + Vector3.down * scaledLocalPosMagnitude;
            }
            else
            {
                target = boneParent.position + parentRotation * scaledLocalPos;
            }

            Vector3 targetOffset = target - curPos;

            if (bone.tree.config.Elastic)
            {
                float el = config.Elasticity;
                targetOffset = targetOffset.normalized * (targetOffset.sqrMagnitude * el * el)/el;
            }

            Vector3 inertia = (curPos - bone.prevPos) * (1 - bone.tree.config.Dumping);
            Vector3 acceleration = (stiffness / mass) * targetOffset + windForce/mass;

            if (bone.tree.Gravity) acceleration += BonePhysics.Instance.G * Vector3.down;

            Vector3 newPos = curPos + inertia + acceleration * (fdt * fdt);

            Vector3 parentOffset = newPos - boneParent.position;

            float stretch = Mathf.Max(parentOffset.magnitude, 0.01f);

            Vector3 localOffset = Quaternion.Inverse(parentRotation) * parentOffset;

            Vector3 localOffsetNormalized = localOffset/stretch;

            float angleCos = Mathf.Sqrt(localOffsetNormalized.x * localOffsetNormalized.x + localOffsetNormalized.z * localOffsetNormalized.z);

            float angleCosNormal = localOffset.y > 0 ? angleCos : 1;

            float maxAngleCos = config.ConstMaxAngle ? bone.tree.maxAnglesCos[0] : bone.tree.maxAnglesCos[bone.id];

            if (angleCosNormal >= maxAngleCos)
            {
                Vector3 localOffsetMax = localOffsetNormalized * maxAngleCos;
                
                Vector3 newProjection = new Vector3(localOffsetMax.x, 0, localOffsetMax.z);
                Vector3 newParentOffsetNormal = newProjection + Vector3.up * Mathf.Sqrt(1 - newProjection.sqrMagnitude);

                parentOffset = parentRotation * newParentOffsetNormal * stretch;
            }

            Vector3 normalPos = boneParent.position + parentOffset;

            if (bone.tree.Collision)
            {
                Chunk<BoneCollider> chunk = BoneCollider.Chunks.GetChunk(BoneCollider.Chunks.WorldToChunk(normalPos));

                for (int i = 0; i < chunk.neighbors.Length; i++)
                {
                    Chunk<BoneCollider> neighbor = chunk.neighbors[i];
                    if (neighbor == null || neighbor.items.Count == 0) continue;

                    foreach (BoneCollider collider in neighbor.items)
                    {
                        Vector3 offset;
                        float distance;
                        bool collide = false;

                        if (collider.P0 == collider.P1)
                        {
                            offset = normalPos - collider.P0;
                            distance = offset.magnitude;

                            if (distance < collider.Radius) 
                                collide = true;
                        }
                        else
                        {
                            Vector3 P0A = normalPos - collider.P0;
                            Vector3 P0P1 = collider.P1 - collider.P0;

                            float t = Mathf.Clamp01(Vector3.Dot(P0A, P0P1)/P0P1.sqrMagnitude);

                            Vector3 C = collider.P0 + P0P1 * t;

                            offset = normalPos - C;
                            distance = offset.magnitude;

                            if (distance < collider.Radius)
                                collide = true;
                        }

                        if (collide)
                        {
                            Vector3 normal = distance > 0.001f ? offset/distance : Vector3.up;
                            Vector3 pushOut = normal * (collider.Radius - distance);
                            Vector3 attraction = target - normalPos;
                            Vector3 slip = attraction - normal * Vector3.Dot(attraction, normal);

                            parentOffset += pushOut + slip * config.Slipping;
                            stretch = parentOffset.magnitude;
                        }
                    }
                }
            }

            float minLen = scaledLocalPosMagnitude * config.minStretch;
            float maxLen = scaledLocalPosMagnitude * config.maxStretch;
                
            if (stretch < minLen)
            {
                parentOffset *= minLen/stretch;
            }
            else if (stretch > maxLen)
            {
                parentOffset *= maxLen/stretch;
            }

            normalPos = boneParent.position + parentOffset;

            Vector3 desiredDir = parentOffset.normalized;

            Vector3 baseDir;
            if (bone.tree.Gravity) 
            {
                baseDir = parentRotation * Vector3.up;
            }
            else 
            {
                baseDir = (target - boneParent.position).normalized;
            }

            Quaternion rotation = Quaternion.FromToRotation(baseDir, desiredDir);
            Vector3 rotationVectorUp = rotation * (parentRotation * Vector3.up);
            Vector3 rotationVectorForward = rotation * (parentRotation * Vector3.forward);

            Quaternion targetRot = Quaternion.LookRotation(rotationVectorForward, rotationVectorUp);

            boneParent.rotation = Quaternion.Lerp(parentRotation, targetRot, bone.tree.influence);

            bone.prevPos = curPos;

            if (!float.IsNaN(normalPos.x))
            {
                bone.bone.position = normalPos;
                bone.curPos = normalPos;
            }
        } 

        bone.UpdateDescendants(fdt);
    }
    
    public static void ResetBone(Bone bone, float fdt) 
    {
        bone.bone.rotation = bone.bone.parent.rotation * bone.localRot;

        bone.curPos = bone.bone.position;
        bone.prevPos = bone.curPos;

        bone.UpdateDescendants(fdt);
    }


    void Update()
    {
        BoneCollider.Chunks.UpdateGCTimer();
    }
}
