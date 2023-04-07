using System;

namespace DELTation.MaliOfflineCompiler.Editor.Compilation
{
    [Serializable]
    public struct CompiledShaderVariant
    {
        public bool Expanded;
        public CompiledShaderHardwareTier HardwareTier;
        public string[] Keywords;
        public CompilerShaderStage[] Stages;
        public bool ComputedMetrics;
    }
}