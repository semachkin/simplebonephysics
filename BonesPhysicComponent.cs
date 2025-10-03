using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[AddComponentMenu("BonesPhysics/RootConfig")]
public class RootConfig : MonoBehaviour
{
    [System.Serializable]
    public struct Properties {
        [Range(0f, 1f)]
        public float Dumping;
        [Range(0, 100f)]
        public float Stiffness;
        [Range(0, 100f)]
        public float Mass;
    }

    public string BonesGroup;

    public bool Disable;
    
    [Space]
    public Properties properties;
}
