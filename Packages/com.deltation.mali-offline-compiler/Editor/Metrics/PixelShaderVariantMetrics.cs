using System;

namespace DELTation.MaliOfflineCompiler.Editor.Metrics
{
    [Serializable]
    public struct PixelShaderVariantMetrics
    {
        public int WorkRegisters;
        public int UniformRegisters;
        public bool StackSpilling;
        public int Arithmetic16Bit;

        public Cycles TotalCycles;
        public Cycles ShortestCycles;
        public Cycles LongestCycles;

        [Serializable]
        public struct Cycles : IEquatable<Cycles>
        {
            public float Arithmetic;
            public float LoadStore;
            public float Varying;
            public float Texture;
            public InstructionCycleType Bound;

            public bool Equals(Cycles other) => Arithmetic.Equals(other.Arithmetic) && LoadStore.Equals(other.LoadStore) &&
                                                Varying.Equals(other.Varying) && Texture.Equals(other.Texture) &&
                                                Bound == other.Bound;

            public override bool Equals(object obj) => obj is Cycles other && Equals(other);

            public override int GetHashCode() => HashCode.Combine(Arithmetic, LoadStore, Varying, Texture, (int) Bound);
        }
    }
}