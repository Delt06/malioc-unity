using System;

namespace DELTation.MaliOfflineCompiler.Editor.Metrics
{
    [Serializable]
    public struct VertexShaderMetrics
    {
        public VertexShaderVariantMetrics Main;
        public VertexShaderVariantMetrics PositionVariant;
        public VertexShaderVariantMetrics VaryingVariant;
        public bool HasUniformComputation;
    }
}