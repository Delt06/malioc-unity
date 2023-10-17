using System;
using System.Collections.Generic;
using DELTation.MaliOfflineCompiler.Editor.Compilation;
using DELTation.MaliOfflineCompiler.Editor.Models;
using UnityEngine;

namespace DELTation.MaliOfflineCompiler.Editor
{
    public class MaliocShaderDrawer
    {
        private const string CyclesFloatFormat = "F2";

        private GUIStyle _boxStyle;

        public void Begin(GUIStyle boxStyle)
        {
            _boxStyle = boxStyle;
        }

        public void DrawMetrics(in CompilerShaderStage stage)
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
                    DrawShaderMetrics(stage.VertexShaderMetrics);
                    break;
                case CompiledShaderStageType.Pixel:
                    DrawShaderMetrics(stage.PixelShaderMetrics);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            GUILayout.EndHorizontal();
        }

        private void DrawShaderMetrics(RuntimeMaliocShader shader)
        {
            if (shader == null)
            {
                return;
            }

            GUILayout.BeginHorizontal();

            foreach (RuntimeMaliocShader.ShaderVariant shaderVariant in shader.Variants)
            {
                DrawShaderVariant(shaderVariant);
                GUILayout.FlexibleSpace();
            }

            {
                GUILayout.BeginVertical();
                GUILayout.Box("Shader Properties", _boxStyle);
                DrawShaderProperties(shader.Properties);
                GUILayout.EndVertical();
            }

            GUILayout.EndHorizontal();
        }

        private void DrawShaderVariant(RuntimeMaliocShader.ShaderVariant shaderVariant)
        {
            GUILayout.BeginVertical();
            GUILayout.Box(shaderVariant.Name, _boxStyle);

            GUILayout.BeginHorizontal();
            {
                DrawShaderProperties(shaderVariant.Properties);
            }
            {
                GUILayout.BeginVertical(_boxStyle);
                DrawCyclesRow(string.Empty, shaderVariant.Pipelines);
                DrawCyclesRow("Total instruction cycles:", shaderVariant.TotalCycles);
                DrawCyclesRow("Shortest path cycles:", shaderVariant.ShortestPathCycles);
                DrawCyclesRow("Longest path cycles:", shaderVariant.LongestPathCycles);
                GUILayout.EndVertical();
            }
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();
        }

        private void DrawShaderProperties(List<RuntimeMaliocShader.ShaderProperty> properties)
        {
            GUILayout.BeginVertical(_boxStyle);

            foreach (RuntimeMaliocShader.ShaderProperty property in properties)
            {
                DrawLabelWithText(property.Name, property.Value + ValueUnitToString(property.ValueUnit));
            }

            GUILayout.EndVertical();
        }

        private static string ValueUnitToString(RuntimeMaliocShader.ShaderProperty.Unit unit) =>
            unit switch
            {
                RuntimeMaliocShader.ShaderProperty.Unit.None => string.Empty,
                RuntimeMaliocShader.ShaderProperty.Unit.Percent => "%",
                _ => throw new ArgumentOutOfRangeException(nameof(unit), unit, null),
            };

        private static void DrawLabelWithText<T>(string label, T value)
        {
            GUILayout.BeginHorizontal(GUILayout.Width(200));
            GUILayout.Label(label);
            GUILayout.FlexibleSpace();
            GUILayout.Label(value.ToString());
            GUILayout.EndHorizontal();
        }

        private static void DrawCyclesRow(string label, RuntimeMaliocShader.ShaderPipelineCycles cycles)
        {
            DrawCyclesRow(label,
                cycles.PipelineCycles,
                cycles.BoundPipeline
            );
        }

        private static void DrawCyclesRow(string label, List<RuntimeMaliocShader.ShaderVariantPipelineType> pipelines)
        {
            GUILayoutOption cyclesWidth = GUILayout.Width(150);
            GUILayoutOption otherWidth = GUILayout.Width(50);

            GUILayout.BeginHorizontal();
            GUILayout.Label(label, cyclesWidth);

            foreach (RuntimeMaliocShader.ShaderVariantPipelineType pipeline in pipelines)
            {
                GUILayout.Label(ShaderVariantPipelineTypeToString(pipeline), otherWidth);
            }

            GUILayout.Label("Bound", otherWidth);

            GUILayout.EndHorizontal();
        }

        private static void DrawCyclesRow(string label, List<float> cycles,
            RuntimeMaliocShader.ShaderVariantPipelineType bound)
        {
            GUILayoutOption cyclesWidth = GUILayout.Width(150);
            GUILayoutOption otherWidth = GUILayout.Width(50);

            GUILayout.BeginHorizontal();
            GUILayout.Label(label, cyclesWidth);

            foreach (float cycle in cycles)
            {
                GUILayout.Label(cycle.ToString(CyclesFloatFormat), otherWidth);
            }

            GUILayout.Label(ShaderVariantPipelineTypeToString(bound), otherWidth);

            GUILayout.EndHorizontal();
        }

        private static string ShaderVariantPipelineTypeToString(
            RuntimeMaliocShader.ShaderVariantPipelineType pipelineType) =>
            pipelineType switch
            {
                RuntimeMaliocShader.ShaderVariantPipelineType.Arithmetic => "A",
                RuntimeMaliocShader.ShaderVariantPipelineType.LoadStore => "LS",
                RuntimeMaliocShader.ShaderVariantPipelineType.Varying => "V",
                RuntimeMaliocShader.ShaderVariantPipelineType.Texture => "T",
                RuntimeMaliocShader.ShaderVariantPipelineType.Null => "N/A",
                _ => throw new ArgumentOutOfRangeException(nameof(pipelineType), pipelineType, null),
            };
    }
}