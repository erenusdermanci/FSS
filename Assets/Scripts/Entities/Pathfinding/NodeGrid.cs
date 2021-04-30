using System.Collections.Generic;
using Tiles;
using Utils;

namespace Entities.Pathfinding
{
    public class NodeGrid
    {
        private const int NodesPerChunkDimension = 16;
        private const int SizeX = Tile.HorizontalChunkCount * NodesPerChunkDimension;
        private const int SizeY = Tile.VerticalChunkCount * NodesPerChunkDimension;
        private readonly Node[,] _grid;

        public NodeGrid()
        {
            _grid = new Node[SizeX,SizeY];

            for (var i = 0; i < SizeX; ++i)
                for (var j = 0; j < SizeY; ++j)
                    _grid[i, j] = new Node(i, j);
        }

        public void SetWeight(int x, int y, int weight)
        {
            _grid[x, y].Weight = weight;
        }

        public List<Node> A_Star(Node start, Node goal)
        {
            // The set of discovered nodes that may need to be (re-)expanded.
            // Initially, only the start node is known.
            var openSet = new PriorityQueue<Node>();
            openSet.Enqueue(start);

            // For node n, cameFrom[n] is the node immediately preceding it on the cheapest path from start
            // to n currently known.
            var cameFrom = new Dictionary<Node, Node>();

            // For node n, gScore[n] is the cost of the cheapest path from start to n currently known.
            var gScore = new Dictionary<Node, int> {[start] = 0};

            // For node n, gScore[n] is the cost of the cheapest path from start to n currently known.
            var fScore = new Dictionary<Node, int> {[start] = start.Weight};

            while (openSet.Count() > 0)
            {
                var current = openSet.Dequeue();
                if (current == goal)
                    return ReconstructPath(cameFrom, current);

                foreach (var neighbor in GetNeighbors(current))
                {
                    // tentative_gScore is the distance from start to the neighbor through current
                    var tentativeGScore = gScore[current] + neighbor.Weight;
                    if (tentativeGScore < gScore[neighbor])
                    {
                        // This path to neighbor is better than any previous one. Record it!
                        cameFrom[neighbor] = current;
                        gScore[neighbor] = tentativeGScore;
                        fScore[neighbor] = gScore[neighbor] + neighbor.Weight;

                        if (!openSet.Contains(neighbor))
                            openSet.Enqueue(neighbor);
                    }
                }
            }

            return null;
        }

        private List<Node> ReconstructPath(Dictionary<Node, Node> cameFrom, Node current)
        {
            var totalPath = new List<Node> { current };
            while (cameFrom.ContainsKey(current))
            {
                current = cameFrom[current];
                totalPath.Add(current);
            }

            totalPath.Reverse();
            return totalPath;
        }

        private List<Node> GetNeighbors(Node central)
        {
            var neighbors = new List<Node>();

            for (var y = central.Y - 1; y < central.Y + 2; y++)
            {
                for (var x = central.X - 1; x < central.X + 2; x++)
                {
                    if (y == 0 && x == 0)
                        continue;

                    neighbors.Add(_grid[x, y]);
                }
            }

            return neighbors;
        }
    }
}
