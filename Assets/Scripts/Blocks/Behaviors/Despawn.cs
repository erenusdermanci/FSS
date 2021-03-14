namespace Blocks.Behaviors
{
    public class Despawn : IBehavior
    {
        public const int Id = 2;

        public int GetId => Id;

        public readonly float DespawnProbability;
        public readonly float Lifetime;
        public readonly int DespawnResultBlockType;

        public Despawn(float despawnProbability,
            float lifetime,
            int despawnResultBlockType)
        {
            DespawnProbability = despawnProbability;
            Lifetime = lifetime;
            DespawnResultBlockType = despawnResultBlockType;
        }
    }
}