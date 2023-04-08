using System;

namespace DELTation.MaliOfflineCompiler.Editor.Compilation
{
    [Serializable]
    public struct CompiledShader
    {
        public CompiledShaderVariant[] Variants;
        public string Name;
        public bool IsValid;
    }
}