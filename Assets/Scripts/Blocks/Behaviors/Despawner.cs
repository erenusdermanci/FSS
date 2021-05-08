
namespace Blocks.Behaviors
{
    public class Despawner : IBehavior
    {
        private readonly float _despawnProbability;
        private readonly float _lifetime;
        private readonly BlockPotential _resultPotentialBlocks;

        public Despawner(float despawnProbability,
            float lifetime,
            BlockPotential resultPotentialBlocks)
        {
            _despawnProbability = despawnProbability;
            _lifetime = lifetime;
            _resultPotentialBlocks = resultPotentialBlocks;
        }
    }
}
