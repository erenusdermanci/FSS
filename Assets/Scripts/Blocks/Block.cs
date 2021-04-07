namespace Blocks
{
    public struct Block
    {
        public int Type;
        public int StateBitset;
        public float Health;
        public float Lifetime;
        public long EntityId;

        public bool GetState(int stateToCheck)
        {
            return ((StateBitset >> stateToCheck) & 1) == 1;
        }

        public void SetState(int stateToSet)
        {
            StateBitset |= 1 << stateToSet;
        }

        public void ClearState(int stateToClear)
        {
            StateBitset &= ~(1 << stateToClear);
        }
    }
}