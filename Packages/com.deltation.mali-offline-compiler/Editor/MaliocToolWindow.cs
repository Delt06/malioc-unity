using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using DELTation.MaliOfflineCompiler.Editor.Compilation;
using DELTation.MaliOfflineCompiler.Editor.Models;
using DELTation.MaliOfflineCompiler.Editor.Parsing;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;

namespace DELTation.MaliOfflineCompiler.Editor
{
    public class MaliocToolWindow : EditorWindow
    {
        private const int PlatformMask = 1 << (int) ShaderCompilerPlatform.GLES3x;

        private static readonly IEqualityComparer<string>
            StringLowercaseEqualityComparerInstance = new StringLowercaseEqualityComparer();

        [SerializeField]
        private Shader _shader;
        [SerializeField]
        private CompiledShaderHardwareTier _hardwareTier = CompiledShaderHardwareTier.Tier1;
        [SerializeField]
        private CompiledShader _compiledShader;
        [SerializeField]
        private CompiledShader _baseline;
        [SerializeField]
        private bool _viewBaseline;
        [SerializeField]
        private Vector2 _scrollPosition = Vector2.zero;
        [SerializeField]
        private string _searchString;
        [SerializeField]
        private List<string> _searchKeywords = new();
        private readonly MaliocShaderDrawer _maliocShaderDrawer = new();

        private GUIStyle _boxStyle;
        private bool[] _expanded;
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
            _maliocShaderDrawer.Begin(_boxStyle);


            HandleKeyboard();

            GUILayout.BeginHorizontal();
            _shader = (Shader) EditorGUILayout.ObjectField(new GUIContent("Shader"), _shader, typeof(Shader), false);

            EditorGUI.BeginDisabledGroup(_shader == null);

            if (GUILayout.Button("Analyze"))
            {
                OpenCompiledShader(_shader);
                AnalyzeShader(_shader);
            }

            EditorGUI.EndDisabledGroup();
            GUILayout.EndHorizontal();

            if (!_compiledShader.IsValid || _compiledShader.Variants == null)
            {
                return;
            }

            if (_compiledShader.Variants.Length == 0)
            {
                EditorGUILayout.HelpBox("The compiled shader does not have any variants.", MessageType.Warning);
                return;
            }

            GUILayout.BeginHorizontal();

            EditorGUI.BeginDisabledGroup(_viewBaseline);
            if (GUILayout.Button("Save Baseline"))
            {
                _baseline = _compiledShader;
            }

            EditorGUI.EndDisabledGroup();

            _viewBaseline = GUILayout.Toggle(_viewBaseline, "View Baseline");

            GUILayout.EndHorizontal();

            {
                EditorGUI.BeginChangeCheck();
                _searchString = EditorGUILayout.TextField("Filter:", _searchString);
                if (EditorGUI.EndChangeCheck())
                {
                    _searchKeywords.Clear();
                    _searchKeywords.AddRange(_searchString.Split(' '));
                }
            }

            _expanded ??= new bool[_compiledShader.Variants.Length];

            if (_viewBaseline)
            {
                if (_baseline.IsValid)
                {
                    Color originalTint = GUI.color;
                    GUI.color = Color.Lerp(originalTint, Color.red, 0.5f);
                    DrawAllVariants(_baseline);
                    GUI.color = originalTint;
                }
                else
                {
                    EditorGUILayout.HelpBox("The baseline has not been saved yet.", MessageType.Warning);
                }
            }
            else
            {
                DrawAllVariants(_compiledShader);
            }
        }

        private void HandleKeyboard()
        {
            Event current = Event.current;
            if (current.type != EventType.KeyDown)
            {
                return;
            }

            if (current.keyCode == KeyCode.B)
            {
                _viewBaseline = !_viewBaseline;
                current.Use();
            }
        }

