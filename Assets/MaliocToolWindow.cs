﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;

public class MaliocToolWindow : EditorWindow
{
    private const int PlatformMask = 1 << (int) ShaderCompilerPlatform.GLES3x;

    private const string CyclesFloatFormat = "F2";

    [SerializeField]
    private Shader _shader;
    [SerializeField]
    private CompiledShaderHardwareTier _hardwareTier = CompiledShaderHardwareTier.Tier1;
    [SerializeField]
    private CompiledShader _compiledShader;
    [SerializeField]
    private Vector2 _scrollPosition = Vector2.zero;
    private GUIStyle _boxStyle;
    private GUIStyle _foldoutStyle;

    private void OnGUI()
    {
        _boxStyle = new GUIStyle(GUI.skin.box)
        {
            fontStyle = FontStyle.Bold,
            richText = true,
        };
        _foldoutStyle = new GUIStyle(EditorStyles.foldout)
        {
            fontStyle = FontStyle.Bold,
            richText = true,
        };
        _shader = (Shader) EditorGUILayout.ObjectField(new GUIContent("Shader"), _shader, typeof(Shader), false);

        if (GUILayout.Button("Analyze"))
        {
            OpenCompiledShader(_shader);
            AnalyzeShader(_shader);
        }

        if (!_compiledShader.IsValid || _compiledShader.Variants == null)
        {
            return;
        }

        if (_compiledShader.Variants.Length == 0)
        {
            GUILayout.Box("The compiled shader does not have any variants.");
            return;
        }

        _hardwareTier = (CompiledShaderHardwareTier) EditorGUILayout.EnumPopup(_hardwareTier);

        _scrollPosition = GUILayout.BeginScrollView(_scrollPosition, false, true);

        for (int index = 0; index < _compiledShader.Variants.Length; index++)
        {
            ref CompiledShaderVariant variant = ref _compiledShader.Variants[index];

            if (variant.HardwareTier != _hardwareTier)
            {
                continue;
            }

            string label = "Keywords: " +
                           (variant.Keywords.Length == 0 ? "<none>" : string.Join(' ', variant.Keywords));
            variant.Expanded = EditorGUILayout.Foldout(variant.Expanded, label, _foldoutStyle);

            if (variant.Expanded)
            {
                if (!variant.ComputedMetrics)
                {
                    variant.ComputedMetrics = true;
                    ComputeMetrics(ref _compiledShader.Variants[index]);
                }

                foreach (CompilerShaderStage stage in variant.Stages)
                {
                    DrawMetrics(stage);
                }
            }


            GUILayout.Space(8);
        }

        GUILayout.EndScrollView();
    }

