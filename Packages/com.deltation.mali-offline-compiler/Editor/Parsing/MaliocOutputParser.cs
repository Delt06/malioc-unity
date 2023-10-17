using System;
using System.Linq;
using DELTation.MaliOfflineCompiler.Editor.Models;
using JetBrains.Annotations;
using Newtonsoft.Json;
using NUnit.Framework;
using static DELTation.MaliOfflineCompiler.Editor.Models.RuntimeMaliocShader;

namespace DELTation.MaliOfflineCompiler.Editor.Parsing
{
    public static class MaliocOutputParser
    {
        public static RuntimeMaliocShader Parse([NotNull] string json)
        {
            if (json == null)
            {
                throw new ArgumentNullException(nameof(json));
            }

            var jsonSerializerSettings = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
            };
            JsonMaliocOutput jsonModel = JsonConvert.DeserializeObject<JsonMaliocOutput>(json, jsonSerializerSettings);
            Assert.IsNotNull(jsonModel);

            Assert.IsTrue(jsonModel.shaders.Length == 1);
            JsonMaliocOutput.Shader shader = jsonModel.shaders[0];

            return new RuntimeMaliocShader
            {
                Properties = shader.properties.Select(ConvertProperty).ToList(),
                Variants = shader.variants.Select(variant =>
                    {
                        var pipelines = variant.performance.pipelines.Select(ParsePipelineType).ToList();
                        return new ShaderVariant
                        {
                            Name = variant.name,
                            Properties = variant.properties.Select(ConvertVariantProperty).ToList(),
                            Pipelines = pipelines,
                            LongestPathCycles =
                                ParseShaderPipelineCycles(variant.performance.longest_path_cycles),
                            ShortestPathCycles =
                                ParseShaderPipelineCycles(variant.performance.shortest_path_cycles),
                            TotalCycles = ParseShaderPipelineCycles(variant.performance.total_cycles),
                        };
                    }
                ).ToList(),
            };
        }

        private static ShaderProperty ConvertProperty(JsonMaliocOutput.ShaderProperty property) =>
            new() { Name = property.display_name, Value = new DynamicValue(property.value) };

        private static ShaderProperty
            ConvertVariantProperty(JsonMaliocOutput.ShaderProperty property) =>
            new()
            {
                Name = property.display_name, Value = new DynamicValue(property.value),
                ValueUnit = ParseValueUnit(property.name),
            };

        private static ShaderProperty.Unit ParseValueUnit(string name) =>
            name switch
            {
                "thread_occupancy" => ShaderProperty.Unit.Percent,
                "fp16_arithmetic" => ShaderProperty.Unit.Percent,
                var _ => ShaderProperty.Unit.None,
            };

        private static ShaderVariantPipelineType ParsePipelineType(string text) =>
            text switch
            {
                "arithmetic" => ShaderVariantPipelineType.Arithmetic,
                "load_store" => ShaderVariantPipelineType.LoadStore,
                "varying" => ShaderVariantPipelineType.Varying,
                "texture" => ShaderVariantPipelineType.Texture,
                null => ShaderVariantPipelineType.Null,
                var _ => throw new ArgumentOutOfRangeException(nameof(text), text, "Invalid pipeline type."),
            };

        private static ShaderPipelineCycles ParseShaderPipelineCycles(
            JsonMaliocOutput.ShaderVariantCycles cycles) =>
            new()
            {
                PipelineCycles = cycles.cycle_count.Select(f => f ?? 0).ToList(),
                BoundPipeline = ParsePipelineType(cycles.bound_pipelines.First()),
            };
    }
}