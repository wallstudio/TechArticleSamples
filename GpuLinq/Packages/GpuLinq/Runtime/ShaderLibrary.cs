using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.IO;

namespace Uphash
{
    class ShaderLibrary : ScriptableObject, ISerializationCallbackReceiver
    {
        public static ShaderLibrary Load() => Resources.LoadAll("__GpuLinq_ShaderLibrary__").OfType<ShaderLibrary>().First();

        [Serializable]
        public struct KeyValue
        {
            public string file;
            public string member;
            public int line;
            public ComputeShader shader;
            public TextAsset code;
        }

        [SerializeField] public List<KeyValue> shaders = new();

        Dictionary<(string, string, int), (ComputeShader cs, TextAsset code)> shaderMap;

        public (ComputeShader cs, TextAsset code) Resolve(string file, string member, int line)
            => shaderMap[(Path.GetFileNameWithoutExtension(file), "?", line)];

        public void OnBeforeSerialize() {}

        public void OnAfterDeserialize()
            => shaderMap = shaders.ToDictionary(x => (x.file, x.member, x.line), x => (x.shader, x.code));
    }
}
