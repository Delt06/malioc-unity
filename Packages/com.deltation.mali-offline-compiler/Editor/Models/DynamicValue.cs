using System;

namespace DELTation.MaliOfflineCompiler.Editor.Models
{
    [Serializable]
    public struct DynamicValue
    {
        public enum ValueType
        {
            Unknown,
            Float,
            Int,
            Bool,
        }

        public ValueType Type;
        public int Int;
        public float Float;
        public bool Bool;

        public DynamicValue(object value)
        {
            if (value is long l)
            {
                value = (int) l;
            }

            (Type, Int, Float, Bool) = value switch
            {
                int i => (ValueType.Int, i, 0, false),
                float f => (ValueType.Float, 0, f, false),
                bool b => (ValueType.Bool, 0, 0, b),
                var _ => throw new ArgumentOutOfRangeException(nameof(value),
                    $"Invalid object type: {value?.GetType()}"
                ),
            };
        }

        public override string ToString() =>
            Type switch
            {
                ValueType.Unknown => throw new InvalidOperationException("Unknown value type"),
                ValueType.Float => Float.ToString("F2"),
                ValueType.Int => Int.ToString(),
                ValueType.Bool => Bool.ToString(),
                _ => throw new ArgumentOutOfRangeException(),
            };
    }
}