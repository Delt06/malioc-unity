using System;
using System.Globalization;
using System.Text.RegularExpressions;

public class MetricsParser
{
    private static readonly Regex WorkRegistersRegex = new(@"\AWork registers: ([0-9]+)\z");
    private static readonly Regex UniformRegistersRegex = new(@"\AUniform registers: ([0-9]+)\z");
    private static readonly Regex StackSpillingRegex = new(@"\AStack spilling registers: (false|true)\z");
    private static readonly Regex Arithmetic16BitRegex = new(@"\A16-bit arithmetic: ([0-9]+)%\z");

    private static readonly Regex VertexInstructionCyclesRegex =
        new(
            @"\A(?<Cycle>.*) cycles:\s+(?<A>[0-9]\.[0-9][0-9])\s+(?<LS>[0-9]\.[0-9][0-9])\s+(?<T>[0-9]\.[0-9][0-9])\s+(?<Bound>[A-Z]+)\z"
        );
    private static readonly Regex PixelInstructionCyclesRegex =
        new(
            @"\A(?<Cycle>.*) cycles:\s+(?<A>[0-9]\.[0-9][0-9])\s+(?<LS>[0-9]\.[0-9][0-9])\s+(?<V>[0-9]\.[0-9][0-9])\s+(?<T>[0-9]\.[0-9][0-9])\s+(?<Bound>[A-Z]+)\z"
        );

    private static readonly Regex HasUniformComputationRegex = new(@"\AHas uniform computation: (false|true)\z");
    private static readonly Regex HasSideEffectsRegex = new(@"\AHas side-effects: (false|true)\z");
    private static readonly Regex ModifiesCoverageRegex = new(@"\AModifies coverage: (false|true)\z");
    private static readonly Regex UsesLateZsTestRegex = new(@"\AUses late ZS test: (false|true)\z");
    private static readonly Regex UsesLateZsUpdateRegex = new(@"\AUses late ZS update: (false|true)\z");
    private static readonly Regex ReadsColorBufferRegex = new(@"\AReads color buffer: (false|true)\z");

