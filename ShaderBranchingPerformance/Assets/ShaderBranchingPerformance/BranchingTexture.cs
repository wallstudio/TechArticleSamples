using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;

public class BranchingTexture : MonoBehaviour
{
    const int N = 4 * 1024;

    enum BraType { STATIC, UNIFORM, DIVERGENT, }
    enum BranchType { FAST, SLOW, FLATTEN_FAST, }

    [SerializeField] ComputeShader cs;
    [SerializeField][Min(1)] int Slow_DivergentFastInterval = 1;

    Texture2D input;
    GraphicsBuffer output;
    
    class Entry
    {
        public CustomSampler samp;
        public List<float> history = new List<float>();
        public float us = -1;
        public Entry(string proName) => samp = CustomSampler.Create(proName, collectGpuData: true);
    }
    Dictionary<(BraType mode, BranchType path), Entry> averageUs = new ()
    {
        [(BraType.STATIC, BranchType.FAST)] = new ("STATIC_FAST"),
        [(BraType.STATIC, BranchType.SLOW)] = new ("STATIC_SLOW"),
        [(BraType.UNIFORM, BranchType.FAST)] = new ("UNIFORM_FAST"),
        [(BraType.UNIFORM, BranchType.SLOW)] = new ("UNIFORM_SLOW"),
        [(BraType.UNIFORM, BranchType.FLATTEN_FAST)] = new ("UNIFORM_FLATTEN_FAST"),
        [(BraType.DIVERGENT, BranchType.FAST)] = new ("DIVERGENT_FAST"),
        [(BraType.DIVERGENT, BranchType.SLOW)] = new ("DIVERGENT_SLOW"),
        [(BraType.DIVERGENT, BranchType.FLATTEN_FAST)] = new ("DIVERGENT_FLATTEN_FAST"),
    };

    void Start()
    {
        var aspect = (float)Display.main.systemWidth / Display.main.systemHeight;
        var width = 360;
        var height = (int)(width / aspect);
        Screen.SetResolution(width, height, fullscreen: false);

        input = new Texture2D(N, N, TextureFormat.RGBA32, false, linear: true);
        input.SetPixels(Enumerable.Range(0, N)
            .SelectMany(y => Enumerable.Range(0, N).Select(x => (x: x / (float)N, y: y / (float)N)).ToArray())
            .Select(xy => new Color(xy.x, xy.y, 0, 1))
            .ToArray());
        input.Apply(updateMipmaps: false, makeNoLongerReadable: true);
        output = new GraphicsBuffer(GraphicsBuffer.Target.Structured, N, sizeof(float));
    }

    void Update()
    {
        using var cmd = new CommandBuffer(){ name = "Bra" };
        {
            foreach (var ((type, branchType), entry) in averageUs)
            {
                var kernel = type switch
                {
                    BraType.STATIC => branchType switch
                    {
                        BranchType.FAST => cs.FindKernel("STATIC_FAST"),
                        BranchType.SLOW => cs.FindKernel("STATIC_SLOW"),
                        BranchType.FLATTEN_FAST => throw new NotImplementedException(),
                        _ => throw new NotImplementedException(),
                    },
                    BraType.UNIFORM => branchType switch
                    {
                        BranchType.FLATTEN_FAST => cs.FindKernel("UNIFORM_FLATTEN"),
                        _ => cs.FindKernel("UNIFORM_BRANCH"),
                    },
                    BraType.DIVERGENT => branchType switch
                    {
                        BranchType.FLATTEN_FAST => cs.FindKernel("DIVERGENT_FLATTEN"),
                        _ => cs.FindKernel("DIVERGENT_BRANCH"),
                    },
                    _ => throw new NotImplementedException(),
                };
                cmd.SetComputeIntParam(cs, "_Uniform", branchType == BranchType.FAST ? 0 : 1);
                cmd.SetComputeIntParam(cs, "_DivergentFastInterval", branchType == BranchType.FAST ? 1000000000 : Slow_DivergentFastInterval);
                cmd.SetComputeTextureParam(cs, kernel, "_Input", input);
                cmd.SetComputeIntParam(cs, "_Input_Width", input.width);
                cmd.SetComputeIntParam(cs, "_Input_Height", input.height);
                cmd.SetComputeBufferParam(cs, kernel, "_Output", output);

                cmd.BeginSample(entry.samp);
                cmd.DispatchCompute(cs, kernel, N / 32, 1, 1);
                cmd.EndSample(entry.samp);
            
                entry.history.Add(entry.samp.GetRecorder().gpuElapsedNanoseconds / 1000f);
                if(entry.history.Count >= 60)
                {
                    entry.us = SystemInfo.supportsGpuRecorder ? entry.history.Average() : -1;
                    entry.history.Clear();
                }
            }
            Graphics.ExecuteCommandBuffer(cmd);
        }
    }

    void OnGUI()
    {
        var rect = Screen.safeArea;
        rect.width = 250;
        using(new GUILayout.AreaScope(rect, "", GUI.skin.box))
        {
            GUILayout.Label($"BranchingBench(Texture fetch){Time.frameCount}");
            GUILayout.Space(10);

            foreach (var g in averageUs.GroupBy(x => x.Key.mode))
            {
                GUILayout.Label($"{g.Key}");
                using(new GUILayout.HorizontalScope())
                {
                    foreach (var (type, entry) in g)
                    {
                        GUILayout.Label($"{type.path} {entry.us:F2}us");
                    }
                }
            }
        }
        
    }
}

