using System;
using DELTation.MaliOfflineCompiler.Editor.Metrics;

namespace DELTation.MaliOfflineCompiler.Editor.Compilation
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