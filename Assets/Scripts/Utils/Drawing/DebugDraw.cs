using System.Collections.Generic;
using UnityEngine;

namespace Utils.Drawing
{
    public static class DebugDraw
    {
        public static void Square(float x, float y, float size, UnityEngine.Color color)
        {
            Rectangle(x, y, size, size, color);
        }

        public static void Rectangle(float x, float y, float width, float height, UnityEngine.Color color)
        {
            Debug.DrawLine(new Vector2(x, y),
                            new Vector2(x + width, y), color);
            Debug.DrawLine(new Vector2(x + width, y),
                            new Vector2(x + width, y + height), color);
            Debug.DrawLine(new Vector2(x, y),
                            new Vector2(x, y + height), color);
            Debug.DrawLine(new Vector2(x, y + height),
                            new Vector2(x + width, y + height), color);
        }

        public static void Circle(float cx, float cy, float radius, UnityEngine.Color color)
        {
            Ellipse(cx, cy, radius, radius, color);
        }

        public static void Ellipse(float cx, float cy, float xRadius, float yRadius, UnityEngine.Color color)
        {
            const int segments = 50;

            var angle = 20f;
            var points = new List<Vector2>();
            for (var i = 0; i < segments + 1; i++)
            {
                var x = Mathf.Sin(Mathf.Deg2Rad * angle) * xRadius;
                var y = Mathf.Cos(Mathf.Deg2Rad * angle) * yRadius;

                points.Add(new Vector2(cx + x, cy + y));

                angle += 360f / segments;
            }

            var previousPoint = points[0];
            for (var i = 1; i < points.Count; ++i)
            {
                Debug.DrawLine(previousPoint, points[i], color);
                previousPoint = points[i];
            }
        }
    }
}