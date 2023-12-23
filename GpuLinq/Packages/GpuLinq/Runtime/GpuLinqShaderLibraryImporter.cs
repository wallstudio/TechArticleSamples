#if UNITY_EDITOR
using System.Linq;
using UnityEngine;
using System.Text.RegularExpressions;

namespace Uphash.Editor
{
    using System;
    using System.Text;
    using UnityEditor;
    using UnityEditor.AssetImporters;

    [ScriptedImporter(1, "gpulinqlib")]
    class GpuLinqShaderLibraryImporter : ScriptedImporter
    {
        [InitializeOnLoadMethod]
        static void InitializeOnLoadMethod()
        {
            var lib = ShaderLibrary.Load();
            AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(lib));  
        }

        [SerializeField] bool enableDebugFlag = true;
        
        public override void OnImportAsset(AssetImportContext ctx)
        {
            var lib = ScriptableObject.CreateInstance<ShaderLibrary>();

            var sourceFiles = AssetDatabase.FindAssets("t:MonoScript")
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<MonoScript>)
                .Where(x => x.name != "ShaderLibrary")
                .Where(x => x.text.Contains(".AsGpuEnumerable()"))
                .ToArray();
            foreach (var file in sourceFiles)
            {
                var line = file.text.Replace("\r\n", "\n").Split('\n');
                var funcs = line
                    .Select((text, i) => (text, startIndex: i))
                    .Where(startLine => startLine.text.Contains(".AsGpuEnumerable()"))
                    .SelectMany(startLine => line
                        .Select((text, i) => (text, targetIndex: i))
                        .Skip(startLine.startIndex + 1)
                        .Select(targetLine =>
                        {
                            var regex = new Regex(@"^\s*\.(?<ope>Where|Select)\((?<arg>.+)=>(?<func>.+)\);?\s*(\/\/.*)?\s*$");
                            return (lineNo: targetLine.targetIndex, m: regex.Match(targetLine.text));
                        })
                        .TakeWhile(lno_m => lno_m.m.Success)
                        .ToArray())
                    .ToArray();

                foreach (var (lineNo, match) in funcs)
                {
                    var ope = match.Groups["ope"].Value;
                    var member = "?";
                    var arg = match.Groups["arg"].Value;
                    var func = match.Groups["func"].Value;
                    var code = Convert(ope, arg, func);
                    var text = new TextAsset(code){ name = $"{file.name}::{member}#{lineNo}" };
                    ctx.AddObjectToAsset($"code_{text.name}", text);
                    var shader = ShaderUtil.CreateComputeShaderAsset(ctx, code);
                    shader.name = text.name;
                    ctx.AddObjectToAsset($"shader_{shader.name}", shader);
                    lib.shaders.Add(new ShaderLibrary.KeyValue
                    {
                        file = file.name,
                        member = member,
                        line = lineNo,
                        shader = shader,
                    });
                }
            }
            ctx.AddObjectToAsset("index", lib);
            ctx.SetMainObject(lib);
        }

        string Convert(string ope, string arg, string func)
        {
            var sb = new StringBuilder();
            if (enableDebugFlag)
            {
                sb.AppendLine($"#pragma enable_d3d11_debug_symbols");
            }
            sb.AppendLine($"#pragma kernel CSMain");
            switch (ope)
            {
                case "Where":
                    ConvertWhere(arg, func, sb);
                    break;
                case "Select":
                    ConvertSelect(arg, func, sb);
                    break;
                default:
                    throw new NotImplementedException($"Unknown operator: {ope}");
            }
            return sb.ToString();

            static void ConvertWhere(string arg, string func, StringBuilder sb)
            {
                sb.AppendLine($"StructuredBuffer<int> _Input;");
                sb.AppendLine($"AppendStructuredBuffer<int> _Output;");
                sb.AppendLine($"uint _InputLength;");
                sb.AppendLine();
                sb.AppendLine($"[numthreads(64, 1, 1)]");
                sb.AppendLine($"void CSMain(uint3 id : SV_DispatchThreadID)");
                sb.AppendLine($"{{");
                sb.AppendLine($"    if (id.x >= _InputLength) return;");
                sb.AppendLine($"");
                sb.AppendLine($"    int {arg} = _Input[id.x];");
                sb.AppendLine($"    if ({func})");
                sb.AppendLine($"    {{");
                sb.AppendLine($"        _Output.Append({arg});");
                sb.AppendLine($"    }}");
                sb.AppendLine($"}}");
            }

            static void ConvertSelect(string arg, string func, StringBuilder sb)
            {
                sb.AppendLine($"StructuredBuffer<int> _Input;");
                sb.AppendLine($"RWStructuredBuffer<int> _Output;");
                sb.AppendLine($"uint _InputLength;");
                sb.AppendLine();
                sb.AppendLine($"[numthreads(64, 1, 1)]");
                sb.AppendLine($"void CSMain(uint3 id : SV_DispatchThreadID)");
                sb.AppendLine($"{{");
                sb.AppendLine($"    if (id.x >= _InputLength) return;");
                sb.AppendLine($"");
                sb.AppendLine($"    int {arg} = _Input[id.x];");
                sb.AppendLine($"    _Output[id.x] = {func};");
                sb.AppendLine($"}}");
            }

        }
    }
}
#endif
