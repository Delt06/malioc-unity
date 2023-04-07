using System;

[Serializable]
public struct VertexShaderMetrics
{
    public VertexShaderVariantMetrics PositionVariant;
    public VertexShaderVariantMetrics VaryingVariant;
    public bool HasUniformComputation;
}