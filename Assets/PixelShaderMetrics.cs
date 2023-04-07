using System;

[Serializable]
public struct PixelShaderMetrics
{
    public PixelShaderVariantMetrics Variant;
    public bool HasUniformComputation;
    public bool HasSideEffects;
    public bool ModifiesCoverage;
    public bool UsesLateZsTest;
    public bool UsesLateZsUpdate;
    public bool ReadsColorBuffer;
}