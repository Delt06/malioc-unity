using System;

[Serializable]
public struct CompiledShader
{
    public CompiledShaderVariant[] Variants;
    public bool IsValid;
}