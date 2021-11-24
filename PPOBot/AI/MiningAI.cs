﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PPOProtocol;

// ReSharper disable once CheckNamespace
namespace PPOBot
{
    // ReSharper disable once InconsistentNaming
    public class MiningAI
    {
        //wrost AI ever.... Don't laugh :D
        public static class RockPrority
        {
            public enum RockPriority
            {
                Gold = 9,
                Rainbow = 8,
                Dark = 7,
                Pale = 5,
                Prism = 4,
                Green = 3,
                Blue = 2,
                Red = 1,
                None = 0
            }

            public static RockPriority PriorityFromColor(string color)
            {
                if (!Enum.TryParse(color.Trim(), out RockPriority rock)) return RockPriority.None;
                return rock;
            }
#if DEBUG
            public static int CountPriorityPower(RockPriority pr)
            {
                return (int)pr;
            }
#endif
        }

        public event Action<string> LogMessage;

        private readonly ProtocolTimeout _delayIfNoRockMineable = new ProtocolTimeout();
        private readonly GameClient _client;

        private IList<MiningObject> Rocks => _client.MiningObjects;
        private readonly List<MiningObject> _minedRocks = new List<MiningObject>();

        public MiningAI(GameClient client)
        {
            _client = client;
            _client.RockDepleted += Game_RockDepleted;
            _client.RockRestored += Game_RockRestored;
            _client.MapUpdated += Game_MapUpdated;
        }

        private void Game_MapUpdated()
        {
            _minedRocks.Clear();
        }

        public bool Update()
        {
            return _delayIfNoRockMineable.Update();
        }

