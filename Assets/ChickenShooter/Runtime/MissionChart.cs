using UnityEngine;
using UnityEngine.UI;

namespace OrbitFlock
{
    internal sealed class MissionChart : MaskableGraphic
    {
        internal const int ChartWidth = 748;
        internal const int ChartHeight = 560;
        private const int ArcSteps = 48;
        private static readonly Color Grid = new Color32(38, 49, 63, 255);
        private static readonly Color Orbit = new Color32(64, 91, 106, 255);
        private static readonly Vector2[] Sectors = { new Vector2(136, 345), new Vector2(374, 266), new Vector2(614, 174) };
        internal static Vector2 Sector(int index) => Sectors[index];

        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            for (int x = 22; x < ChartWidth; x += 26)
                Stroke(mesh, new Vector2(x, 68), new Vector2(x, ChartHeight - 22), 1f, Grid);
            for (int y = 68; y < ChartHeight; y += 26)
                Stroke(mesh, new Vector2(22, y), new Vector2(ChartWidth - 22, y), 1f, Grid);

            var random = new System.Random(40);
            for (int i = 0; i < 88; ++i)
            {
                var star = new Vector2(random.Next(30, 719), random.Next(80, 507));
                float size = i % 11 == 0 ? 1.7f : 0.8f;
                Color shade = i % 11 == 0 ? FlightUi.Muted : Orbit;
                Stroke(mesh, star - Vector2.right * size, star + Vector2.right * size, size, shade);
                if (size > 1f)
                    Stroke(mesh, star - Vector2.up * 3f, star + Vector2.up * 3f, 1f, shade);
            }

            DrawRoute(mesh, Sectors[0], Sectors[1], -45f);
            DrawRoute(mesh, Sectors[1], Sectors[2], 68f);
            DrawEarth(mesh, Sectors[0]);
            DrawJupiter(mesh, Sectors[1]);
            DrawGalaxy(mesh, Sectors[2]);
        }

        private static void DrawRoute(VertexHelper mesh, Vector2 start, Vector2 end, float bend)
        {
            Vector2 normal = Vector2.Perpendicular((end - start).normalized);
            Vector2 Point(float t) => Vector2.Lerp(start, end, t) + normal * (Mathf.Sin(t * Mathf.PI) * bend);
            for (int step = 0; step < 27; ++step)
            {
                float t = step / 27f;
                Stroke(mesh, Point(t), Point(t + 0.020f), 1.65f, FlightUi.Amber);
            }
        }

        private static void DrawEarth(VertexHelper mesh, Vector2 center)
        {
            Arc(mesh, center, new Vector2(72, 71), 1.2f, -0.27f, Orbit);
            Arc(mesh, center, new Vector2(87, 28), 1f, -0.41f, Orbit);
            Disk(mesh, center, new Vector2(49, 49), new Color32(77, 173, 189, 255));
            Disk(mesh, center + new Vector2(-3, -4), new Vector2(44, 44), new Color32(50, 126, 149, 255));
            Shape(mesh, center, FlightUi.Mint, new Vector2(-41,-9), new Vector2(-36,-27), new Vector2(-17,-40),
                new Vector2(-5,-37), new Vector2(-10,-24), new Vector2(-25,-19), new Vector2(-27,-6), new Vector2(-20,2),
                new Vector2(-34,3));
            Shape(mesh, center, new Color32(119, 207, 164, 255), new Vector2(-21,4), new Vector2(1,7), new Vector2(9,19),
                new Vector2(-1,29), new Vector2(-3,40), new Vector2(-13,35), new Vector2(-11,23), new Vector2(-24,15));
            Shape(mesh, center, FlightUi.Mint, new Vector2(11,-28), new Vector2(29,-30), new Vector2(40,-18),
                new Vector2(40,-5), new Vector2(30,-10), new Vector2(20,-3), new Vector2(9,-11));
            Stroke(mesh, center + new Vector2(-29, -15), center + new Vector2(-16, -20), 2, new Color32(219, 238, 208, 210));
            Stroke(mesh, center + new Vector2(6, 23), center + new Vector2(26, 19), 2, new Color32(219, 238, 208, 210));
        }

        private static void DrawJupiter(VertexHelper mesh, Vector2 center)
        {
            Arc(mesh, center, new Vector2(91, 36), 1.2f, -0.32f, Orbit);
            Arc(mesh, center, new Vector2(94, 66), 1f, -0.30f, Orbit);
            Disk(mesh, center, new Vector2(65, 65), new Color32(224, 158, 108, 255));
            Stripe(mesh, center, -39, -28, new Color32(248, 205, 146, 255));
            Stripe(mesh, center, -15, -3, new Color32(172, 104, 86, 255));
            Stripe(mesh, center, 4, 17, new Color32(247, 198, 140, 255));
            Stripe(mesh, center, 25, 34, new Color32(175, 102, 86, 255));
            Stripe(mesh, center, 42, 48, new Color32(241, 189, 134, 255));
            Disk(mesh, center + new Vector2(28, 21), new Vector2(16, 6), new Color32(172, 80, 72, 255));
            Arc(mesh, center, new Vector2(91, 31), 1.8f, -0.31f, FlightUi.Amber, 0f, Mathf.PI);
        }

