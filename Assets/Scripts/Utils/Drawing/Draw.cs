using System;
using System.Collections.Generic;

namespace Utils.Drawing
{
    public static class Draw
    {
        public static void Rectangle(int x, int y, int w, int h, Action<int, int> draw)
        {
            var px = x - w / 2;
            var py = y - h / 2;

            for (var i = px; i < px + w; i++)
            {
                for (var j = py; j < py + h; j++)
                {
                    draw(i, j);
                }
            }
        }

        public static void Circle(int x, int y, int radius, Action<int, int> draw)
        {
            for (var i = -radius; i <= radius; ++i)
            {
                for (var j = -radius; j <= radius; ++j)
                {
                    if (j * j + i * i <= radius * radius)
                    {
                        draw(x + i, j + y);
                    }
                }
            }
        }

        public static void Line(int x1, int y1, int x2, int y2, Action<int, int> draw)
        {
            var w = x2 - x1;
            var h = y2 - y1;
            var dx1 = 0;
            var dy1 = 0;
            var dx2 = 0;
            var dy2 = 0;
            if (w < 0) dx1 = -1;
            else if (w > 0) dx1 = 1;
            if (h < 0) dy1 = -1;
            else if (h > 0) dy1 = 1;
            if (w < 0) dx2 = -1;
            else if (w > 0) dx2 = 1;
            var longest = Math.Abs(w);
            var shortest = Math.Abs(h);
            if (!(longest > shortest))
            {
                longest = Math.Abs(h);
                shortest = Math.Abs(w);
                if (h < 0) dy2 = -1;
                else if (h > 0) dy2 = 1;
                dx2 = 0;
            }

            var numerator = longest >> 1;
            for (var i = 0; i <= longest; i++)
            {
                draw(x1, y1);
                numerator += shortest;
                if (!(numerator < longest))
                {
                    numerator -= longest;
                    x1 += dx1;
                    y1 += dy1;
                }
                else
                {
                    x1 += dx2;
                    y1 += dy2;
                }
            }
        }

        public static void Fill(int sx, int sy, Func<int, int, int> get, Action<int, int> draw)
        {
            var positionQueue = new Queue<Vector2i>();
            var processing = new HashSet<Vector2i>();
            var first = new Vector2i(sx, sy);
            positionQueue.Enqueue(first);
            var blockUnderCursor = get(sx, sy);
            while (positionQueue.Count != 0)
            {
                var pos = positionQueue.Dequeue();

                var x = pos.x;
                var y = pos.y;
                draw(x, y);

                var right = new Vector2i(x + 1, y);
                var left = new Vector2i(x - 1, y);
                var up = new Vector2i(x, y + 1);
                var down = new Vector2i(x, y - 1);
                if (get(right.x, right.y) == blockUnderCursor && !processing.Contains(right))
                {
                    positionQueue.Enqueue(right);
                    processing.Add(right);
                }
                if (get(left.x, left.y) == blockUnderCursor && !processing.Contains(left))
                {
                    positionQueue.Enqueue(left);
                    processing.Add(left);
                }
                if (get(up.x, up.y) == blockUnderCursor && !processing.Contains(up))
                {
                    positionQueue.Enqueue(up);
                    processing.Add(up);
                }
                if (get(down.x, down.y) == blockUnderCursor && !processing.Contains(down))
                {
                    positionQueue.Enqueue(down);
                    processing.Add(down);
                }
            }
        }
    }
}