using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace LaserHero
{
    public class LaserArena : MonoBehaviour
    {
        public static LaserArena Instance;
        public LaserHeroPlayer hero;
        public LaserArenaRotation rotation;
        public LaserCollapseSettings collapseSettings;
        public Bounds arenaBounds = new Bounds(new Vector3(0, 8, 66), new Vector3(50, 35, 58));
        public Vector3 entry = new Vector3(0, 1, 41);
        public float roundDuration = 240;
        public float RemainingTime { get; private set; }
        public bool Inside => hero != null && InArena(hero.transform.position);

        public bool InArena(Vector3 p) => p.x > arenaBounds.min.x && p.x < arenaBounds.max.x
            && p.z > arenaBounds.min.z && p.z < arenaBounds.max.z;
        void Awake()
        {
            Instance = this;
            RemainingTime = roundDuration;
        }

        void OnEnable()
        {
            Instance = this;
            InvalidateGroundIndex();
        }

        void OnDisable()
        {
            if (Instance == this)
                Instance = null;
        }

        bool RotationActive => rotation != null && rotation.isActiveAndEnabled;

        void Update()
        {
            if (RotationActive)
            {
                RemainingTime = rotation.RemainingTime;
                return;
            }

            RemainingTime -= Time.deltaTime;
            if (RemainingTime <= 0)
                ResetArena();
        }

        public static bool CanDamage(LaserVoxelBody body, Vector3 point) => Instance != null
            && (!Instance.RotationActive || Instance.rotation.CanEnterArena) && Instance.Inside && Instance.InArena(point);
        public static bool CanPhysicsDamage(Vector3 point) => Instance != null && (!Instance.RotationActive
            || Instance.rotation.CanEnterArena) && Instance.InArena(point);
        static Dictionary<Vector2Int, List<LaserVoxelBody>> groundIndex;
        public static void InvalidateGroundIndex()
        {
            groundIndex = null;
        }

        public static bool GroundSupports(Vector3 p)
        {
            if (groundIndex == null)
            {
                groundIndex = new Dictionary<Vector2Int, List<LaserVoxelBody>>();
                foreach (var body in LaserVoxelBody.Active)
                    if (body.terrain && !body.isFragment)
                    {
                        Vector3 min = body.transform.position, max =
                            min + (Vector3)body.dimensions * body.cellSize - Vector3.one * .001f;
                        for (int z = Mathf.FloorToInt(min.z / 8); z <= Mathf.FloorToInt(max.z / 8); z++)
                            for (int x = Mathf.FloorToInt(min.x / 8); x <= Mathf.FloorToInt(max.x / 8); x++)
                            {
                                var key = new Vector2Int(x, z);
                                if (!groundIndex.TryGetValue(key, out var list))
                                    groundIndex[key] = list = new List<LaserVoxelBody>();
                                list.Add(body);
                            }
                    }
            }

            if (groundIndex.TryGetValue(new Vector2Int(Mathf.FloorToInt(p.x / 8), Mathf.FloorToInt(p.z / 8)), out var candidates))
                foreach (var body in candidates)
                    if (body != null && body.ContainsSolid(p))
                        return true;
            return false;
        }

        public static void ValidateBuildings(int generation = 0, Vector3? changedPoint = null, float radius = 0)
        {
            foreach (var body in LaserVoxelBody.Active.ToArray())
                if (!body.terrain && !body.isFragment && body.allowDestruction)
                {
                    if (changedPoint.HasValue)
                    {
                        var b = body.GetComponent<MeshRenderer>().bounds;
                        var p = changedPoint.Value;
                        if (p.x < b.min.x - radius || p.x > b.max.x + radius || p.z < b.min.z - radius || p.z > b.max.z + radius)
                            continue;
                    }

                    body.ValidateSupport(generation);
                }
        }

        public void Enter()
        {
            if (RotationActive && !rotation.CanEnterArena)
                return;
            hero.Teleport(entry);
            hero.yaw = 0;
            hero.pitch = 10;
        }

        public void Lobby()
        {
            hero.Teleport(new Vector3(0, 1, 0));
            hero.yaw = 0;
            hero.pitch = 8;
        }

        public void ResetArena()
        {
            // Manual regeneration must use the same protected loading cycle as map voting.
            if (RotationActive)
            {
                rotation.RegenerateCurrentArena();
                return;
            }

            foreach (var body in LaserVoxelBody.Active.ToArray())
                if (body.isFragment)
                {
                    body.gameObject.SetActive(false);
                    Destroy(body.gameObject);
                }
                else
                    body.ResetBody();
            InvalidateGroundIndex();
            Physics.SyncTransforms();
            foreach (var batch in FindObjectsByType<LaserWorldRenderBatcher>(FindObjectsSortMode.None))
                batch.Flush();
            RemainingTime = roundDuration;
            if (Inside)
                hero.Teleport(entry);
        }

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }
    }
}
