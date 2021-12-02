using System;
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
        public event Action<string> LogMessage;

        private readonly ProtocolTimeout _delayIfNoRockMineable = new ProtocolTimeout();
        private readonly GameClient _client;

        private List<MiningObject> Rocks => _client.MiningObjects;
        private readonly List<MiningObject> _minedRocks = new List<MiningObject>();

        private static readonly Random Random = new Random();

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
                if (rock is null || _minedRocks.Count <= 0) return;
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
        public bool IsRockMineAble(MiningObject rock) => !_minedRocks.Contains(rock) && IsRockMineAbleAt(rock.X, rock.Y);
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
                    var coloredRocks = Rocks.FindAll(r => IsRockMineAble(r)).ToList();
                    if (coloredRocks.Count > 0)
                    {
                        _lastRock = FindBestRock(coloredRocks);
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
                    Rocks.FindAll(r => colors.Contains(r.Color)).ForEach(r =>
                        Console.WriteLine($"Is Mineable {r.Color}({r.X}, {r.Y}): {IsRockMineAble(r)}"));
                    Rocks.ForEach(r =>
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
                var tempRocks = Rocks.FindAll(rock => IsRockMineAble(rock));
                if (tempRocks.Count > 0)
                {
                    _lastRock = FindBestRock(tempRocks);
                    MineRock(_lastRock, axe);
                    return true;
                }
                _minedRocks.Clear();
            }
            return false;
        }

        public MiningObject FindBestRock(List<MiningObject> rocks = null)
        {
            var closets_rocks = FindClosestRocks(rocks).FindAll(rock => rock.Priority().RequiredLevel() <= _client.Mining.MiningLevel).ToList();

            var best_priority = closets_rocks.Max(rock => rock.Priority());
            closets_rocks.RemoveAll(rock => rock.Priority() != best_priority);

            var best_distance = closets_rocks.Min(rock => _client.DistanceTo(rock.X, rock.Y));
            closets_rocks.RemoveAll(rock => _client.DistanceTo(rock.X, rock.Y) != best_distance);

            return closets_rocks[Random.Next(0, closets_rocks.Count - 1)];
        }


        public List<MiningObject> FindClosestRocks(List<MiningObject> rocks = null)
        {
            if (rocks is null)
                rocks = Rocks;
            if (rocks.Count == 1)
                return rocks;
            if (_lastRock != null)
            {
                rocks.ToList().RemoveAll(r => r.X == _lastRock.X && r.Y == _lastRock.Y && _lastRock.Color == r.Color); //removing last rock.
            }

            var closets_rocks = rocks.OrderBy(rock => rock.Priority()).Reverse().ToList(); /* reverse coz we want the big boiz first */
            closets_rocks.Sort((lhs, rhs) => _client.DistanceTo(lhs.X, lhs.Y).CompareTo(_client.DistanceTo(rhs.X, rhs.Y))); /* and we want the closest boiz first */

            closets_rocks.RemoveAll(rock => !IsRockMineAble(rock));

            return closets_rocks;
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
