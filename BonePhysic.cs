using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#nullable enable annotations

public class BonesPhysic : MonoBehaviour
{
    public const float G = 10f;
    public const float refFdt = 0.02f;
    public Vector3 wind;

    public Camera gameCamera;

    public float maxCameraDistance;

    [Range(0, 100f)]
    public float windStrength;

    static bool DEBUG_MODE;
    public bool debugMode;

    static List<GizmosNode> gizmosPoints = new List<GizmosNode>();
    struct GizmosNode 
    {
        public GizmosNode(Transform pos0, Transform pos1, Color colorR) {
            node = pos1;
            nodeStart = pos0;
            color = colorR;
        }
        public Transform node;
        public Transform nodeStart;
        public Color color;
    }

    class Bone
    {
        public Transform bone;

        public Bone? parent;
        public List<Bone> children;

        public Vector3 prevPos;
        public Vector3 curPos;
        public Vector3 localPos;
        public Quaternion localRot;

        public int id;

        public BonesTree tree;

        public Bone(Transform node, BonesTree treePtr, Bone? parentBone) {
            bone = node;

            children = new List<Bone>();
            parent = parentBone;

            localPos = node.localPosition;
            curPos = node.position;
            prevPos = curPos;
            localRot = node.localRotation;
            tree = treePtr;

            if (parentBone is not null) {
                id = parentBone.id + 1;
            } else
                id = 1;

            foreach (Transform child in node) {
                if (DEBUG_MODE)
                    gizmosPoints.Add(new GizmosNode(node, child, Color.red));

                children.Add(new Bone(child, tree, this));
            }
        }
    }
    
    static Dictionary<string, RootConfig> bonesGroups = new Dictionary<string, RootConfig>();
    
    class BonesTree 
    {
        public Bone root;
        public RootConfig config;
        public bool disabled;

        public BonesTree(Transform rootBone) {
            RootConfig rootConfig = rootBone.gameObject.GetComponent<RootConfig>();
            if (bonesGroups.ContainsKey(rootConfig.BonesGroup)) {
                rootConfig.properties = bonesGroups[rootConfig.BonesGroup].properties;
            }

            config = rootConfig;

            root = new Bone(rootBone, this, null);
        }
    }

    List<BonesTree> trees = new List<BonesTree>();

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

    void Start()
    {   
        DEBUG_MODE = debugMode;

        Transform groups = transform.Find("Groups");
        foreach (Transform group in groups) {
            RootConfig config = group.gameObject.GetComponent<RootConfig>();
            bonesGroups.Add(config.BonesGroup, config);
        }

        GameObject[] bonesArray = GameObject.FindGameObjectsWithTag("PhysicBone");

        foreach(GameObject bone in bonesArray) {
            trees.Add(new BonesTree(bone.transform));
        }
    }

    float fdt;

    void UpdateBone(Bone bone) {
        if (bone.parent is not null)
        {
            float stiffness = bone.tree.config.properties.Stiffness * (refFdt / (fdt * fdt));
            float mass = bone.tree.config.properties.Mass;

            Transform boneParent = bone.parent.bone;

            Vector3 curPos = bone.curPos;

            Vector3 scaledLocalPos = Vector3.Scale(bone.localPos, boneParent.lossyScale);

            Quaternion parentRotation = boneParent.parent.rotation * bone.parent.localRot;
            Vector3 target = boneParent.position + parentRotation * scaledLocalPos;

            float windNoise = Mathf.PerlinNoise(Time.time * (windStrength * 0.5f), bone.id);
            Vector3 windForce = wind.normalized * windNoise * windStrength;

            Vector3 inertia = (curPos - bone.prevPos) * (1 - bone.tree.config.properties.Dumping);
            Vector3 acceleration = (stiffness / mass) * (target - curPos) + windForce/mass;
            Vector3 newPos = curPos + inertia + acceleration * (fdt * fdt);

            bone.prevPos = curPos;

            Vector3 dir = (newPos - boneParent.position).normalized;
            Vector3 normalPos = boneParent.position + dir * scaledLocalPos.magnitude;

            bone.curPos = normalPos;

            Vector3 localDir = (target - boneParent.position).normalized;
            Vector3 desiredDir = (bone.curPos - boneParent.position).normalized;

            Quaternion rotation = Quaternion.FromToRotation(localDir, desiredDir);
            Vector3 rotationVectorUp = rotation * (parentRotation * Vector3.up);
            Vector3 rotationVectorForward = rotation * (parentRotation * Vector3.forward);

            boneParent.rotation = Quaternion.LookRotation(rotationVectorForward, rotationVectorUp);
        } 
        foreach(Bone child in bone.children) {
            UpdateBone(child);
        }
    }
    void ResetBone(Bone bone) {
        bone.bone.rotation = bone.bone.parent.rotation * bone.localRot;
        foreach(Bone child in bone.children) {
            UpdateBone(child);
        }
    }

    void FixedUpdate()
    {
        fdt = Time.fixedDeltaTime;
        trees.ForEach(tree => {
            bool disabled = tree.config.Disable;
            if (!disabled) {
                Vector3 viewportPoint = gameCamera.WorldToViewportPoint(tree.root.bone.position);
                bool isVisible = Mathf.Clamp(viewportPoint.z, 0, maxCameraDistance) == viewportPoint.z  && 
                    Mathf.Clamp(viewportPoint.x, -0.1f, 1.1f) == viewportPoint.x && 
                    Mathf.Clamp(viewportPoint.y, -0.1f, 1.1f) == viewportPoint.y;

                disabled = !isVisible;
            }
            if (disabled) {
                if (!tree.disabled) {
                    ResetBone(tree.root);
                    tree.disabled = true;
                }
                return;
            }
            tree.disabled = false;
            UpdateBone(tree.root);
        });
    }
}
