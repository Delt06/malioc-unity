// ReSharper disable InconsistentNaming

using System;

namespace DELTation.MaliOfflineCompiler.Editor.Models
{
    [Serializable]
    internal class JsonMaliocOutput
    {
        public Shader[] shaders;

        [Serializable]
        public class Shader
        {
            public ShaderProperty[] properties;
            public ShaderVariant[] variants;
        }

        [Serializable]
        public class ShaderProperty
        {
            public string display_name;
            public string name;
            public object value;
        }

        [Serializable]
        public class ShaderVariant
        {
            public string name;
            public ShaderVariantPerformance performance;
            public ShaderProperty[] properties;
        }

        [Serializable]
        public class ShaderVariantPerformance
        {
            public string[] pipelines;
            public ShaderVariantCycles longest_path_cycles;
            public ShaderVariantCycles shortest_path_cycles;
            public ShaderVariantCycles total_cycles;
        }

        [Serializable]
        public class ShaderVariantCycles
        {
            public string[] bound_pipelines;
            public float?[] cycle_count;
        }
    }
}