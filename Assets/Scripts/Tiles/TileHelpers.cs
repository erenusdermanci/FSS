using System.Collections.Generic;
using Utils;

namespace Tiles
{
    public static class TileHelpers
    {
        public static readonly Dictionary<MovementDirection, TileStreamingInfo> TileStreamingInfosPerDirection =
            new Dictionary<MovementDirection, TileStreamingInfo>()
            {
                {
                    MovementDirection.East, new TileStreamingInfo
                    {
                        LoadOffsets = new List<Vector2i>()
                        {
                            new Vector2i(2, -1),
                            new Vector2i(2, 0),
                            new Vector2i(2, 1)
                        },
                        UnloadOffsets = new List<Vector2i>()
                        {
                            new Vector2i(-1, -1),
                            new Vector2i(-1, 0),
                            new Vector2i(-1, 1)
                        }
                    }
                },
                {
                    MovementDirection.West, new TileStreamingInfo
                    {
                        LoadOffsets = new List<Vector2i>()
                        {
                            new Vector2i(-2, -1),
                            new Vector2i(-2, 0),
                            new Vector2i(-2, 1)
                        },
                        UnloadOffsets = new List<Vector2i>()
                        {
                            new Vector2i(1, -1),
                            new Vector2i(1, 0),
                            new Vector2i(1, 1)
                        }
                    }
                },
                {
                    MovementDirection.North, new TileStreamingInfo
                    {
                        LoadOffsets = new List<Vector2i>()
                        {
                            new Vector2i(-1, 2),
                            new Vector2i(0, 2),
                            new Vector2i(1, 2)
                        },
                        UnloadOffsets = new List<Vector2i>()
                        {
                            new Vector2i(-1, -1),
                            new Vector2i(0, -1),
                            new Vector2i(1, -1)
                        }
                    }
                },
                {
                    MovementDirection.South, new TileStreamingInfo
                    {
                        LoadOffsets = new List<Vector2i>()
                        {
                            new Vector2i(-1, -2),
                            new Vector2i(0, -2),
                            new Vector2i(1, -2)
                        },
                        UnloadOffsets = new List<Vector2i>()
                        {
                            new Vector2i(-1, 1),
                            new Vector2i(0, 1),
                            new Vector2i(1, 1)
                        }
                    }
                }
            };

    public static MovementDirection DeduceMovementDirection(Vector2i oldPos, Vector2i newPos)
        {
            if (newPos.x > oldPos.x)
                return MovementDirection.East;

            if (newPos.x < oldPos.x)
                return MovementDirection.West;

            if (newPos.y > oldPos.y)
                return MovementDirection.North;

            if (newPos.y < oldPos.y)
                return MovementDirection.South;

            return MovementDirection.Error;
        }

        public struct TileStreamingInfo
        {
            public List<Vector2i> LoadOffsets;
            public List<Vector2i> UnloadOffsets;
        }

        public enum MovementDirection
        {
            East,
            West,
            North,
            South,
            Error
        }
    }
}
