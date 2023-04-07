using System;

namespace Malioc.Compilation
{
    [Serializable]
    public struct CompiledShader
    {
        public CompiledShaderVariant[] Variants;
        public bool IsValid;
    }
}