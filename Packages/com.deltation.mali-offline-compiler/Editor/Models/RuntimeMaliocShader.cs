using System;
using System.Collections.Generic;

namespace DELTation.MaliOfflineCompiler.Editor.Models
{
    [Serializable]
    public class RuntimeMaliocShader
    {
        [Serializable]
        public enum ShaderVariantPipelineType
        {
            Null,
            Arithmetic,
            LoadStore,
            Varying,
            Texture,
        }

        public List<ShaderProperty> Properties;
        public List<ShaderVariant> Variants;

        [Serializable]
        public class ShaderProperty
        {
            public enum Unit
            {
                None,
                Percent,
            }

            public string Name;
            public DynamicValue Value;
            public Unit ValueUnit;
        }

        [Serializable]
        public class ShaderVariant
        {
            public string Name;
            public List<ShaderVariantPipelineType> Pipelines;
            public ShaderPipelineCycles LongestPathCycles;
            public ShaderPipelineCycles ShortestPathCycles;
            public ShaderPipelineCycles TotalCycles;
            public List<ShaderProperty> Properties;
        }

        [Serializable]
        public class ShaderPipelineCycles
        {
            public List<float> PipelineCycles;
            public ShaderVariantPipelineType BoundPipeline;
        }
    }
}