namespace Utils
{
    public readonly struct Color
    {
        public readonly byte r, g, b, a;
        private readonly float _maxShift;

        public Color(byte r, byte g, byte b, byte a, float maxShift = 0.0f)
        {
            this.r = r;
            this.g = g;
            this.b = b;
            this.a = a;
            _maxShift = maxShift;
        }

        public void Shift(out byte r, out byte g, out byte b)
        {
            var shift = UnityHelpers.Helpers.GetRandomShiftAmount(_maxShift);
            r = UnityHelpers.Helpers.ShiftColorComponent(this.r, shift);
            g = UnityHelpers.Helpers.ShiftColorComponent(this.g, shift);
            b = UnityHelpers.Helpers.ShiftColorComponent(this.b, shift);
        }
    }
}
