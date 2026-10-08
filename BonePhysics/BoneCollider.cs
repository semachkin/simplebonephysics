using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Chunks3D;

namespace Chunks3D
{
    public class Chunk<ItemType>
    {
        public List<ItemType> items;

        public Chunk<ItemType>[] neighbors = new Chunk<ItemType>[27];

        public int NonEmptyNeighbors()
        {
            int count = 0;

            for (int i = 0; i < neighbors.Length; i++)
            {
                Chunk<ItemType> neighbor = neighbors[i];

                if (neighbor != null && neighbor.items.Count > 0) count++;
            }

            return count;
        }

        public bool IsNeighborsEmpty()
        {
            bool emptyNeighbors = true;

            for (int i = 0; i < neighbors.Length; i++)
            {
                Chunk<ItemType> neighbor = neighbors[i];
                if (neighbor != null && neighbor.items.Count > 0)
                {
                    emptyNeighbors = false;
                    break;
                }
            }

            return emptyNeighbors;
        }

        public Chunk()
        {
            items = new List<ItemType>();
        }
    }

    public class Chunks<ItemType>
    {
        public enum GarbageCollectMode
        {
            Weak,
            Force
        };

        private Stack<Chunk<ItemType>> chunkPool = new Stack<Chunk<ItemType>>();

        public const int MaxPoolSize = 20;

        public float lastGCTime;
        public float GCPeriod;

        public GarbageCollectMode garbageMode;

        Dictionary<Vector3Int, Chunk<ItemType>> dictionary;

        public float chunkSize;

        public Vector3Int WorldToChunk(Vector3 point) => Vector3Int.FloorToInt(point/chunkSize);

        public Chunk<ItemType> GetChunk(Vector3Int chunkPos)
        {
            if (dictionary.TryGetValue(chunkPos, out Chunk<ItemType> chunk))
            {
                return chunk;
            }
            
            Chunk<ItemType> newChunk;
            if (chunkPool.Count > 0)
            {
                newChunk = chunkPool.Pop();
            }
            else
            {
                newChunk = new Chunk<ItemType>();
            }

            dictionary.Add(chunkPos, newChunk);
            LinkChunkNeighbors(newChunk, chunkPos);

            return newChunk;
        }

        public void RemoveItem(Vector3Int chunkPos, ItemType obj)
        {
            if (dictionary.TryGetValue(chunkPos, out Chunk<ItemType> chunk))
            {
                chunk.items.Remove(obj);
            }
        }

        public void UnlinkChunk(Vector3Int chunkPos, Chunk<ItemType> chunk)
        {
            dictionary.Remove(chunkPos);

            if (garbageMode == GarbageCollectMode.Weak)
            {
                for (int i = 0; i < chunk.neighbors.Length; i++)
                {
                    Chunk<ItemType> neighborChunk = chunk.neighbors[i];
                    if (neighborChunk != null && neighborChunk != chunk)
                    {
                        int oppositeIndex = 26 - i;

                        neighborChunk.neighbors[oppositeIndex] = null;
                    }
                }
            }
        }

        readonly List<Vector3Int> keysToRemoveCache = new List<Vector3Int>();

        public void CollectGarbage()
        {
            int chunksCollected = 0;

            keysToRemoveCache.Clear();

            foreach (KeyValuePair<Vector3Int, Chunk<ItemType>> kvp in dictionary)
            {
                Chunk<ItemType> chunk = kvp.Value;

                if (chunk.items.Count == 0 && (garbageMode == GarbageCollectMode.Weak && chunk.IsNeighborsEmpty()))
                {
                    keysToRemoveCache.Add(kvp.Key);
                    chunksCollected++;
                }
            }

            for (int i = 0; i < keysToRemoveCache.Count; i++)
            {
                Vector3Int key = keysToRemoveCache[i];
                Chunk<ItemType> chunk = dictionary[key];
     
                UnlinkChunk(key, chunk);
                chunksCollected++;

                if (chunkPool.Count < MaxPoolSize)
                {
                    chunkPool.Push(chunk);
                }
            }

            //Debug.Log($"Chunks collected {chunksCollected} Pool size {chunkPool.Count}");
        }

        public void AddItem(Vector3Int chunkPos, ItemType obj)
        {
            Chunk<ItemType> chunk = GetChunk(chunkPos);

            chunk.items.Add(obj);
        }

        public void RemoveItem(Transform obj) => RemoveItem(WorldToChunk(obj.position), obj.GetComponent<ItemType>());

        public void AddItem(Transform obj) => AddItem(WorldToChunk(obj.position), obj.GetComponent<ItemType>());

        public void SwapChunk(ItemType obj, Vector3Int prevChunk, Vector3Int newChunk)
        {
            AddItem(newChunk, obj);
            RemoveItem(prevChunk, obj);
        }

