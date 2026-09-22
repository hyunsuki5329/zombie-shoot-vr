namespace VRFPS
{
    public sealed class MagazineState
    {
        readonly int capacity;
        public int Rounds { get; private set; }
        public bool Inserted { get; private set; }
        public MagazineState(int capacity)
        {
            if (capacity <= 0) throw new System.ArgumentOutOfRangeException(nameof(capacity));
            this.capacity = capacity;
            Rounds = capacity;
            Inserted = true;
        }

        public int Eject()
        {
            var discarded = Rounds;
            Rounds = 0;
            Inserted = false;
            return discarded;
        }

        // Physical token ownership and Fresh state are validated by the socket before this call.
        public bool InsertFresh()
        {
            if (Inserted) return false;
            Inserted = true;
            Rounds = capacity;
            return true;
        }

        public bool TryConsume()
        {
            if (!Inserted || Rounds <= 0) return false;
            Rounds--;
            return true;
        }
    }
}
