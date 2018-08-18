namespace PPOProtocol
{
    public enum Direction
    {
        Up,
        Down,
        Left,
        Right
    }
    public static class DirectionExtensions
    {
        public static string AsChar(this Direction direction)
        {
            switch (direction)
            {
                case Direction.Up:
                    return "u";
                case Direction.Down:
                    return "d";
                case Direction.Left:
                    return "l";
                case Direction.Right:
                    return "r";
            }
            return null;
        }

        public static string AsString(this Direction direction)
        {
            switch (direction)
            {
                case Direction.Up:
                    return "up";
                case Direction.Down:
                    return "down";
                case Direction.Left:
                    return "left";
                case Direction.Right:
                    return "right";
            }
            return null;
        }

        public static Direction GetOpposite(this Direction direction)
        {
            switch (direction)
            {
                case Direction.Up:
                    return Direction.Down;
                case Direction.Down:
                    return Direction.Up;
                case Direction.Left:
                    return Direction.Right;
                case Direction.Right:
                default:
                    return Direction.Left;
            }
        }

        public static void ApplyToCoordinates(this Direction direction, ref int x, ref int y)
        {
            switch (direction)
            {
                case Direction.Up:
                    y--;
                    break;
                case Direction.Down:
                    y++;
                    break;
                case Direction.Left:
                    x--;
                    break;
                case Direction.Right:
                    x++;
                    break;
            }
        }

        public static Direction FromChar(char c)
        {
            switch (c)
            {
                case 'u':
                    return Direction.Up;
                case 'd':
                    return Direction.Down;
                case 'l':
                    return Direction.Left;
                case 'r':
                    return Direction.Right;
            }
            throw new System.Exception("The direction '" + c + "' does not exist");
        }

        /// <summary>
        /// Converts an integer into an actual direction. Needed for NPC view directions, but could also be used elsewhere.
        /// </summary>
        /// <param name="direction">Integer representing an npc's view direction.</param>
        /// <returns>The converted direction.</returns>
        public static Direction FromInt(int direction)
        {
            switch (direction)
            {
                case 0:
                    return Direction.Up;
                case 1:
                    return Direction.Right;
                case 2:
                    return Direction.Down;
                case 3:
                    return Direction.Left;
            }
            throw new System.Exception("The direction '" + direction + "' does not exist");
        }
    }
}
