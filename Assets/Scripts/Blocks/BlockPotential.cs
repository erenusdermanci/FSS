namespace Blocks
{
    public class BlockPotential
    {
        public readonly int Type;
        public readonly float Probability;

        public BlockPotential(int type, float probability)
        {
            Type = type;
            Probability = probability;
        }
    }
}