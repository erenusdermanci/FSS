namespace ProceduralGeneration
{
    public class ConfiguredNoise
    {
        private readonly FastNoiseLite _noise;
        private float _xOffset;
        private float _yOffset;

        public ConfiguredNoise()
        {
            _noise = new FastNoiseLite();
        }

        public void Configure(NoiseConfig config)
        {
            _noise.SetNoiseType(config.type);
            _noise.SetFractalOctaves(config.octaves);
            _noise.SetFractalGain(config.gain);
            _noise.SetFrequency(config.frequency);
            _noise.SetFractalType(config.fractalType);
            _xOffset = config.xOffset;
            _yOffset = config.yOffset;
        }

        public float GetNoise(float x, float y)
        {
            return _noise.GetNoise(x + _xOffset, y + _yOffset);
        }
    }
}