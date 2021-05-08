using Utils;

namespace Blocks.Behaviors
{
    public class FireSpreader : IBehavior
    {
        public readonly float CombustionProbability;
        public readonly Color FireColor;
        private readonly Color _burntColor;
        private readonly float _burningRate;
        private readonly BlockPotential[] _emissionPotentialBlocks;
        private readonly BlockPotential _combustionPotentialBlock;
        private readonly bool _selfExtinguishing;
        private readonly bool _destroyedWhenExtinguished;

        private static readonly float[] BurningRateMultipliers = {1f, 0.2f, 0.2f, 1f, 1f, 1f, 0.2f, 0.2f};

        public FireSpreader(float combustionProbability,
            Color fireColor,
            Color burntColor,
            float burningRate,
            BlockPotential[] emissionPotentialBlocks,
            BlockPotential combustionPotentialBlock,
            bool selfExtinguishing,
            bool destroyedWhenExtinguished)
        {
            CombustionProbability = combustionProbability;
            FireColor = fireColor;
            _burntColor = burntColor;
            _burningRate = burningRate;
            _emissionPotentialBlocks = emissionPotentialBlocks;
            _combustionPotentialBlock = combustionPotentialBlock;
            _selfExtinguishing = selfExtinguishing;
            _destroyedWhenExtinguished = destroyedWhenExtinguished;
        }
    }
}
