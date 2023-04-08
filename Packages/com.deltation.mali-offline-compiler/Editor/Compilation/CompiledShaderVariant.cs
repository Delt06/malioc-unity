using System;

namespace DELTation.MaliOfflineCompiler.Editor.Compilation
{
    [Serializable]
    public struct CompiledShaderVariant
    {
        public CompiledShaderHardwareTier HardwareTier;
        public string[] Keywords;
        public CompilerShaderStage[] Stages;
        public bool ComputedMetrics;
    }
}