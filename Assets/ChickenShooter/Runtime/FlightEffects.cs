using UnityEngine;

namespace OrbitFlock
{
    internal sealed class FlightEffects : MonoBehaviour
    {
        private const int RingSegments = 64;
        private readonly Vector3[] ringPoints = new Vector3[RingSegments];
        private CombatWorld world;
        private StageController stage;
        private Transform ship;
        private Transform view;
        private Vector3 restingCameraPosition;
        private ParticleSystem particles;
        private LineRenderer shockwave;
        private Material material;
        private Mesh distantStars, nearStars;
        private Transform distantLayer, nearLayer;
        private Vector2 pulseOrigin;
        private float pulseAge = 1f;
        private float exhaustAt;
        private float shake;
        private Color accent;

        internal void Initialize(CombatWorld simulation, Transform player, int sector)
        {
            world = simulation;
            stage = GetComponent<StageController>();
            ship = player;
            accent = sector == 1 ? FlightUi.Mint : sector == 2 ? FlightUi.Amber : FlightUi.Rose;
            view = Camera.main.transform;
            restingCameraPosition = view.localPosition;
            material = new Material(Shader.Find("Sprites/Default"));
            material.mainTexture = Texture2D.whiteTexture;
            particles = CreateParticles();
            shockwave = CreateShockwave();
            distantLayer = CreateStars("Distant stars", 72, 0.012f, -8, sector * 31, out distantStars);
            nearLayer = CreateStars("Near stars", 36, 0.023f, -7, sector * 67, out nearStars);
            world.EnemyDestroyed += Burst;
            world.MissileLaunched += BeginShockwave;
        }

        private void LateUpdate()
        {
            if (world == null || Time.deltaTime == 0f)
                return;

            distantLayer.localPosition = Vector3.down * Mathf.Repeat(Time.time * 0.17f, 10f);
            nearLayer.localPosition = Vector3.down * Mathf.Repeat(Time.time * 0.45f, 10f);
            if (stage.CanMove && Time.time >= exhaustAt)
            {
                exhaustAt = Time.time + 0.035f;
                Emit((Vector2)ship.position + Vector2.down * 0.44f, new Vector2(0f, -2.1f),
                    accent, 0.12f, 0.27f);
            }

            if (shockwave.enabled)
                UpdateShockwave();

            if (shake > 0f)
            {
                shake = Mathf.MoveTowards(shake, 0f, Time.deltaTime * 0.56f);
                float t = Time.time * 59f;
                view.localPosition = restingCameraPosition + new Vector3(Mathf.Sin(t), Mathf.Cos(t * 0.81f)) * shake;
            }
        }

        private ParticleSystem CreateParticles()
        {
            var emitter = new GameObject("Flight particles");
            emitter.transform.SetParent(transform, false);
            ParticleSystem system = emitter.AddComponent<ParticleSystem>();
            system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ParticleSystem.MainModule main = system.main;
            main.loop = true;
            main.maxParticles = 384;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.gravityModifier = 0.07f;
            ParticleSystem.EmissionModule emission = system.emission;
            emission.enabled = false;
            ParticleSystem.ShapeModule shape = system.shape;
            shape.enabled = false;
            ParticleSystem.ColorOverLifetimeModule fade = system.colorOverLifetime;
            fade.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
            fade.color = gradient;
            ParticleSystemRenderer renderer = emitter.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = material;
            renderer.sortingOrder = 5;
            system.Play();
            return system;
        }

        private LineRenderer CreateShockwave()
        {
            var effect = new GameObject("Missile shockwave");
            effect.transform.SetParent(transform, false);
            LineRenderer ring = effect.AddComponent<LineRenderer>();
            ring.sharedMaterial = material;
            ring.positionCount = RingSegments;
            ring.sortingOrder = 6;
            ring.loop = true;
            ring.enabled = false;
            return ring;
        }

