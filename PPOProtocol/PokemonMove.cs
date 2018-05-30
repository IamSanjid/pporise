using System.Globalization;

namespace PPOProtocol
{
    public class PokemonMove
    {
        public int Position { get; private set; }
        public int Id { get; private set; }
        public int MaxPoints { get; private set; }
        public int CurrentPoints { get; set; }

        private TextInfo ti = CultureInfo.CurrentCulture.TextInfo;

        public MovesManager.MoveData Data
        {
            get { return MovesManager.Instance.GetMoveData(Id); }
        }

        public string Name
        {
            get { return Data?.Name != null ? ti.ToTitleCase(Data?.Name) : Data?.Name; }
        }

        public string PP
        {
            get
            {
                return Name != null ? CurrentPoints + " / " + MaxPoints : "";
            }
        }

        public PokemonMove(int position, int id, int currentPoints, int maxPoints = -1)
        {
            Position = position;
            Id = id;
            if (maxPoints == -1)
                MaxPoints = currentPoints;
            else
                MaxPoints = maxPoints;
            CurrentPoints = currentPoints;
        }
    }
}
