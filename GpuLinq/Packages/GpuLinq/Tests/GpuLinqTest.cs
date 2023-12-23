using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Uphash;

public class GpuLinqTest
{
    [Test]
    public void GpuLinqTestSimplePasses()
    {
        Debug.Log(typeof(GpuLinq).AssemblyQualifiedName);
    }
}
