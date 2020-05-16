namespace PPOProtocol
{
    public static class Actions
    {
        public const uint SWAPPING_POKEMON  = 0b0000000001;
        public const uint USING_MOVE        = 0b0000000010;
        public const uint USING_ITEM        = 0b0000000100;
        public const uint USING_ON_POKEMON  = 0b0000001000;
        public const uint IN_BATTLE         = 0b0000010000;

        public const uint MOVING_UP         = 0b0000100000;
        public const uint MOVING_DOWN       = 0b0001000000;
        public const uint MOVING_LEFT       = 0b0010000000;
        public const uint MOVING_RIGHT      = 0b0100000000;
        public const uint ACTION_KEY        = 0b1000000000;
    }
}
