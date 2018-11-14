using PPOProtocol;
using System;
using System.Collections.Generic;

namespace PPOBot
{

    //PROShine helped me to create this class ( Well you know that I just copied and pasted it :D )
    public class Pathfinding
    {

        private static Direction[] _directions = new[] { Direction.Up, Direction.Down, Direction.Left, Direction.Right };

        class Node
        {
            public int X;
            public int Y;
            public int Distance;
            public int Score;
            public Node Parent;

            public Direction FromDirection;
            public int DirectionChangeCount;

            public uint Hash => (uint)X * 0x7FFFU + (uint)Y;

            public Node(int x, int y)
            {
                X = x;
                Y = y;
            }

            public Node(int x, int y, Direction direction)
            {
                X = x;
                Y = y;
                FromDirection = direction;
            }
        }

        private GameClient _client;

        public Pathfinding(GameClient client)
        {
            _client = client;
        }

        public bool MoveTo(int destinationX, int destinationY, string reason)
        {
            var node = FindPath(_client.PlayerX, _client.PlayerY, destinationX, destinationY);

            if (node != null)
            {
                Stack<Direction> directions = new Stack<Direction>();
                while (node.Parent != null)
                {
                    directions.Push(node.FromDirection);
                    node = node.Parent;
                }

#if DEBUG
                Console.WriteLine("Total Directions: " + directions.Count.ToString());
#endif

                while (directions.Count > 0)
                {
                    _client.Move(directions.Pop(), reason);
                }
                return true;
            }

            Console.WriteLine("NULL!!!");

            return true;
        }

        public bool MoveToSameCell(string reason)
        {
            foreach (Direction direction in _directions)
            {

                int destinationX = _client.PlayerX;
                int destinationY = _client.PlayerY;

                direction.ApplyToCoordinates(ref destinationX, ref destinationY);

                _client.Move(direction, reason);
                _client.Move(direction.GetOpposite(), reason);
            }
            return true;
        }

        private Node FindPath(int fromX, int fromY, int toX, int toY)
        {
            var openList = new Dictionary<uint, Node>();
            HashSet<uint> closedList = new HashSet<uint>();

            Node start = new Node(fromX, fromY);

            openList.Add(start.Hash, start);

            while (openList.Count > 0)
            {
                Node current = GetBestNode(openList.Values);
                int distance = GameClient.DistanceBetween(current.X, current.Y, toX, toY);
                if (distance == 0)
                {
                    return current;
                }

                openList.Remove(current.Hash);
                closedList.Add(current.Hash);

                var neighbors = GetNeighbors(current);
                foreach (var node in neighbors)
                {
                    if (closedList.Contains(node.Hash))
                        continue;

                    node.Parent = current;
                    node.Distance = current.Distance + 1;
                    node.Score = node.Distance;

                    node.DirectionChangeCount = current.DirectionChangeCount;
                    if (node.FromDirection != current.FromDirection)
                    {
                        node.DirectionChangeCount += 1;
                    }
                    node.Score += node.DirectionChangeCount / 4;
                    if (!openList.ContainsKey(node.Hash))
                    {
                        openList.Add(node.Hash, node);
                    }
                    else if (openList[node.Hash].Score > node.Score)
                    {
                        openList.Remove(node.Hash);
                        openList.Add(node.Hash, node);
                    }
                }
            }
            return null;
        }

        private List<Node> GetNeighbors(Node node)
        {
            List<Node> neighbors = new List<Node>();

            foreach (Direction direction in _directions)
            {
                int destinationX = node.X;
                int destinationY = node.Y;

                direction.ApplyToCoordinates(ref destinationX, ref destinationY);

                neighbors.Add(new Node(destinationX, destinationY, direction));
            }

            return neighbors;
        }

        private Node GetBestNode(IEnumerable<Node> nodes)
        {
            List<Node> bestNodes = new List<Node>();
            int bestScore = int.MaxValue;
            foreach (Node node in nodes)
            {
                if (node.Score < bestScore)
                {
                    bestNodes.Clear();
                    bestScore = node.Score;
                }
                if (node.Score == bestScore)
                {
                    bestNodes.Add(node);
                }
            }
            return bestNodes[_client.Rand.Next(bestNodes.Count)];
        }

    }
}
