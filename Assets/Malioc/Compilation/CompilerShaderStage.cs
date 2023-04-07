using System;
using Malioc.Metrics;

namespace Malioc.Compilation
{
    [Serializable]
    public struct CompilerShaderStage
    {
        public CompiledShaderStageType StageType;
        public string[] Code;
        public VertexShaderMetrics VertexShaderMetrics;
        public PixelShaderMetrics PixelShaderMetrics;
    }
}