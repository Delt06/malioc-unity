using System;

[Serializable]
public struct CompilerShaderStage
{
    public CompiledShaderStageType StageType;
    public string[] Code;
    public VertexShaderMetrics VertexShaderMetrics;
    public PixelShaderMetrics PixelShaderMetrics;
}