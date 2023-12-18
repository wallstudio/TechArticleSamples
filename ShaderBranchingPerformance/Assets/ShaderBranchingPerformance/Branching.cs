using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;

public class Bra : MonoBehaviour
{
    const int N = 16 * 1024;

    enum BraType { STATIC, UNIFORM, DIVERGENT, }
    enum BranchType { FAST, SLOW, FLATTEN_FAST, }

    [SerializeField] ComputeShader cs;
    [SerializeField][Min(1)] int Slow_DivergentFastInterval = 1;

    GraphicsBuffer input, output;
    
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

        input = new GraphicsBuffer(GraphicsBuffer.Target.Structured, N, sizeof(float));
        input.SetData(Enumerable.Range(0, N)
            .Select(i => (float)i / N)
            .ToArray());
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
                cmd.SetComputeBufferParam(cs, kernel, "_Input", input);
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

        GetComponent<Renderer>().material.SetBuffer("_Buff", output);
        GetComponent<Renderer>().material.SetInteger("_N", N);
    }

    void OnGUI()
    {
        var rect = Screen.safeArea;
        rect.width = 250;
        using(new GUILayout.AreaScope(rect, "", GUI.skin.box))
        {
            GUILayout.Label($"{Time.frameCount}");
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

