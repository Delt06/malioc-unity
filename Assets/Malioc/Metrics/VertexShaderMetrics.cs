using System;

namespace Malioc.Metrics
{
    [Serializable]
    public struct VertexShaderMetrics
    {
        public VertexShaderVariantMetrics PositionVariant;
        public VertexShaderVariantMetrics VaryingVariant;
        public bool HasUniformComputation;
    }
}