        private void DrawAllVariants(in CompiledShader shownShader)
        {
            GUILayout.BeginHorizontal();

            GUILayout.Box(shownShader.Name ?? "Unknown Shader", _boxStyle);
            GUILayout.FlexibleSpace();
            GUILayout.Label("Tier: ");
            _hardwareTier = (CompiledShaderHardwareTier) EditorGUILayout.EnumPopup(_hardwareTier);

            GUILayout.EndHorizontal();

            _scrollPosition = GUILayout.BeginScrollView(_scrollPosition, false, true);

            for (int index = 0; index < shownShader.Variants.Length; index++)
            {
                ref CompiledShaderVariant variant = ref shownShader.Variants[index];

                if (variant.HardwareTier != _hardwareTier || !HasAllKeywords(variant))
                {
                    continue;
                }

                const string none = "<none>";
                string keywords = variant.Keywords.Length == 0 ? none : string.Join(' ', variant.Keywords);
                string passName = string.IsNullOrWhiteSpace(variant.PassName) ? none : variant.PassName;
                string lightMode = string.IsNullOrWhiteSpace(variant.LightMode) ? none : variant.LightMode;
                string label = $"Name: {passName} | LightMode: {lightMode} | Keywords: {keywords}";
                ref bool expanded = ref _expanded[index];
                expanded = EditorGUILayout.Foldout(expanded, label, _foldoutStyle);

                if (expanded)
                {
                    if (!variant.ComputedMetrics)
                    {
                        variant.ComputedMetrics = true;
                        ComputeMetrics(ref shownShader.Variants[index]);
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

        private bool HasAllKeywords(in CompiledShaderVariant variant)
        {
            if (_searchKeywords.Count == 0 || _searchKeywords[0] == string.Empty)
            {
                return true;
            }

            foreach (string searchKeyword in _searchKeywords)
            {
                if (!variant.Keywords.Contains(searchKeyword, StringLowercaseEqualityComparerInstance) &&
                    !StringLowercaseEqualityComparerInstance.Equals(searchKeyword, variant.LightMode) &&
                    !StringLowercaseEqualityComparerInstance.Equals(searchKeyword, variant.PassName))
                {
                    return false;
                }
            }

            return true;
        }

        private void DrawMetrics(in CompilerShaderStage stage)
        {
            _maliocShaderDrawer.DrawMetrics(stage);
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
                            $"--core Mali-G76 --format json -{stageFlag} {shaderFilePath}",
                        FileName = "malioc.exe",
                        CreateNoWindow = true,
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                    },
                };
                process.Start();

                string json = process.StandardOutput.ReadToEnd();
                RuntimeMaliocShader model = MaliocOutputParser.Parse(json);

                File.Delete(shaderFilePath);

                switch (shaderStage.StageType)
                {
                    case CompiledShaderStageType.Vertex:
                        shaderStage.VertexShaderMetrics = model;
                        break;
                    case CompiledShaderStageType.Pixel:
                        shaderStage.PixelShaderMetrics = model;
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

            CompiledShader oldCompiledShader = _compiledShader;
            _compiledShader = CompiledShaderParser.Parse(shader, lines);

            if (_expanded == null || _expanded.Length != _compiledShader.Variants.Length)
            {
                _expanded = new bool[_compiledShader.Variants.Length];
            }
            else
            {
                // clear expanded values
                if (!oldCompiledShader.IsValid || !HaveAllSameKeywords(oldCompiledShader, _compiledShader))
                {
                    Array.Clear(_expanded, 0, _expanded.Length);
                }
            }
        }

        private bool HaveAllSameKeywords(in CompiledShader shader1, in CompiledShader shader2)
        {
            if (shader1.Variants.Length != shader2.Variants.Length)
            {
                return false;
            }

            for (int i = 0; i < shader1.Variants.Length; i++)
            {
                if (!HaveSameKeywords(shader1.Variants[i], shader2.Variants[i]))
                {
                    return false;
                }
            }

            return true;
        }

        private bool HaveSameKeywords(in CompiledShaderVariant variant1, in CompiledShaderVariant variant2)
        {
            if (variant1.Keywords.Length != variant2.Keywords.Length)
            {
                return false;
            }

            for (int i = 0; i < variant1.Keywords.Length; i++)
            {
                if (variant1.Keywords[i] != variant2.Keywords[i])
                {
                    return false;
                }
            }

            return true;
        }

        private class StringLowercaseEqualityComparer : IEqualityComparer<string>
        {
            public bool Equals(string x, string y) => string.Equals(x, y, StringComparison.InvariantCultureIgnoreCase);

            public int GetHashCode(string obj) => obj.GetHashCode();
        }
    }
}