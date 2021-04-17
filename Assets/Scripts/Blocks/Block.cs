namespace Blocks
{
    public struct Block
    {
        public int type;
        public int states;
        public float health;
        public float lifetime;
        public long entityId;

        public void Initialize(int blockType)
        {
            var blockDesc = BlockConstants.BlockDescriptors[blockType];

            type = blockType;
            states = blockDesc.InitialStates;
            health = blockDesc.BaseHealth;
            lifetime = 0;
        }

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
