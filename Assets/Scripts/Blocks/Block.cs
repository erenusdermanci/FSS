namespace Blocks
{
    public struct Block
    {
        public int type;
        public int states;
        public float health;
        public float lifetime;
        public long entityId;

        public bool GetState(int stateToCheck)
        {
            return ((states >> stateToCheck) & 1) == 1;
        }

        public void SetState(int stateToSet)
        {
            states |= 1 << stateToSet;
        }

        public void ClearState(int stateToClear)
        {
            states &= ~(1 << stateToClear);
        }
    }
}