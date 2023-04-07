using System;

namespace DELTation.MaliOfflineCompiler.Editor.Metrics
{
    [Serializable]
    public struct PixelShaderMetrics
    {
        public PixelShaderVariantMetrics Main;
        public bool HasUniformComputation;
        public bool HasSideEffects;
        public bool ModifiesCoverage;
        public bool UsesLateZsTest;
        public bool UsesLateZsUpdate;
        public bool ReadsColorBuffer;
    }
}