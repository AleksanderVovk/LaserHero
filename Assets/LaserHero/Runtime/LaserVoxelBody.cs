using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace LaserHero
{
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
    public class LaserVoxelBody : MonoBehaviour
    {
        public Vector3Int dimensions = new Vector3Int(16, 16, 12);
        public float cellSize = .5f, hitPoints = 8;
        public Color bodyColor = new Color(.15f, .65f, 1), trimColor = Color.white;
        public int style;
        public bool collapseUnsupported = true;
        public bool terrain, requiresGroundSupport = true, isFragment, allowDestruction = true;
        public Color accentColor = new Color(1, .9f, .2f);
        public Color[] surfaceColors;
        public bool singleMaterialSurface;
        public static event System.Action<LaserVoxelBody> SurfaceChanged;
        public byte[] palette;
        public bool autoRespawn;
        public int physicalGeneration;
        int splitGeneration;
        public float respawnDelay = 40;
        public string displayName = "Постройка";
        float[] health;
        byte[] original;
        Mesh mesh;
        Matrix4x4 studTextureAnchor;
        bool hasStudTextureAnchor;
        MaterialPropertyBlock fragmentProperties;
        readonly List<Vector3> meshVertices = new List<Vector3>();
        readonly List<Vector3> meshNormals = new List<Vector3>();
        readonly List<Color> meshColors = new List<Color>();
        List<int>[] meshTriangles;
        int[] greedyMask, supportSeen, supportQueue;
        int supportStamp;
        struct SupportGroup
        {
            public int start, count;
            public bool anchored;
        }

        readonly List<SupportGroup> supportGroups = new List<SupportGroup>();
        readonly List<BoxCollider> compoundBoxes = new List<BoxCollider>();
        bool[] compoundUsed;
        bool dirty;
        bool roundDamaged;
        float emptyAt = -1;
        int remaining;
        int chipSequence;
        float[] sectionStrength;
        int[] sectionCounts;
        public int FractureCount { get; private set; }
        public Vector3 LastFracturePoint { get; private set; }
        public static int ChipsEmitted { get; private set; }
        public int Remaining => remaining;
        public float SolidWorldVolume => remaining * cellSize * cellSize * cellSize
            * Mathf.Abs(transform.lossyScale.x * transform.lossyScale.y * transform.lossyScale.z);
        public float PhysicsMass => Mathf.Clamp(SolidWorldVolume * LaserCollapseSettings.Current.density, .1f,
            Mathf.Max(1, LaserCollapseSettings.Current.maximumMass));
        public int Total { get; private set; }
        public float LastHitAt { get; private set; } = -100;

        public static readonly HashSet<LaserVoxelBody> Active = new HashSet<LaserVoxelBody>();
        static readonly Vector3Int[] Directions =
        {
            Vector3Int.right,
            Vector3Int.left,
            Vector3Int.up,
            Vector3Int.down,
            new Vector3Int(0, 0, 1),
            new Vector3Int(0, 0, -1)
        };
        static readonly Vector3[, ] Faces =
        {
            {
                new Vector3(1, 0, 0),
                new Vector3(1, 1, 0),
                new Vector3(1, 1, 1),
                new Vector3(1, 0, 1)
            },
            {
                new Vector3(0, 0, 1),
                new Vector3(0, 1, 1),
                new Vector3(0, 1, 0),
                new Vector3(0, 0, 0)
            },
            {
                new Vector3(0, 1, 1),
                new Vector3(1, 1, 1),
                new Vector3(1, 1, 0),
                new Vector3(0, 1, 0)
            },
            {
                new Vector3(0, 0, 0),
                new Vector3(1, 0, 0),
                new Vector3(1, 0, 1),
                new Vector3(0, 0, 1)
            },
            {
                new Vector3(1, 0, 1),
                new Vector3(1, 1, 1),
                new Vector3(0, 1, 1),
                new Vector3(0, 0, 1)
            },
            {
                new Vector3(0, 0, 0),
                new Vector3(0, 1, 0),
                new Vector3(1, 1, 0),
                new Vector3(1, 0, 0)
            }
        };
        void OnEnable()
        {
            Active.Add(this);
            // Rebuilding a mesh under an inactive root can leave its native collider unbound.
            // Commit physics again once the object has entered the active hierarchy.
            if (health != null && mesh != null)
                BindCollision();
            if (terrain)
                LaserArena.InvalidateGroundIndex();
            SurfaceChanged?.Invoke(this);
        }

        void OnDisable()
        {
            Active.Remove(this);
            if (terrain)
                LaserArena.InvalidateGroundIndex();
            SurfaceChanged?.Invoke(this);
        }

        void Start()
        {
            if (health == null)
                Generate();
        }

        int Index(int x, int y, int z) => x + dimensions.x * (y + dimensions.y * z);
        bool Inside(int x, int y, int z) => x >= 0 && y >= 0 && z >= 0 && x < dimensions.x && y < dimensions.y && z < dimensions.z;
        bool Solid(int x, int y, int z) => Inside(x, y, z) && health[Index(x, y, z)] > 0;
        public void Generate()
        {
            int n = dimensions.x * dimensions.y * dimensions.z;
            health = new float[n];
            original = new byte[n];
            palette = new byte[n];
            Total = 0;
            for (int z = 0; z < dimensions.z; z++)
                for (int y = 0; y < dimensions.y; y++)
                    for (int x = 0; x < dimensions.x; x++)
                    {
                        bool wall = x < 2 || z < 2 || x >= dimensions.x - 2 || z >= dimensions.z - 2;
                        bool floor = y % 6 == 0;
                        bool exists = wall || floor;
                        // Windows, doorways, layered roof and different silhouette families.
                        if (wall && y % 6 >= 2 && y % 6 <= 4 && (x % 5 >= 2 && x % 5 <= 3 || z % 5 >= 2 && z % 5 <= 3))
                            exists = false;
                        if (z < 2 && x > dimensions.x / 2 - 2 && x < dimensions.x / 2 + 2 && y < 4)
                            exists = false;
                        if (style == 1)
                            exists = (x < 3 || x >= dimensions.x - 3) && (z < 3 || z >= dimensions.z - 3) || floor;
                        if (style == 2)
                            exists = y < dimensions.y - 4 ? (wall || floor)
                                : x >= y - (dimensions.y - 4) && x < dimensions.x - (y - (dimensions.y - 4));
                        if (style == 3)
                            exists = true;
                        if (style == 4)
                            exists = (x >= 4 && x <= 5 && z >= 4 && z <= 5 && y < 9) || (x >= 1
                                && x <= 8 && z >= 1 && z <= 8 && y >= 7 && y <= 11) || (x >= 2 && x <= 7 && z >= 2
                                && z <= 7 && y >= 12);
                        if (style == 5)
                            exists = (y == 3) || (z == dimensions.z - 1 && y >= 3) || (y < 3
                                && (x == 1 || x == dimensions.x - 2) && (z == 0 || z == dimensions.z - 1));
                        if (style == 6)
                            exists = (x >= 1 && x <= 2 && z >= 1 && z <= 2
                                && y < dimensions.y - 1) || (y >= dimensions.y - 2 && x >= 1) || (x >= dimensions.x - 2
                                && y >= dimensions.y - 5);
                        if (style == 8)
                            exists = true;
                        int i = Index(x, y, z);
                        palette[i] = (byte)(style == 4 ? (y >= 7 ? 1 : 0) : style == 6
                            ? (x >= dimensions.x - 2 && y >= dimensions.y - 5 ? 2 : 0) : y % 6 == 0
                            || y == dimensions.y - 1 ? 1 : 0);
                        if (style == 8)
                            palette[i] = (byte)(y == dimensions.y - 1 ? 1 : 0);
                        if (style >= 70)
                            exists = LaserParkShapes.Cell(style, x, y, z, dimensions, cellSize, transform.position, out palette[i]);
                        else if (style >= 50)
                            exists = LaserOceanShapes.Cell(style, x, y, z, dimensions, cellSize,
                                transform.position, out palette[i]);
                        else if (style >= 30)
                            exists = LaserSpaceShapes.Cell(style, x, y, z, dimensions, cellSize,
                                transform.position, out palette[i]);
                        else if (style >= 9)
                            exists = LaserCityShapes.Cell(style, x, y, z, dimensions, cellSize, transform.position, out palette[i]);
                        if (exists)
                        {
                            original[i] = 1;
                            health[i] = hitPoints;
                            Total++;
                        }
                    }

            remaining = Total;
            emptyAt = -1;
            roundDamaged = false;
            EnsureSupportStorage();
            Rebuild();
            if (terrain)
                LaserArena.InvalidateGroundIndex();
        }

        public int Carve(Vector3 worldPoint, float radius, float damage, Vector3 direction, int maxDestroyed =
            int.MaxValue, bool physicalImpact = false, int impactGeneration = 0)
        {
            if (!isActiveAndEnabled || health == null || !allowDestruction || maxDestroyed <= 0
                || !(physicalImpact ? LaserArena.CanPhysicsDamage(worldPoint) : LaserArena.CanDamage(this, worldPoint)))
                return 0;
            splitGeneration = physicalImpact ? impactGeneration + 1 : physicalGeneration;
            LastHitAt = Time.time;
            Vector3 local = transform.InverseTransformPoint(worldPoint) / cellSize;
            float r = Mathf.Max(.55f, radius / cellSize);
            int killed = 0, chips = 0;
            int x0 = Mathf.Max(0, Mathf.FloorToInt(local.x - r)), x1 = Mathf.Min(dimensions.x - 1, Mathf.CeilToInt(local.x + r));
            int y0 = Mathf.Max(0, Mathf.FloorToInt(local.y - r)), y1 = Mathf.Min(dimensions.y - 1, Mathf.CeilToInt(local.y + r));
            int z0 = Mathf.Max(0, Mathf.FloorToInt(local.z - r)), z1 = Mathf.Min(dimensions.z - 1, Mathf.CeilToInt(local.z + r));
            for (int z = z0; z <= z1 && killed < maxDestroyed; z++)
                for (int y = y0; y <= y1 && killed < maxDestroyed; y++)
                    for (int x = x0; x <= x1 && killed < maxDestroyed; x++)
                    {
                        int i = Index(x, y, z);
                        if (health[i] <= 0)
                            continue;
                        if (Vector3.Distance(new Vector3(x + .5f, y + .5f, z + .5f), local) > r + .35f)
                            continue;
                        health[i] -= damage;
                        roundDamaged = true;
                        if (health[i] <= 0)
                        {
                            remaining--;
                            killed++;
                            chipSequence++;
                            if (chips < 5 && chipSequence % 5 == 0)
                            {
                                LaserDebris.Emit(transform.TransformPoint(new Vector3(x
                                    + .5f, y + .5f, z + .5f) * cellSize), CellColor(x, y, z), cellSize, -direction);
                                chips++;
                                ChipsEmitted++;
                            }

                            dirty = true;
                        }
                    }

            if (killed > 0)
            {
                if (collapseUnsupported || isFragment)
                    ValidateSupport(splitGeneration);
                if (terrain)
                    LaserArena.ValidateBuildings(splitGeneration, worldPoint, radius);
            }

            if (remaining == 0 && emptyAt < 0)
                emptyAt = Time.time;
            return killed;
        }

        public bool ContainsSolid(Vector3 world)
        {
            if (health == null)
                return false;
            Vector3 p = transform.InverseTransformPoint(world) / cellSize;
            return Solid(Mathf.FloorToInt(p.x), Mathf.FloorToInt(p.y), Mathf.FloorToInt(p.z));
        }

        public int FractureWeakSection(Vector3 worldContact, float impactSpeed, int budget)
        {
            var settings = LaserCollapseSettings.Current;
            if (!isActiveAndEnabled || !isFragment || health == null || remaining < 24
                || !settings.fractureFallingObjects || physicalGeneration >= settings.maximumGeneration
                || FractureCount >= settings.maximumSelfFractures || !LaserArena.CanPhysicsDamage(worldContact))
                return 0;
            Vector3 size = Vector3.Scale((Vector3)dimensions * cellSize, transform.lossyScale);
            size = new Vector3(Mathf.Abs(size.x), Mathf.Abs(size.y), Mathf.Abs(size.z));
            int axis = size.y >= size.x && size.y >= size.z ? 1 : size.z >= size.x ? 2 : 0;
            float width = Mathf.Max(size[(axis + 1) % 3], size[(axis + 2) % 3]);
            float aspect = size[axis] / Mathf.Max(cellSize, width);
            int length = dimensions[axis];
            if (aspect < settings.minimumFractureAspect || length < 8)
                return 0;
            if (sectionStrength == null || sectionStrength.Length != length)
            {
                sectionStrength = new float[length];
                sectionCounts = new int[length];
            }
            else
            {
                System.Array.Clear(sectionStrength, 0, length);
                System.Array.Clear(sectionCounts, 0, length);
            }

            for (int i = 0; i < health.Length; i++)
                if (health[i] > 0)
                {
                    int slice = axis == 0 ? i % dimensions.x : axis == 1
                        ? (i / dimensions.x) % dimensions.y : i / (dimensions.x * dimensions.y);
                    sectionCounts[slice]++;
                    sectionStrength[slice] += Mathf.Clamp01(health[i] / Mathf.Max(.001f, hitPoints));
                }

            float contact = transform.InverseTransformPoint(worldContact)[axis] / cellSize;
            int best = -1, left = 0;
            float bestScore = float.MaxValue;
            for (int slice = 0; slice < length; slice++)
            {
                int count = sectionCounts[slice];
                if (slice >= Mathf.Max(2, length / 5) && slice < Mathf.Min(length - 2, length * 4 / 5)
                    && left >= Mathf.Max(8, remaining / 12) && remaining - left - count >= Mathf.Max(8, remaining / 12)
                    && count > 0)
                {
                    float centerDistance = Mathf.Abs((slice + .5f) / length - .5f) * 2;
                    float lever = Mathf.Min(.5f, Mathf.Abs(slice + .5f - contact) / length);
                    float score = sectionStrength[slice] * (1 + centerDistance * .6f) / (.5f + lever);
                    if (score < bestScore)
                    {
                        bestScore = score;
                        best = slice;
                    }
                }

                left += count;
            }

            if (best < 0)
                return 0;
            float average = remaining / (float)length;
            float threshold = settings.fractureSpeed / Mathf.Sqrt(aspect) * Mathf.Sqrt(Mathf.Max(.25f,
                sectionStrength[best] / Mathf.Max(1, average)));
            if (impactSpeed < threshold || .5f * PhysicsMass * impactSpeed * impactSpeed < settings.minimumFractureEnergy)
                return 0;
            // A one-cell jagged height field severs the whole section. Do not leave a half-cut
            // beam when the frame budget is exhausted: postpone the entire cut instead.
            int cutCount = 0;
            for (int i = 0; i < health.Length; i++)
                if (health[i] > 0 && OnFractureCut(CellPosition(i), axis, best))
                    cutCount++;
            if (cutCount == 0 || cutCount > Mathf.Min(budget, settings.maximumFractureVoxels))
                return 0;
            for (int i = 0; i < health.Length; i++)
                if (health[i] > 0 && OnFractureCut(CellPosition(i), axis, best))
                {
                    health[i] = 0;
                    remaining--;
                }

            roundDamaged = true;
            dirty = true;
            FractureCount++;
            Vector3 local = (Vector3)dimensions * cellSize * .5f;
            local[axis] = (best + .5f) * cellSize;
            LastFracturePoint = transform.TransformPoint(local);
            physicalGeneration++;
            ValidateSupport(physicalGeneration);
            FlushSurface();
            return cutCount;
        }

        static bool OnFractureCut(Vector3Int cell, int axis, int slice)
        {
            int u = cell[(axis + 1) % 3], v = cell[(axis + 2) % 3];
            return cell[axis] == slice + (((u * 17 + v * 31) % 5) == 0 ? 1 : 0);
        }

        public void ValidateSupport(int generation = -1)
        {
            if (generation >= 0)
                splitGeneration = generation;
            if (health == null || remaining == 0)
                return;
            EnsureSupportStorage();
            if (supportStamp == int.MaxValue)
            {
                System.Array.Clear(supportSeen, 0, supportSeen.Length);
                supportStamp = 0;
            }

            supportStamp++;
            supportGroups.Clear();
            int width = dimensions.x, height = dimensions.y, plane = width * height, tail = 0;
            for (int start = 0; start < health.Length; start++)
            {
                if (health[start] <= 0 || supportSeen[start] == supportStamp)
                    continue;
                int first = tail, head = tail;
                supportQueue[tail++] = start;
                supportSeen[start] = supportStamp;
                bool anchored = false;
                while (head < tail)
                {
                    int index = supportQueue[head++], z = index / plane, y = (index - z * plane) / width, x = index % width;
                    if (!anchored && !isFragment && y == 0)
                    {
                        Vector3 below = transform.TransformPoint(new Vector3(x + .5f, -.12f, z + .5f) * cellSize);
                        anchored = terrain || !requiresGroundSupport || LaserArena.GroundSupports(below);
                    }

                    if (x > 0)
                        VisitSupport(index - 1, ref tail);
                    if (x + 1 < width)
                        VisitSupport(index + 1, ref tail);
                    if (y > 0)
                        VisitSupport(index - width, ref tail);
                    if (y + 1 < height)
                        VisitSupport(index + width, ref tail);
                    if (z > 0)
                        VisitSupport(index - plane, ref tail);
                    if (z + 1 < dimensions.z)
                        VisitSupport(index + plane, ref tail);
                }

                supportGroups.Add(new SupportGroup { start = first, count = tail - first, anchored = anchored });
            }

            int largest = -1;
            if (isFragment)
                for (int i = 0; i < supportGroups.Count; i++)
                    if (largest < 0 || supportGroups[i].count > supportGroups[largest].count)
                        largest = i;
            for (int i = 0; i < supportGroups.Count; i++)
            {
                var group = supportGroups[i];
                if (isFragment ? i == largest : group.anchored)
                    continue;
                GetComponent<MeshCollider>().enabled = false;
                if (isFragment)
                    foreach (var box in compoundBoxes)
                        if (box != null)
                            box.enabled = false;
                SpawnFragment(supportQueue, group.start, group.count);
                for (int n = group.start; n < group.start + group.count; n++)
                {
                    health[supportQueue[n]] = 0;
                    remaining--;
                }

                dirty = true;
            }

            if (dirty)
                Rebuild();
            if (remaining == 0 && isFragment)
                Destroy(gameObject);
        }

        void VisitSupport(int index, ref int tail)
        {
            if (health[index] <= 0 || supportSeen[index] == supportStamp)
                return;
            supportSeen[index] = supportStamp;
            supportQueue[tail++] = index;
        }

        void EnsureSupportStorage()
        {
            if (supportSeen == null || supportSeen.Length != health.Length)
            {
                supportSeen = new int[health.Length];
                supportQueue = new int[health.Length];
                supportStamp = 0;
            }
        }

        Vector3Int CellPosition(int index)
        {
            int z = index / (dimensions.x * dimensions.y);
            return new Vector3Int(index % dimensions.x, (index - z * dimensions.x * dimensions.y) / dimensions.x, z);
        }

        void SpawnFragment(int[] cells, int start, int count)
        {
            Vector3Int min = CellPosition(cells[start]), max = min;
            for (int n = start + 1; n < start + count; n++)
            {
                var p = CellPosition(cells[n]);
                min = Vector3Int.Min(min, p);
                max = Vector3Int.Max(max, p);
            }

            var go = new GameObject(displayName + " — обломок");
            go.transform.SetPositionAndRotation(transform.TransformPoint((Vector3)min * cellSize), transform.rotation);
            go.transform.localScale = transform.lossyScale;
            var fragment = go.AddComponent<LaserVoxelBody>();
            fragment.dimensions = max - min + Vector3Int.one;
            fragment.cellSize = cellSize;
            fragment.hitPoints = hitPoints;
            fragment.bodyColor = bodyColor;
            fragment.trimColor = trimColor;
            fragment.accentColor = accentColor;
            fragment.displayName = displayName;
            fragment.isFragment = true;
            fragment.requiresGroundSupport = false;
            fragment.allowDestruction = true;
            fragment.collapseUnsupported = false;
            fragment.terrain = false;
            fragment.physicalGeneration = splitGeneration;
            fragment.surfaceColors = surfaceColors;
            fragment.singleMaterialSurface = singleMaterialSurface;
            // Keep the texture frame from before detachment, including subsequent cuts.
            fragment.studTextureAnchor = (hasStudTextureAnchor ? studTextureAnchor
                : transform.localToWorldMatrix) * Matrix4x4.Translate((Vector3)min * cellSize);
            fragment.hasStudTextureAnchor = true;
            int length = fragment.dimensions.x * fragment.dimensions.y * fragment.dimensions.z;
            fragment.health = new float[length];
            fragment.original = new byte[length];
            fragment.palette = new byte[length];
            fragment.Total = fragment.remaining = count;
            for (int n = start; n < start + count; n++)
            {
                int a = cells[n];
                var q = CellPosition(a) - min;
                int b = fragment.Index(q.x, q.y, q.z);
                fragment.health[b] = health[a];
                fragment.original[b] = 1;
                fragment.palette[b] = palette[a];
            }

            fragment.GetComponent<MeshRenderer>().sharedMaterials = GetComponent<MeshRenderer>().sharedMaterials;
            fragment.EnsureSupportStorage();
            fragment.Rebuild();
            var rb = go.AddComponent<Rigidbody>();
            rb.mass = fragment.PhysicsMass;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            rb.angularDamping = .12f;
            var parentBody = GetComponent<Rigidbody>();
            if (parentBody != null)
            {
                rb.linearVelocity = parentBody.GetPointVelocity(go.transform.position);
                rb.angularVelocity = parentBody.angularVelocity;
            }
            else
                rb.AddTorque(new Vector3(.3f, .03f, .15f) * rb.mass, ForceMode.Impulse);
            go.AddComponent<LaserFallingImpact>().voxelCount = count;
        }

        Color CellColor(int x, int y, int z)
        {
            int i = Index(x, y, z);
            int id = palette != null ? palette[i] : 0;
            Color c = surfaceColors != null && id < surfaceColors.Length ? surfaceColors[id] : id == 2
                ? accentColor : id == 1 ? trimColor : bodyColor;
            return c * (.87f + ((x * 13 + y * 7 + z * 3) % 7) * .025f);
        }

        void LateUpdate()
        {
            if (dirty)
                Rebuild();
            if (autoRespawn && emptyAt >= 0 && Time.time - emptyAt > respawnDelay)
                ResetBody();
            if (isFragment && transform.position.y < -40)
                Destroy(gameObject);
        }

        public bool NeedsRoundPreparation => health == null || roundDamaged || remaining != Total || dirty
            || mesh == null || GetComponent<MeshFilter>().sharedMesh != mesh;

        public void FlushSurface()
        {
            if (dirty)
                Rebuild();
        }

        public void PrepareForRound()
        {
            if (NeedsRoundPreparation)
                ResetBody();
            else
                BindCollision(); // Intact voxel data does not imply that the native physics shape survived.
        }

        public void ResetBody()
        {
            if (original == null || health == null || health.Length != dimensions.x * dimensions.y * dimensions.z)
            {
                Generate();
                return;
            }

            for (int i = 0; i < health.Length; i++)
                health[i] = original[i] * hitPoints;
            remaining = Total;
            emptyAt = -1;
            roundDamaged = false;
            LastHitAt = -100;
            Rebuild();
            if (terrain)
                LaserArena.InvalidateGroundIndex();
        }

        void BindCollision()
        {
            var collider = GetComponent<MeshCollider>();
            collider.enabled = false;
            collider.sharedMesh = null;
            if (isFragment)
            {
                RebuildCompound();
                return;
            }

            collider.isTrigger = false;
            collider.convex = false;
            collider.cookingOptions = MeshColliderCookingOptions.UseFastMidphase
                | MeshColliderCookingOptions.CookForFasterSimulation | MeshColliderCookingOptions.EnableMeshCleaning
                | MeshColliderCookingOptions.WeldColocatedVertices;
            if (mesh != null && mesh.vertexCount > 0)
            {
                collider.sharedMesh = mesh;
                collider.enabled = true;
            }
        }

        public void Rebuild()
        {
            if (health == null)
                return;
            int slots = Mathf.Max(Mathf.Max(3, surfaceColors != null ? surfaceColors.Length
                : 0), GetComponent<MeshRenderer>().sharedMaterials.Length);
            meshVertices.Clear();
            meshNormals.Clear();
            meshColors.Clear();
            if (meshTriangles == null || meshTriangles.Length != slots)
            {
                meshTriangles = new List<int>[slots];
                for (int i = 0; i < slots; i++)
                    meshTriangles[i] = new List<int>();
            }

            foreach (var list in meshTriangles)
                list.Clear();
            int maskSize = Mathf.Max(dimensions.x * dimensions.y, Mathf.Max(dimensions.y * dimensions.z,
                dimensions.x * dimensions.z));
            if (greedyMask == null || greedyMask.Length < maskSize)
                greedyMask = new int[maskSize];
            // Merge visible coplanar cells of one material; studs are supplied by the existing shader.
            for (int axis = 0; axis < 3; axis++)
            {
                int uAxis = (axis + 1) % 3, vAxis = (axis + 2) % 3, w = dimensions[uAxis], h =
                    dimensions[vAxis], depth = dimensions[axis];
                int stride = axis == 0 ? 1 : axis == 1 ? dimensions.x : dimensions.x * dimensions.y;
                Vector3 normal = Vector3.zero;
                normal[axis] = 1;
                Vector3 alongU = Vector3.zero;
                alongU[uAxis] = cellSize;
                Vector3 alongV = Vector3.zero;
                alongV[vAxis] = cellSize;
                for (int sign = -1; sign <= 1; sign += 2)
                    for (int slice = 0; slice < depth; slice++)
                    {
                        bool neighbourInside = slice + sign >= 0 && slice + sign < depth;
                        for (int v = 0; v < h; v++)
                            for (int u = 0; u < w; u++)
                            {
                                int x = axis == 0 ? slice : axis == 1 ? v
                                    : u, y = axis == 0 ? u : axis == 1 ? slice : v, z = axis == 0 ? v : axis == 1 ? u : slice;
                                int index = Index(x, y, z);
                                greedyMask[u + v * w] = health[index] > 0
                                    && (!neighbourInside || health[index + stride * sign] <= 0)
                                    ? Mathf.Min(palette[index], slots - 1) + 1 : 0;
                            }

                        for (int v = 0; v < h; v++)
                            for (int u = 0; u < w;)
                            {
                                int id = greedyMask[u + v * w];
                                if (id == 0)
                                {
                                    u++;
                                    continue;
                                }

                                int rw = 1;
                                while (u + rw < w && greedyMask[u + rw + v * w] == id)
                                    rw++;
                                int rh = 1;
                                bool full = true;
                                while (v + rh < h && full)
                                {
                                    for (int n = 0; n < rw; n++)
                                        if (greedyMask[u + n + (v + rh) * w] != id)
                                        {
                                            full = false;
                                            break;
                                        }

                                    if (full)
                                        rh++;
                                }

                                for (int yy = 0; yy < rh; yy++)
                                    for (int xx = 0; xx < rw; xx++)
                                        greedyMask[u + xx + (v + yy) * w] = 0;
                                Vector3 p = Vector3.zero;
                                p[axis] = (slice + (sign > 0 ? 1 : 0)) * cellSize;
                                p[uAxis] = u * cellSize;
                                p[vAxis] = v * cellSize;
                                Vector3 a = alongU * rw, b = alongV * rh;
                                int first = meshVertices.Count;
                                if (sign > 0)
                                {
                                    meshVertices.Add(p);
                                    meshVertices.Add(p + a);
                                    meshVertices.Add(p + a + b);
                                    meshVertices.Add(p + b);
                                }
                                else
                                {
                                    meshVertices.Add(p + b);
                                    meshVertices.Add(p + a + b);
                                    meshVertices.Add(p + a);
                                    meshVertices.Add(p);
                                }

                                int cx = axis == 0 ? slice : axis == 1 ? v
                                    : u, cy = axis == 0 ? u : axis == 1 ? slice : v, cz = axis == 0 ? v : axis == 1 ? u : slice;
                                Color colour = CellColor(cx, cy, cz);
                                if (singleMaterialSurface)
                                {
                                    int paletteId = id - 1;
                                    colour = surfaceColors != null
                                        && paletteId < surfaceColors.Length ? surfaceColors[paletteId] : paletteId == 2
                                        ? accentColor : paletteId == 1 ? trimColor : bodyColor;
                                }

                                for (int n = 0; n < 4; n++)
                                {
                                    meshNormals.Add(normal * sign);
                                    meshColors.Add(colour);
                                }

                                var tris = meshTriangles[singleMaterialSurface ? 0 : id - 1];
                                tris.Add(first);
                                tris.Add(first + 1);
                                tris.Add(first + 2);
                                tris.Add(first);
                                tris.Add(first + 2);
                                tris.Add(first + 3);
                                u += rw;
                            }
                    }
            }

            // Detach the native collision shape before mutating its mesh, including when the arena is inactive.
            var collider = GetComponent<MeshCollider>();
            collider.enabled = false;
            collider.sharedMesh = null;
            if (mesh == null)
            {
                mesh = new Mesh
                {
                    name = "Greedy destructible voxel surface"
                };
                mesh.indexFormat = IndexFormat.UInt32;
                mesh.MarkDynamic();
            }

            mesh.Clear();
            mesh.SetVertices(meshVertices);
            mesh.SetNormals(meshNormals);
            mesh.SetColors(meshColors);
            mesh.subMeshCount = singleMaterialSurface ? 1 : slots;
            for (int i = 0; i < mesh.subMeshCount; i++)
                mesh.SetTriangles(meshTriangles[i], i, false);
            mesh.RecalculateBounds();
            GetComponent<MeshFilter>().sharedMesh = mesh;
            BindCollision();
            dirty = false;
            if (isFragment && singleMaterialSurface)
            {
                if (!hasStudTextureAnchor)
                {
                    studTextureAnchor = transform.localToWorldMatrix;
                    hasStudTextureAnchor = true;
                }

                if (fragmentProperties == null)
                    fragmentProperties = new MaterialPropertyBlock();
                var renderer = GetComponent<MeshRenderer>();
                renderer.GetPropertyBlock(fragmentProperties);
                fragmentProperties.SetFloat("_UseStudAnchor", 1);
                fragmentProperties.SetMatrix("_StudAnchor", studTextureAnchor);
                fragmentProperties.SetMatrix("_StudAnchorInverse", studTextureAnchor.inverse);
                renderer.SetPropertyBlock(fragmentProperties);
            }

            SurfaceChanged?.Invoke(this);
        }

        void RebuildCompound()
        {
            if (compoundUsed == null || compoundUsed.Length != health.Length)
                compoundUsed = new bool[health.Length];
            else
                System.Array.Clear(compoundUsed, 0, compoundUsed.Length);
            var used = compoundUsed;
            int boxes = 0;
            for (int z = 0; z < dimensions.z; z++)
                for (int y = 0; y < dimensions.y; y++)
                    for (int x = 0; x < dimensions.x; x++)
                    {
                        if (!Solid(x, y, z) || used[Index(x, y, z)])
                            continue;
                        int ex = x;
                        while (ex + 1 < dimensions.x && Solid(ex + 1, y, z) && !used[Index(ex + 1, y, z)])
                            ex++;
                        int ez = z;
                        bool extend = true;
                        while (extend && ez + 1 < dimensions.z)
                        {
                            for (int xx = x; xx <= ex; xx++)
                                if (!Solid(xx, y, ez + 1) || used[Index(xx, y, ez + 1)])
                                {
                                    extend = false;
                                    break;
                                }

                            if (extend)
                                ez++;
                        }

                        int ey = y;
                        extend = true;
                        while (extend && ey + 1 < dimensions.y)
                        {
                            for (int zz = z; zz <= ez && extend; zz++)
                                for (int xx = x; xx <= ex; xx++)
                                    if (!Solid(xx, ey + 1, zz) || used[Index(xx, ey + 1, zz)])
                                    {
                                        extend = false;
                                        break;
                                    }

                            if (extend)
                                ey++;
                        }

                        for (int zz = z; zz <= ez; zz++)
                            for (int yy = y; yy <= ey; yy++)
                                for (int xx = x; xx <= ex; xx++)
                                    used[Index(xx, yy, zz)] = true;
                        BoxCollider box;
                        if (boxes < compoundBoxes.Count)
                            box = compoundBoxes[boxes];
                        else
                        {
                            box = gameObject.AddComponent<BoxCollider>();
                            compoundBoxes.Add(box);
                        }

                        box.enabled = true;
                        box.center = new Vector3((x + ex + 1) * .5f, (y + ey + 1) * .5f, (z + ez + 1) * .5f) * cellSize;
                        box.size = new Vector3(ex - x + 1, ey - y + 1, ez - z + 1) * cellSize;
                        boxes++;
                    }

            for (int i = boxes; i < compoundBoxes.Count; i++)
                if (compoundBoxes[i] != null)
                    compoundBoxes[i].enabled = false;
            var rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.mass = PhysicsMass;
                rb.ResetCenterOfMass();
                rb.ResetInertiaTensor();
            }

            var impact = GetComponent<LaserFallingImpact>();
            if (impact != null)
                impact.voxelCount = remaining;
        }

        void OnDestroy()
        {
            if (mesh != null)
                Destroy(mesh);
        }
    }

    public class LaserDebris : MonoBehaviour
    {
        static readonly List<LaserDebris> Pool = new List<LaserDebris>();
        static Material material;
        Rigidbody body;
        Renderer visual;
        float expires;
        public static void Emit(Vector3 position, Color color, float size, Vector3 direction)
        {
            Pool.RemoveAll(p => p == null);
            LaserDebris piece = Pool.Find(p => !p.gameObject.activeSelf);
            if (piece == null && Pool.Count < 48)
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                go.name = "Voxel debris";
                go.layer = 2;
                piece = go.AddComponent<LaserDebris>();
                piece.body = go.AddComponent<Rigidbody>();
                piece.visual = go.GetComponent<Renderer>();
                if (material == null)
                    material = LaserImpactFeedback.Instance != null
                        && LaserImpactFeedback.Instance.debrisMaterial != null
                        ? LaserImpactFeedback.Instance.debrisMaterial : new Material(Shader.Find("Universal Render Pipeline/Simple Lit"));
                piece.visual.sharedMaterial = material;
                Pool.Add(piece);
            }

            if (piece == null)
                return;
            piece.gameObject.SetActive(true);
            piece.transform.position = position;
            piece.transform.localScale = Vector3.one * size * Random.Range(.4f, .8f);
            piece.transform.rotation = Random.rotation;
            var props = new MaterialPropertyBlock();
            props.SetColor("_BaseColor", color);
            props.SetColor("_Color", color);
            props.SetVector("_ObjectScale", new Vector4(size, size, size, 1));
            props.SetFloat("_IsDynamic", 1);
            piece.visual.SetPropertyBlock(props);
            piece.body.linearVelocity = direction * Random.Range(3f,
                6f) + Random.insideUnitSphere * 2 + Vector3.up * Random.Range(1.5f, 4f);
            piece.body.angularVelocity = Random.insideUnitSphere * 10;
            piece.expires = Time.time + Random.Range(1.2f, 1.8f);
        }

        void Update()
        {
            if (Time.time > expires)
                gameObject.SetActive(false);
        }
    }
}