    public static VertexShaderMetrics ParseVertexShaderMetrics(string[] lines)
    {
        VertexShaderMetricsSection metricsSection = VertexShaderMetricsSection.None;
        var metrics = new VertexShaderMetrics();

        foreach (string line in lines)
        {
            metricsSection = line switch
            {
                "Position variant" => VertexShaderMetricsSection.PositionVariant,
                "Varying variant" => VertexShaderMetricsSection.VaryingVariant,
                "Shader properties" => VertexShaderMetricsSection.ShaderProperties,
                _ => metricsSection,
            };

            switch (metricsSection)
            {
                case VertexShaderMetricsSection.None:
                    break;
                case VertexShaderMetricsSection.PositionVariant:
                case VertexShaderMetricsSection.VaryingVariant:
                {
                    // read
                    VertexShaderVariantMetrics variant = metricsSection == VertexShaderMetricsSection.PositionVariant
                        ? metrics.PositionVariant
                        : metrics.VaryingVariant;

                    // update
                    if (TryParseInt(line, WorkRegistersRegex, out int workRegisters))
                    {
                        variant.WorkRegisters = workRegisters;
                    }
                    else if (TryParseInt(line, UniformRegistersRegex, out int uniformRegisters))
                    {
                        variant.UniformRegisters = uniformRegisters;
                    }
                    else if (TryParseBool(line, StackSpillingRegex, out bool stackSpilling))
                    {
                        variant.StackSpilling = stackSpilling;
                    }
                    else if (TryParseInt(line, Arithmetic16BitRegex, out int arithmetic16Bit))
                    {
                        variant.Arithmetic16Bit = arithmetic16Bit;
                    }

                    {
                        Match match = VertexInstructionCyclesRegex.Match(line);
                        if (match.Success)
                        {
                            var cycles = new VertexShaderVariantMetrics.Cycles
                            {
                                Arithmetic = ParseFloat(match.Groups["A"].Value),
                                LoadStore = ParseFloat(match.Groups["LS"].Value),
                                Texture = ParseFloat(match.Groups["T"].Value),
                                Bound = Enum.Parse<InstructionCycleType>(match.Groups["Bound"].Value),
                            };

                            switch (match.Groups["Cycle"].Value)
                            {
                                case "Total instruction":
                                    variant.TotalCycles = cycles;
                                    break;
                                case "Shortest path":
                                    variant.ShortestCycles = cycles;
                                    break;
                                case "Longest path":
                                    variant.LongestCycles = cycles;
                                    break;
                                default:
                                    throw new ArgumentOutOfRangeException();
                            }
                        }
                    }


                    // write back
                    if (metricsSection == VertexShaderMetricsSection.PositionVariant)
                    {
                        metrics.PositionVariant = variant;
                    }
                    else
                    {
                        metrics.VaryingVariant = variant;
                    }

                    break;
                }

                case VertexShaderMetricsSection.ShaderProperties:
                {
                    if (TryParseBool(line, HasUniformComputationRegex, out bool hasUniformComputation))
                    {
                        metrics.HasUniformComputation = hasUniformComputation;
                    }

                    break;
                }

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        return metrics;
    }

    public static PixelShaderMetrics ParsePixelShaderMetrics(string[] lines)
    {
        var metrics = new PixelShaderMetrics();
        bool shaderPropertiesStarted = false;

        foreach (string line in lines)
        {
            if (shaderPropertiesStarted)
            {
                if (TryParseBool(line, HasUniformComputationRegex, out bool hasUniformComputation))
                {
                    metrics.HasUniformComputation = hasUniformComputation;
                }

                if (TryParseBool(line, HasSideEffectsRegex, out bool hasSideEffects))
                {
                    metrics.HasSideEffects = hasSideEffects;
                }

                if (TryParseBool(line, ModifiesCoverageRegex, out bool modifiesCoverage))
                {
                    metrics.ModifiesCoverage = modifiesCoverage;
                }

                if (TryParseBool(line, UsesLateZsTestRegex, out bool usesLateZsTest))
                {
                    metrics.UsesLateZsTest = usesLateZsTest;
                }

                if (TryParseBool(line, UsesLateZsUpdateRegex, out bool usesLateZsUpdate))
                {
                    metrics.UsesLateZsUpdate = usesLateZsUpdate;
                }

                if (TryParseBool(line, ReadsColorBufferRegex, out bool readsColorBuffer))
                {
                    metrics.ReadsColorBuffer = readsColorBuffer;
                }

                break;
            }

            if (line == "Shader properties")
            {
                shaderPropertiesStarted = true;
            }
            else
            {
                ref PixelShaderVariantMetrics variant = ref metrics.Variant;

                if (TryParseInt(line, WorkRegistersRegex, out int workRegisters))
                {
                    variant.WorkRegisters = workRegisters;
                }
                else if (TryParseInt(line, UniformRegistersRegex, out int uniformRegisters))
                {
                    variant.UniformRegisters = uniformRegisters;
                }
                else if (TryParseBool(line, StackSpillingRegex, out bool stackSpilling))
                {
                    variant.StackSpilling = stackSpilling;
                }
                else if (TryParseInt(line, Arithmetic16BitRegex, out int arithmetic16Bit))
                {
                    variant.Arithmetic16Bit = arithmetic16Bit;
                }

                {
                    Match match = PixelInstructionCyclesRegex.Match(line);
                    if (match.Success)
                    {
                        var cycles = new PixelShaderVariantMetrics.Cycles
                        {
                            Arithmetic = ParseFloat(match.Groups["A"].Value),
                            LoadStore = ParseFloat(match.Groups["LS"].Value),
                            Varying = ParseFloat(match.Groups["V"].Value),
                            Texture = ParseFloat(match.Groups["T"].Value),
                            Bound = Enum.Parse<InstructionCycleType>(match.Groups["Bound"].Value),
                        };

                        switch (match.Groups["Cycle"].Value)
                        {
                            case "Total instruction":
                                variant.TotalCycles = cycles;
                                break;
                            case "Shortest path":
                                variant.ShortestCycles = cycles;
                                break;
                            case "Longest path":
                                variant.LongestCycles = cycles;
                                break;
                            default:
                                throw new ArgumentOutOfRangeException();
                        }
                    }
                }
            }
        }

        return metrics;
    }

    private static float ParseFloat(string text) =>
        float.Parse(text, NumberStyles.Float,
            NumberFormatInfo.InvariantInfo
        );

    private static bool TryParseInt(string line, Regex regex, out int value)
    {
        Match match = regex.Match(line);
        if (!match.Success)
        {
            value = 0;
            return false;
        }

        string groupValue = match.Groups[1].Value;
        return int.TryParse(groupValue, out value);
    }

    private static bool TryParseBool(string line, Regex regex, out bool value)
    {
        Match match = regex.Match(line);
        if (!match.Success)
        {
            value = false;
            return false;
        }

        string groupValue = match.Groups[1].Value;
        return bool.TryParse(groupValue, out value);
    }

    private enum VertexShaderMetricsSection
    {
        None,
        PositionVariant,
        VaryingVariant,
        ShaderProperties,
    }
}