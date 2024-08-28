using Microsoft.Xna.Framework;

namespace MichaelWeaponsMod.Content.MathStuff
{
    public class RectangleToTriangles
    {
        public Vector2 TopLeft { get; }
        public Vector2 TopRight { get; }
        public Vector2 BottomLeft { get; }
        public Vector2 BottomRight { get; }
        public Vector2 Center { get; }

        public RectangleToTriangles(Vector2 topLeft, int width, int height)
        {
            TopLeft = topLeft;
            TopRight = new Vector2(topLeft.X + width, topLeft.Y);
            BottomLeft = new Vector2(topLeft.X, topLeft.Y + height);
            BottomRight = new Vector2(topLeft.X + width, topLeft.Y + height);
            Center = new Vector2(topLeft.X + width / 2f, topLeft.Y + height / 2f);
        }

        public (Vector2, Vector2, Vector2)[] GetTriangles()
        {
            return new (Vector2, Vector2, Vector2)[]
            {
            (TopLeft, TopRight, Center),
            (TopRight, BottomRight, Center),
            (BottomRight, BottomLeft, Center),
            (BottomLeft, TopLeft, Center)
            };
        }

        public int? GetContainingTriangleIndex(Vector2 point)
        {
            var triangles = GetTriangles();

            for (int i = 0; i < triangles.Length; i++)
            {
                var (A, B, C) = triangles[i];

                if (IsPointInTriangle(point, A, B, C))
                {
                    return i; // Return the index of the triangle containing the point
                }
            }

            return null; // Point is not in any triangle
        }

        private bool IsPointInTriangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
        {
            // Compute vectors
            var v0 = c - a;
            var v1 = b - a;
            var v2 = p - a;

            // Compute dot products
            float dot00 = Vector2.Dot(v0, v0);
            float dot01 = Vector2.Dot(v0, v1);
            float dot02 = Vector2.Dot(v0, v2);
            float dot11 = Vector2.Dot(v1, v1);
            float dot12 = Vector2.Dot(v1, v2);

            // Compute barycentric coordinates
            float invDenom = 1 / (dot00 * dot11 - dot01 * dot01);
            float u = (dot11 * dot02 - dot01 * dot12) * invDenom;
            float v = (dot00 * dot12 - dot01 * dot02) * invDenom;

            // Check if point is in triangle
            return (u >= 0) && (v >= 0) && (u + v <= 1);
        }
    }
}