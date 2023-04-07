using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine.Assertions;

public class CompiledShaderParser
{
    public static CompiledShader Parse(string[] lines)
    {
        var compiledShader = new CompiledShader
        {
            Variants = FindVariants(lines).Select(v => ParseVariant(lines, v)).ToArray(),
            IsValid = true,
        };
        return compiledShader;
    }

    private static List<VariantTempData> FindVariants(string[] lines)
    {
        var variantsTempData = new List<VariantTempData>();
        var keywordsRegex = new Regex(@"\AKeywords: (<none>|.+)\z");

        for (int index = 0; index < lines.Length - 1; index++)
        {
            string line1 = lines[index];
            if (line1 != "//////////////////////////////////////////////////////")
            {
                continue;
            }

            string line2 = lines[index + 1];
            Match keywordsMatch = keywordsRegex.Match(line2);
            if (!keywordsMatch.Success)
            {
                continue;
            }

            var variantTempData = new VariantTempData
            {
                FromLineIndex = index,
            };

            string keywords = keywordsMatch.Groups[1].Value;
            variantTempData.Keywords = keywords == "<none>" ? Array.Empty<string>() : keywords.Split(' ');
            variantsTempData.Add(variantTempData);
        }

        for (int index = 0; index < variantsTempData.Count; index++)
        {
            VariantTempData variantTempData = variantsTempData[index];
            variantTempData.ToLineIndex = index == variantsTempData.Count - 1
                ? lines.Length
                : variantsTempData[index + 1].FromLineIndex;
            variantsTempData[index] = variantTempData;
        }

        return variantsTempData;
    }

    private static CompiledShaderVariant ParseVariant(string[] lines, in VariantTempData variantTempData)
    {
        var hardwareTierRegex = new Regex(@"\A-- Hardware tier variant: Tier ([0-9]+)\z");
        int hardwareTier = -1;
        var stageIfdefRegex = new Regex(@"\A#ifdef ([A-Z]+)\z");
        var stages = new List<CompilerShaderStage>();

        for (int lineIndex = variantTempData.FromLineIndex; lineIndex < variantTempData.ToLineIndex; lineIndex++)
        {
            string line = lines[lineIndex];

            {
                Match match = stageIfdefRegex.Match(line);
                if (match.Success)
                {
                    string stageName = match.Groups[1].Value;
                    CompiledShaderStageType stageType = stageName switch
                    {
                        "VERTEX" => CompiledShaderStageType.Vertex,
                        "FRAGMENT" => CompiledShaderStageType.Pixel,
                        _ => throw new ArgumentOutOfRangeException(nameof(stageName),
                            $"Found an invalid stage name {stageName} at line {lineIndex + 1}."
                        ),
                    };
                    var stage = new CompilerShaderStage
                    {
                        Code = ParseStageCode(lines, variantTempData, lineIndex),
                        StageType = stageType,
                    };
                    stages.Add(stage);

                    continue;
                }
            }

            {
                Match match = hardwareTierRegex.Match(line);
                if (match.Success)
                {
                    hardwareTier = int.Parse(match.Groups[1].Value);
                }
            }
        }

        Assert.IsTrue(hardwareTier >= 0);

        return new CompiledShaderVariant
        {
            Keywords = variantTempData.Keywords,
            Stages = stages.ToArray(),
            HardwareTier = (CompiledShaderHardwareTier) hardwareTier,
        };
    }

    private static string[] ParseStageCode(string[] lines, in VariantTempData variantTempData, int ifdefLineIndex)
    {
        var ifRegex = new Regex(@"\A\s*#if");
        var endifRegex = new Regex(@"\A\s*#endif");

        // #ifdef STAGE is already found
        int nesting = 1;

        for (int lineIndex = ifdefLineIndex + 1; lineIndex < variantTempData.ToLineIndex; lineIndex++)
        {
            string line = lines[lineIndex];
            if (ifRegex.IsMatch(line))
            {
                nesting++;
            }
            else if (endifRegex.IsMatch(line))
            {
                nesting--;
            }

            if (nesting == 0)
            {
                int fromLineIndex = ifdefLineIndex + 1;
                int stageCodeSize = lineIndex - fromLineIndex;
                string[] stageCode = new string[stageCodeSize];
                Array.Copy(lines, fromLineIndex, stageCode, 0, stageCodeSize);
                return stageCode;
            }
        }

        throw new ArgumentException($"Could not find #endif for #ifdef at line {ifdefLineIndex + 1}");
    }

    private struct VariantTempData
    {
        public int FromLineIndex;
        public int ToLineIndex;
        public string[] Keywords;
    }
}