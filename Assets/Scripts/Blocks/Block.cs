namespace Blocks
{
    public struct Block
    {
        public int Lock;
        public int Type;
        public int States;
        public float Lifetime;

        public void Initialize(int blockType)
        {
            var blockDesc = BlockConstants.BlockDescriptors[blockType];

            Lock = 0;
            Type = blockType;
            States = blockDesc.InitialStates;
            Lifetime = 0;
        }

        public bool GetState(int stateToCheck)
        {
            return ((States >> stateToCheck) & 1) == 1;
        }

        public void SetState(int stateToSet)
        {
            States |= 1 << stateToSet;
        }

        public void ClearState(int stateToClear)
        {
            States &= ~(1 << stateToClear);
        }
    }
}
