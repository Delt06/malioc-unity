﻿using System;

[Serializable]
public struct CompiledShaderVariant
{
    public bool Expanded;
    public CompiledShaderHardwareTier HardwareTier;
    public string[] Keywords;
    public CompilerShaderStage[] Stages;
    public bool ComputedMetrics;
}