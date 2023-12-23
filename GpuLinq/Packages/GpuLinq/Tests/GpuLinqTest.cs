using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Uphash;
using Debug = UnityEngine.Debug;

public class GpuLinqTest
{
    [Test]
    public void GpuLinqTestSimplePasses()
    {
        var source = Enumerable.Range(1, 1_000_000).ToArray();
        
        var cpuSw = Stopwatch.StartNew();
        var cpu = source
            .Where(x => (uint)x % 2 == 0)
            .Select(x => x * 2)
            .ToArray();
        Debug.Log($"cpu: {cpuSw.ElapsedMilliseconds}ms");

        var gpuSw = Stopwatch.StartNew();
        var gpu = source
            .AsGpuEnumerable()
            .Where(x => (uint)x % 2 == 0) // unorderd
            .Select(x => x * 2)
            .ToArray();
        Debug.Log($"gpu: {gpuSw.ElapsedMilliseconds}ms");

        Assert.IsTrue(new HashSet<int>(cpu).SetEquals(gpu));
    }
}