        private Transform CreateStars(string name, int count, float size, int order, int seed, out Mesh mesh)
        {
            var layer = new GameObject(name);
            layer.transform.SetParent(transform, false);
            var random = new System.Random(seed);
            var vertices = new Vector3[count * 8];
            var colors = new Color32[vertices.Length];
            var indices = new int[count * 12];

            for (int index = 0; index < count; ++index)
            {
                float x = (float)random.NextDouble() * 17f - 8.5f;
                float y = (float)random.NextDouble() * 10f - 5f;
                float radius = size * (1f + (float)random.NextDouble());
                var shade = (Color32)Color.Lerp(new Color(0.52f, 0.65f, 0.81f, 0.38f), accent, order == -7 ? 0.31f : 0.12f);
                for (int copy = 0; copy < 2; ++copy)
                {
                    int offset = (index * 2 + copy) * 4;
                    float centerY = y + copy * 10f;
                    vertices[offset] = new Vector3(x - radius, centerY - radius);
                    vertices[offset + 1] = new Vector3(x - radius, centerY + radius);
                    vertices[offset + 2] = new Vector3(x + radius, centerY + radius);
                    vertices[offset + 3] = new Vector3(x + radius, centerY - radius);
                    for (int vertex = 0; vertex < 4; ++vertex)
                        colors[offset + vertex] = shade;
                    int first = (index * 2 + copy) * 6;
                    indices[first] = indices[first + 3] = offset;
                    indices[first + 1] = offset + 1;
                    indices[first + 2] = indices[first + 4] = offset + 2;
                    indices[first + 5] = offset + 3;
                }
            }

            mesh = new Mesh { name = name, vertices = vertices, colors32 = colors, triangles = indices };
            layer.AddComponent<MeshFilter>().sharedMesh = mesh;
            MeshRenderer renderer = layer.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.sortingOrder = order;
            return layer.transform;
        }

        private void Burst(ActorKind kind, Vector2 position)
        {
            Color shade = kind == ActorKind.Egg ? new Color32(255, 230, 193, 255) : accent;
            int count = kind == ActorKind.Chicken ? 10 : 6;
            for (int index = 0; index < count; ++index)
            {
                float angle = index * (2f * Mathf.PI / count);
                var motion = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * (1.5f + (index % 3) * 0.45f);
                Emit(position, motion, shade, kind == ActorKind.Chicken ? 0.075f : 0.12f, 0.38f);
            }
            if (kind != ActorKind.Egg)
                shake = Mathf.Max(shake, 0.025f);
        }

        private void Emit(Vector2 position, Vector2 velocity, Color color, float size, float lifetime)
        {
            particles.Emit(new ParticleSystem.EmitParams
            {
                position = position,
                velocity = velocity,
                startColor = color,
                startSize = size,
                startLifetime = lifetime
            }, 1);
        }

        private void BeginShockwave(Vector2 origin)
        {
            pulseOrigin = origin;
            pulseAge = 0f;
            shockwave.enabled = true;
            shake = 0.14f;
        }

        private void UpdateShockwave()
        {
            pulseAge += Time.deltaTime;
            float progress = pulseAge / 0.46f;
            if (progress >= 1f)
            {
                shockwave.enabled = false;
                return;
            }

            float radius = Mathf.Lerp(0.35f, 15f, progress);
            for (int index = 0; index < ringPoints.Length; ++index)
            {
                float angle = index * (2f * Mathf.PI / RingSegments);
                ringPoints[index] = pulseOrigin + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
            }
            shockwave.SetPositions(ringPoints);
            shockwave.widthMultiplier = Mathf.Lerp(0.13f, 0.02f, progress);
            Color color = Color.Lerp(FlightUi.Paper, accent, progress);
            color.a = 1f - progress;
            shockwave.startColor = shockwave.endColor = color;
        }

        private void OnDestroy()
        {
            if (world != null)
            {
                world.EnemyDestroyed -= Burst;
                world.MissileLaunched -= BeginShockwave;
            }
            if (view != null)
                view.localPosition = restingCameraPosition;
            Destroy(distantStars);
            Destroy(nearStars);
            Destroy(material);
        }
    }
}
