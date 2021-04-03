using System.Collections.Generic;
using UnityEngine;

namespace Utils
{
    public static class DebugDraw
    {
        public static void Rectangle(float x, float y, float w, float h, UnityEngine.Color color)
        {
            Debug.DrawLine(new Vector2(x, y),
                            new Vector2(x + w, y), color);
            Debug.DrawLine(new Vector2(x + w, y),
                            new Vector2(x + w, y + h), color);
            Debug.DrawLine(new Vector2(x, y),
                            new Vector2(x, y + h), color);
            Debug.DrawLine(new Vector2(x, y + h),
                            new Vector2(x + w, y + h), color);
        }

        public static void Circle(float cx, float cy, float radius)
        {
            Ellipse(cx, cy, radius, radius);
        }

        public static void Ellipse(float cx, float cy, float xRadius, float yRadius)
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
                Debug.DrawLine(previousPoint, points[i], UnityEngine.Color.red);
                previousPoint = points[i];
            }
        }
    }
}