        public Transform FindNearbyTransform(Vector3 point, float maxDistance = Mathf.Infinity)
        {
            Chunk<ItemType> chunk = GetChunk(WorldToChunk(point));

            Transform nearestTransform = null;

            float minSqrDistance = maxDistance * maxDistance;

            for (int i = 0; i < chunk.neighbors.Length; i++)
            {
                Chunk<ItemType> neighbor = chunk.neighbors[i];

                if (neighbor == null) continue;

                List<ItemType> items = chunk.items;
                int itemsCount = items.Count;
        
                for (int j = 0; j < itemsCount; j++)
                {
                    ItemType item = items[j];
            
                    if (item == null) continue; 

                    if (item is MonoBehaviour monoBehaviour)
                    {
                        Transform transform = monoBehaviour.transform;

                        float sqrDistance = (transform.position - point).sqrMagnitude;

                        if (sqrDistance < minSqrDistance)
                        {
                            minSqrDistance = sqrDistance;
                            nearestTransform = transform;
                        }
                    }
                }
            }

            return nearestTransform;
        }

        public void LinkChunkNeighbors(Chunk<ItemType> chunk, Vector3Int pos)
        {
            int index = 0;

            for (int x = -1; x <= 1; x++)
            {
                for (int y = -1; y <= 1; y++)
                {
                    for (int z = -1; z <= 1; z++)
                    {
                        Vector3Int neighborPos = pos + new Vector3Int(x, y, z);

                        if (dictionary.TryGetValue(neighborPos, out Chunk<ItemType> neighborChunk))
                        {
                            chunk.neighbors[index] = neighborChunk;

                            int oppositeIndex = 26 - index;
                            neighborChunk.neighbors[oppositeIndex] = chunk;
                        }

                        index++;
                    }
                }
            }
        }

        public void UpdateGCTimer()
        {
            if (Time.time > (lastGCTime + GCPeriod))
            {
                CollectGarbage();
                lastGCTime = Time.time;
            }
        }

        public Chunks(float chunkSize, float GCPeriod, GarbageCollectMode mode)
        {
            this.chunkSize = chunkSize;
            this.GCPeriod = GCPeriod;
            this.garbageMode = mode;

            dictionary = new Dictionary<Vector3Int, Chunk<ItemType>>();
        }
    }
}

[ExecuteAlways]
public class BoneCollider : MonoBehaviour
{
    public Transform point0;
    public Transform point1;

    public float Radius;

    public Vector3 P0 => point0?.position ?? Vector3.zero;
    public Vector3 P1 => point1?.position ?? P0;

    public static Chunks<BoneCollider> Chunks;

#if BONEPHYS_DEBUG
    public Color GizmosColor = Color.red;

    static GizmosMeshBuilder.CachedMeshes CachedMeshes = new GizmosMeshBuilder.CachedMeshes(8);

    void OnDrawGizmos() 
    {
        Gizmos.color = GizmosColor;

        if (P1 != P0)
        {
            Vector3 dir = P1 - P0;
            Quaternion lookRotation = Quaternion.LookRotation(dir);

            Matrix4x4 originalMatrix = Gizmos.matrix;

            Gizmos.matrix = Matrix4x4.TRS(P0, lookRotation, new Vector3(Radius, Radius, dir.magnitude));

            Gizmos.DrawWireMesh(CachedMeshes.CachedCylinderMesh);

            Gizmos.matrix = Matrix4x4.TRS(P0, lookRotation, new Vector3(Radius, Radius, -Radius));

            Gizmos.DrawWireMesh(CachedMeshes.CachedHalfSphereMesh);

            Gizmos.matrix = Matrix4x4.TRS(P1, lookRotation, new Vector3(Radius, Radius, Radius));

            Gizmos.DrawWireMesh(CachedMeshes.CachedHalfSphereMesh);

            Gizmos.matrix = originalMatrix;
        }
        else
        {
            Gizmos.DrawWireSphere(P0, Radius);
        }
    }

    private void Reset()
    {
        CachedMeshes.Destroy();
    }
#endif

    Vector3Int lastChunk;

    void Start()
    {
        BoneCollider.Chunks.AddItem(transform);

        lastChunk = BoneCollider.Chunks.WorldToChunk(transform.position);
    }

    void OnDestroy() 
    {
        if (Application.isPlaying)
        {
            BoneCollider.Chunks.RemoveItem(transform);
        }
    }

    void Update()
    {
        if (Application.isPlaying)
        {
            Vector3Int chunkPos = BoneCollider.Chunks.WorldToChunk(transform.position);

            if (chunkPos != lastChunk)
            {
                BoneCollider.Chunks.SwapChunk(this, lastChunk, chunkPos);
                lastChunk = chunkPos;
            }
        }        
    }

    void Awake()
    {
        if (BoneCollider.Chunks == null && Application.isPlaying)
        {
            BoneCollider.Chunks = new Chunks<BoneCollider>(
                BonePhysics.Instance.collisionChunkSize, BonePhysics.Instance.collisionGarbageCollectPeriod, Chunks<BoneCollider>.GarbageCollectMode.Weak
            );
        }
    }
}