        private void Game_RockRestored(MiningObject rock)
        {
            try
            {
                if (rock is null) return;
                if (_minedRocks.Count <= 0)
                {
                    return;
                }
                _minedRocks.RemoveAll(r => r.X == rock.X && r.Y == rock.Y);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        private void Game_RockDepleted(MiningObject rock)
        {
            try
            {
                if (rock is null) return;
                if (!_minedRocks.Any(r => r.X == rock.X && r.Y == rock.Y))
                    _minedRocks.Add(rock);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        private MiningObject _lastRock;
        public bool IsAnyMineAbleRock() => _client.IsAnyMinableRocks();
        public bool IsRockMineAbleAt(int x, int y) => _client.IsMinable(x, y);
        public bool IsRockMineAble(MiningObject rock) => IsRockMineAbleAt(rock.X, rock.Y);
        public bool MineRockAt(int x, int y, string axe)
        {
            if (!IsRockMineAbleAt(x, y)) return false;
            _client.MineRock(x, y, axe);
            return true;
        }

        public bool IsColoredRocksMineable(string[] colors) =>
            Rocks.Any(r => colors.Contains(r.Color) && IsRockMineAble(r));

        public bool MineRock(MiningObject rock, string axe)
        {
            _minedRocks.Add(rock);
            return MineRockAt(rock.X, rock.Y, axe);
        }
        public bool MineMultipleColoredRocks(string axe, string[] colors, bool waitForRocks = false)
        {
            try
            {
                if (IsAnyMineAbleRock())
                {
                    var tempRocks = Rocks.ToList().FindAll(r => !_minedRocks.Contains(r));
                    var coloredRocks = new List<MiningObject>();
                    foreach (var rock in tempRocks)
                    {
                        if (rock.IsGoldMember == _client.IsGoldMember && IsRockMineAble(rock) &&
                            colors.ToList().Contains(rock.Color))
                        {
                            coloredRocks.Add(rock);
                        }
                        else if (IsRockMineAble(rock) && colors.ToList().Contains(rock.Color))
                        {
                            coloredRocks.Add(rock);
                        }
                    }

                    if (coloredRocks.Count > 0)
                    {
                        _lastRock = FindClosestRock(coloredRocks);
                        if (_lastRock != null)
                        {
                            MineRock(_lastRock, axe);
                            return true;
                        }
                    }

                    _minedRocks.Clear();

                    if (!IsColoredRocksMineable(colors))
                    {
                        int delay = 0;
                        if (colors.Contains("Red") || colors.Contains("Blue") || colors.Contains("Green"))
                        {
                            delay = 30000;
                        }
                        else if (colors.Contains("Prism") || colors.Contains("Pale"))
                        {
                            delay = 60000;
                        }
                        else if (colors.Contains("Dark") || colors.Contains("Rainbow"))
                        {
                            delay = 180000;
                        }

                        _delayIfNoRockMineable.Set(delay);
                        LogMessage?.Invoke($"There is no specific colored mine able rocks. Waiting for {TimeSpan.FromMilliseconds(delay).FormatTimeString()}");
                    }
#if DEBUG
                    Rocks.ToList().FindAll(r => colors.Contains(r.Color)).ForEach(r =>
                        Console.WriteLine($"Is Mineable {r.Color}({r.X}, {r.Y}): {IsRockMineAble(r)}"));
                    Rocks.ToList().ForEach(r =>
                        Console.WriteLine($"Is Mineable {r.Color}({r.X}, {r.Y}): {IsRockMineAble(r)}"));
#endif
                }
                if (_client.HasItemName(axe) && waitForRocks)
                    return true;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
            return false;
        }

        public bool MineAnyRock(string axe)
        {
            if (IsAnyMineAbleRock())
            {
                var tempRocks = new List<MiningObject>();
                foreach (var rock in Rocks)
                {
                    if (rock.IsGoldMember == _client.IsGoldMember && IsRockMineAble(rock))
                    {
                        tempRocks.Add(rock);
                    }
                    else if (IsRockMineAble(rock))
                    {
                        tempRocks.Add(rock);
                    }
                }
                tempRocks = tempRocks.FindAll(r => !_minedRocks.Contains(r));
                if (tempRocks.Count > 0)
                {
                    _lastRock = FindClosestRock(tempRocks);
                    MineRock(_lastRock, axe);
                    return true;
                }
                _minedRocks.Clear();
            }
            return false;
        }

        private class Node
        {
            public int Distance { get; set; }
            public RockPrority.RockPriority Priority { get; set; }
        }
        public MiningObject FindClosestRock(IList<MiningObject> rocks = null)
        {
            var nodes = new Dictionary<MiningObject, Node>();
            if (rocks is null)
                rocks = Rocks;
            if (rocks.Count == 1)
                return rocks.FirstOrDefault();
            if (_lastRock != null)
            {
                rocks.ToList().RemoveAll(r => r.X == _lastRock.X && r.Y == _lastRock.Y && _lastRock.Color == r.Color); //removing last rock.
            }
            foreach (var rock in rocks)
            {
                if (_minedRocks.Contains(rock))
                    continue;

                if (rock.IsMined) continue;

                var distance = GameClient.DistanceBetween(_client.PlayerX, _client.PlayerY, rock.X, rock.Y);
                var node = new Node
                {
                    Distance = distance,
                    Priority = RockPrority.PriorityFromColor(rock.Color)
                };

                if (distance == 0) return rock;
                if (!nodes.ContainsKey(rock))
                    nodes.Add(rock, node);
            }
            // ReSharper disable once InvertIf
            if (nodes.Count > 0)
            {
                var bestNodes = FindBestNodes(nodes);
#if DEBUG
                foreach (var n in nodes)
                {
                    Console.WriteLine(
                        $"X:{n.Key.X}-Y:{n.Key.Y}-Distance:{n.Value.Distance}\tColor:{n.Key.Color}\tPrority-Power:{RockPrority.CountPriorityPower(n.Value.Priority)}");
                    //bestNodes = FindBestNodes(nodes);
                }
#endif
                if (bestNodes.Count > 0)
                {
                    bestNodes = FindBestNodes(nodes);
#if DEBUG
                    Console.WriteLine("Best Nodes:");
                    foreach (var n in bestNodes)
                    {
                        Console.WriteLine(
                            $"X:{n.Key.X}-Y:{n.Key.Y}-Distance:{n.Value.Distance}\tColor:{n.Key.Color}\tPrority-Power:{RockPrority.CountPriorityPower(n.Value.Priority)}");
                    }
#endif
                    rocks = bestNodes.Keys.ToList();
                    foreach (var n in bestNodes)
                    {
                        if (IsNodeClosest(n.Value, rocks))
                            return n.Key;
                        //_nodes.Remove(n.Key);
                    }
                }
            }
            return null;
        }

        private Dictionary<MiningObject, Node> FindBestNodes(Dictionary<MiningObject, Node> nodes)
        {
            var newNodes = new Dictionary<MiningObject, Node>();
            foreach (var node in nodes)
            {
                if (nodes.Values.Any(n => n.Priority > node.Value.Priority))
                {
                    // ReSharper disable once RedundantJumpStatement
                    continue;
                }
                newNodes.Add(node.Key, node.Value);
            }

            return newNodes;
        }
        private bool IsNodeClosest(Node node, IList<MiningObject> rocks)
        {
            if (rocks.Any(r =>
                GameClient.DistanceBetween(_client.PlayerX, _client.PlayerY, r.X, r.Y) < node.Distance))
            {
                return false;
            }

            return true;
        }
    }

    public static class TimeSpanExtention
    {
        public static string FormatTimeString(this TimeSpan obj)
        {
            var sb = new StringBuilder();
            if (obj.Hours != 0)
            {
                sb.Append(obj.Hours);
                sb.Append(" ");
                sb.Append("hours");
                sb.Append(" ");
            }

            else if (obj.Minutes != 0 || sb.Length != 0)
            {
                sb.Append(obj.Minutes);
                sb.Append(" ");
                sb.Append("minutes");
                sb.Append(" ");
            }
            else if (obj.Seconds != 0 || sb.Length != 0)
            {
                sb.Append(obj.Seconds);
                sb.Append(" ");
                sb.Append("seconds");
                sb.Append(" ");
            }
            else if (obj.Milliseconds != 0 || sb.Length != 0)
            {
                sb.Append(obj.Milliseconds);
                sb.Append(" ");
                sb.Append("Milliseconds");
                sb.Append(" ");
            }
            else if (sb.Length == 0)
            {
                sb.Append(0);
                sb.Append(" ");
                sb.Append("Milliseconds");
            }
            return sb.ToString();
        }
    }
}