    private void DrawMetrics(in CompilerShaderStage stage)
    {
        GUILayout.BeginHorizontal();
        GUILayout.Box(stage.StageType switch
            {
                CompiledShaderStageType.Vertex => "VS",
                CompiledShaderStageType.Pixel => "PS",
                _ => throw new ArgumentOutOfRangeException(),
            },
            _boxStyle
        );

        switch (stage.StageType)
        {
            case CompiledShaderStageType.Vertex:
                DrawVertexShaderMetrics(stage.VertexShaderMetrics);
                break;
            case CompiledShaderStageType.Pixel:
                DrawPixelShaderMetrics(stage.PixelShaderMetrics);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        GUILayout.EndHorizontal();
    }

    private void DrawVertexShaderMetrics(in VertexShaderMetrics metrics)
    {
        GUILayout.BeginHorizontal();
        DrawVertexShaderVariantMetrics("Position variant", metrics.PositionVariant);
        DrawVertexShaderVariantMetrics("Varying variant", metrics.VaryingVariant);
        GUILayout.FlexibleSpace();
        DrawVertexShaderProperties(metrics);
        GUILayout.EndHorizontal();
    }

    private void DrawPixelShaderMetrics(in PixelShaderMetrics metrics)
    {
        GUILayout.BeginHorizontal();
        DrawPixelShaderVariantMetrics("Main variant", metrics.Variant);
        GUILayout.FlexibleSpace();
        DrawPixelShaderProperties(metrics);
        GUILayout.EndHorizontal();
    }

    private void DrawVertexShaderVariantMetrics(string header, in VertexShaderVariantMetrics metrics)
    {
        GUILayout.BeginVertical();
        GUILayout.Box(header, _boxStyle);

        GUILayout.BeginHorizontal();
        {
            GUILayout.BeginVertical(_boxStyle);
            DrawLabelWithText("Work registers: ", metrics.WorkRegisters);
            DrawLabelWithText("Uniform registers: ", metrics.UniformRegisters);
            DrawLabelWithText("Stack spilling: ", metrics.StackSpilling);
            DrawLabelWithText("16-bit arithmetic: ", metrics.Arithmetic16Bit + "%");
            GUILayout.EndVertical();
        }
        {
            GUILayout.BeginVertical(_boxStyle);
            DrawCyclesRow(string.Empty, "A", "LS", "T", "Bound");
            DrawCyclesRow("Total instruction cycles:", metrics.TotalCycles);
            DrawCyclesRow("Shortest path cycles:", metrics.ShortestCycles);
            DrawCyclesRow("Longest path cycles:", metrics.LongestCycles);
            GUILayout.EndVertical();
        }
        GUILayout.EndHorizontal();

        GUILayout.EndVertical();
    }

    private void DrawPixelShaderVariantMetrics(string header, in PixelShaderVariantMetrics metrics)
    {
        GUILayout.BeginVertical();
        GUILayout.Box(header, _boxStyle);

        GUILayout.BeginHorizontal();
        {
            GUILayout.BeginVertical(_boxStyle);
            DrawLabelWithText("Work registers: ", metrics.WorkRegisters);
            DrawLabelWithText("Uniform registers: ", metrics.UniformRegisters);
            DrawLabelWithText("Stack spilling: ", metrics.StackSpilling);
            DrawLabelWithText("16-bit arithmetic: ", metrics.Arithmetic16Bit + "%");
            GUILayout.EndVertical();
        }
        {
            GUILayout.BeginVertical(_boxStyle);
            DrawCyclesRow(string.Empty, "A", "LS", "V", "T", "Bound");
            DrawCyclesRow("Total instruction cycles:", metrics.TotalCycles);
            DrawCyclesRow("Shortest path cycles:", metrics.ShortestCycles);
            DrawCyclesRow("Longest path cycles:", metrics.LongestCycles);
            GUILayout.EndVertical();
        }
        GUILayout.EndHorizontal();

        GUILayout.EndVertical();
    }

    private void DrawVertexShaderProperties(in VertexShaderMetrics metrics)
    {
        GUILayout.BeginVertical();
        GUILayout.Box("Shader properties", _boxStyle);

        GUILayout.BeginVertical(_boxStyle);
        DrawLabelWithText("Has uniform computation:", metrics.HasUniformComputation);
        GUILayout.EndVertical();

        GUILayout.EndVertical();
    }

    private void DrawPixelShaderProperties(in PixelShaderMetrics metrics)
    {
        GUILayout.BeginVertical();
        GUILayout.Box("Shader properties", _boxStyle);

        GUILayout.BeginVertical(_boxStyle);
        DrawLabelWithText("Has uniform computation:", metrics.HasUniformComputation);
        DrawLabelWithText("Has side-effects:", metrics.HasSideEffects);
        DrawLabelWithText("Modifies coverage:", metrics.ModifiesCoverage);
        DrawLabelWithText("Uses late ZS test:", metrics.UsesLateZsTest);
        DrawLabelWithText("Uses late ZS update:", metrics.UsesLateZsUpdate);
        DrawLabelWithText("Reads color buffer:", metrics.ReadsColorBuffer);
        GUILayout.EndVertical();

        GUILayout.EndVertical();
    }

    private void DrawLabelWithText<T>(string label, T value)
    {
        GUILayout.BeginHorizontal(GUILayout.Width(200));
        GUILayout.Label(label);
        GUILayout.FlexibleSpace();
        GUILayout.Label(value.ToString());
        GUILayout.EndHorizontal();
    }

    private void DrawCyclesRow(string label, in VertexShaderVariantMetrics.Cycles cycles)
    {
        DrawCyclesRow(label,
            cycles.Arithmetic.ToString(CyclesFloatFormat),
            cycles.LoadStore.ToString(CyclesFloatFormat),
            cycles.Texture.ToString(CyclesFloatFormat),
            cycles.Bound.ToString()
        );
    }

    private void DrawCyclesRow(string label, in PixelShaderVariantMetrics.Cycles cycles)
    {
        DrawCyclesRow(label,
            cycles.Arithmetic.ToString(CyclesFloatFormat),
            cycles.LoadStore.ToString(CyclesFloatFormat),
            cycles.Varying.ToString(CyclesFloatFormat),
            cycles.Texture.ToString(CyclesFloatFormat),
            cycles.Bound.ToString()
        );
    }

    private void DrawCyclesRow(string label, string v1, string v2, string v3, string v4)
    {
        GUILayoutOption cyclesWidth = GUILayout.Width(150);
        GUILayoutOption otherWidth = GUILayout.Width(50);

        GUILayout.BeginHorizontal();
        GUILayout.Label(label, cyclesWidth);
        GUILayout.Label(v1, otherWidth);
        GUILayout.Label(v2, otherWidth);
        GUILayout.Label(v3, otherWidth);
        GUILayout.Label(v4, otherWidth);
        GUILayout.EndHorizontal();
    }

    private void DrawCyclesRow(string label, string v1, string v2, string v3, string v4, string v5)
    {
        GUILayoutOption cyclesWidth = GUILayout.Width(150);
        GUILayoutOption otherWidth = GUILayout.Width(50);

        GUILayout.BeginHorizontal();
        GUILayout.Label(label, cyclesWidth);
        GUILayout.Label(v1, otherWidth);
        GUILayout.Label(v2, otherWidth);
        GUILayout.Label(v3, otherWidth);
        GUILayout.Label(v4, otherWidth);
        GUILayout.Label(v5, otherWidth);
        GUILayout.EndHorizontal();
    }

    [MenuItem("Window/Analysis/Mali Offline Compiler")]
    public static void Run()
    {
        CreateWindow<MaliocToolWindow>("Mali Offline Compiler");
    }

    private static void ComputeMetrics(ref CompiledShaderVariant variant)
    {
        for (int stageIndex = 0; stageIndex < variant.Stages.Length; stageIndex++)
        {
            ref CompilerShaderStage shaderStage = ref variant.Stages[stageIndex];
            string fileName = Guid.NewGuid().ToString();
            string shaderFilePath = Path.Combine(Application.dataPath, "..", "Temp", fileName);

            File.WriteAllLines(shaderFilePath, shaderStage.Code);

            string stageFlag = shaderStage.StageType switch
            {
                CompiledShaderStageType.Vertex => "v",
                CompiledShaderStageType.Pixel => "f",
                _ => throw new ArgumentOutOfRangeException(),
            };

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    Arguments =
                        $"-c Mali-G76 -{stageFlag} {shaderFilePath}",
                    FileName = "malioc.exe",
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                },
            };
            process.Start();

            var metrics = new List<string>();

            while (!process.StandardOutput.EndOfStream)
            {
                string line = process.StandardOutput.ReadLine();
                metrics.Add(line);
            }

            File.Delete(shaderFilePath);

            string[] metricsLines = metrics.ToArray();
            switch (shaderStage.StageType)
            {
                case CompiledShaderStageType.Vertex:
                    shaderStage.VertexShaderMetrics = MetricsParser.ParseVertexShaderMetrics(metricsLines);
                    break;
                case CompiledShaderStageType.Pixel:
                    shaderStage.PixelShaderMetrics = MetricsParser.ParsePixelShaderMetrics(metricsLines);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }

    private static void OpenCompiledShader(Shader shader)
    {
        Type shaderUtilType = typeof(ShaderUtil);
        MethodInfo openCompiledShaderMethod =
            shaderUtilType.GetMethod("OpenCompiledShader", BindingFlags.NonPublic | BindingFlags.Static);
        const int mode = 3; // custom platform
        const bool includeAllVariants = false;
        const bool preprocessOnly = false;
        const bool stripLineDirectives = false;
        openCompiledShaderMethod?.Invoke(null, new object[]
            {
                shader,
                mode,
                PlatformMask,
                includeAllVariants,
                preprocessOnly,
                stripLineDirectives,
            }
        );
    }

    private void AnalyzeShader(Shader shader)
    {
        string compiledShaderName = "Compiled-" + shader.name.Replace('/', '-') + ".shader";
        string[] lines = File.ReadAllLines(Path.Combine(Application.dataPath, "..", "Temp", compiledShaderName));

        _compiledShader = CompiledShaderParser.Parse(lines);
    }
}