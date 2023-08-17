using System;
using DELTation.MaliOfflineCompiler.Editor.Models;

namespace DELTation.MaliOfflineCompiler.Editor.Compilation
{
    [Serializable]
    public struct CompilerShaderStage
    {
        public CompiledShaderStageType StageType;
        public string[] Code;
        public RuntimeMaliocShader PixelShaderMetrics;
        public RuntimeMaliocShader VertexShaderMetrics;
    }
}