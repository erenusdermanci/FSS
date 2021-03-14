namespace Blocks.Behaviors
{
    public readonly struct FireSpread : IBehavior
    {
        public const int Id = 1;

        public int GetId => Id;

        public readonly float BurningRate;
        public readonly int CombustionEmissionBlockType;
        public readonly float CombustionEmissionProbability;
        public readonly int CombustionResultBlockType;
        public readonly float CombustionResultProbability;

        public FireSpread(float burningRate,
            int combustionEmissionBlockType,
            float combustionEmissionProbability,
            int combustionResultBlockType,
            float combustionResultProbability)
        {
            BurningRate = burningRate;
            CombustionEmissionBlockType = combustionEmissionBlockType;
            CombustionEmissionProbability = combustionEmissionProbability;
            CombustionResultBlockType = combustionResultBlockType;
            CombustionResultProbability = combustionResultProbability;
        }
    }
}