        private static void Stripe(VertexHelper mesh, Vector2 center, float top, float bottom, Color color)
        {
            for (int i = 0; i < 4; ++i)
            {
                float y0 = Mathf.Lerp(top, bottom, i / 4f);
                float y1 = Mathf.Lerp(top, bottom, (i + 1) / 4f);
                float left = Mathf.Sqrt(65 * 65 - y0 * y0);
                float right = Mathf.Sqrt(65 * 65 - y1 * y1);
                Quad(mesh, center + new Vector2(-left, y0), center + new Vector2(left, y0),
                    center + new Vector2(right, y1), center + new Vector2(-right, y1), color);
            }
        }

        private static void DrawGalaxy(VertexHelper mesh, Vector2 center)
        {
            Arc(mesh, center, new Vector2(92, 67), 1f, -0.35f, Orbit);
            Disk(mesh, center, new Vector2(55, 46), new Color32(79, 56, 88, 200));
            for (int arm = 0; arm < 2; ++arm)
            {
                Vector2 previous = center;
                for (int point = 1; point <= 36; ++point)
                {
                    float t = point / 36f;
                    float angle = arm * Mathf.PI + t * 6.7f;
                    Vector2 next = center + new Vector2(Mathf.Cos(angle) * t * 75f, Mathf.Sin(angle) * t * 45f);
                    Color shade = Color.Lerp(FlightUi.Paper, new Color32(178, 105, 189, 100), t);
                    Stroke(mesh, previous, next, Mathf.Lerp(5.4f, 1f, t), shade);
                    previous = next;
                }
            }
            Disk(mesh, center, new Vector2(13, 8), FlightUi.Rose);
            Disk(mesh, center, new Vector2(6, 5), FlightUi.Paper);
            Stroke(mesh, center - Vector2.right * 26, center + Vector2.right * 26, 1f, FlightUi.Rose);
            Stroke(mesh, center - Vector2.up * 10, center + Vector2.up * 10, 1f, FlightUi.Rose);
        }

        private static void Shape(VertexHelper mesh, Vector2 center, Color shade, params Vector2[] border)
        {
            Vector2 middle = Vector2.zero;
            foreach (Vector2 point in border)
                middle += point;
            middle /= border.Length;
            for (int i = 0; i < border.Length; ++i)
                Triangle(mesh, center + middle, center + border[i], center + border[(i + 1) % border.Length], shade);
        }

        private static void Disk(VertexHelper mesh, Vector2 center, Vector2 radius, Color shade)
        {
            Vector2 previous = center + Vector2.right * radius.x;
            for (int i = 1; i <= ArcSteps; ++i)
            {
                float angle = i * 2 * Mathf.PI / ArcSteps;
                Vector2 next = center + new Vector2(Mathf.Cos(angle) * radius.x, Mathf.Sin(angle) * radius.y);
                Triangle(mesh, center, previous, next, shade);
                previous = next;
            }
        }

        private static void Arc(VertexHelper mesh, Vector2 center, Vector2 radius, float width, float rotation,
            Color shade, float from = 0, float to = 2 * Mathf.PI)
        {
            Vector2 Point(float angle)
            {
                var point = new Vector2(Mathf.Cos(angle) * radius.x, Mathf.Sin(angle) * radius.y);
                return center + new Vector2(point.x * Mathf.Cos(rotation) - point.y * Mathf.Sin(rotation),
                    point.x * Mathf.Sin(rotation) + point.y * Mathf.Cos(rotation));
            }

            Vector2 previous = Point(from);
            for (int i = 1; i <= ArcSteps; ++i)
            {
                Vector2 next = Point(Mathf.Lerp(from, to, i / (float)ArcSteps));
                Stroke(mesh, previous, next, width, shade);
                previous = next;
            }
        }

        private static void Stroke(VertexHelper mesh, Vector2 start, Vector2 end, float width, Color shade)
        {
            Vector2 normal = Vector2.Perpendicular((end - start).normalized) * (width * 0.5f);
            Quad(mesh, start - normal, start + normal, end + normal, end - normal, shade);
        }

        private static void Triangle(VertexHelper mesh, Vector2 a, Vector2 b, Vector2 c, Color shade)
        {
            int index = mesh.currentVertCount;
            Point(mesh, a, shade);
            Point(mesh, b, shade);
            Point(mesh, c, shade);
            mesh.AddTriangle(index, index + 1, index + 2);
        }

        private static void Quad(VertexHelper mesh, Vector2 a, Vector2 b, Vector2 c, Vector2 d, Color shade)
        {
            int index = mesh.currentVertCount;
            Point(mesh, a, shade);
            Point(mesh, b, shade);
            Point(mesh, c, shade);
            Point(mesh, d, shade);
            mesh.AddTriangle(index, index + 1, index + 2);
            mesh.AddTriangle(index, index + 2, index + 3);
        }

        private static void Point(VertexHelper mesh, Vector2 point, Color shade)
            => mesh.AddVert(new Vector2(point.x, -point.y), shade, Vector2.zero);
    }
}
