using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using PlasticGui.Configuration.OAuth;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements;

namespace Uphash
{
    public interface IGpuEnumerable<T> : IEnumerable<T> where T : unmanaged
    {
    }


    public static class GpuLinq
    {
        static readonly Lazy<ShaderLibrary> shaderLibrary = new(ShaderLibrary.Load);

        public static IGpuEnumerable<T> AsGpuEnumerable<T>(this IEnumerable<T> source) where T : unmanaged => new GpuLinqEnumerable<T>(source);

        public static IGpuEnumerable<T> Where<T>(
            this IGpuEnumerable<T> source, Func<T, bool> _,
            [CallerFilePath] string file = null, [CallerMemberName] string member = null, [CallerLineNumber] int line = 0)
            where T : unmanaged
        {
            return new LazyEnumerable<T>("Where", new (() => Dispatch<T, T>(source, useCounter: true, file, member, line)));
        }

        public static IGpuEnumerable<U> Select<T, U>(
            this IGpuEnumerable<T> source, Func<T, U> _,
            [CallerFilePath] string file = null, [CallerMemberName] string member = null, [CallerLineNumber] int line = 0)
            where T : unmanaged where U : unmanaged
        {
            return new LazyEnumerable<U>("Select", new (() => Dispatch<T, U>(source, useCounter: false, file, member, line)));
        }

        static unsafe U[] Dispatch<T, U>(
            IGpuEnumerable<T> source, bool useCounter,
            string file, string member, int line)
            where T : unmanaged where U : unmanaged
        {
            var inputArr = source.ToArray();
            using var input = new GraphicsBuffer(GraphicsBuffer.Target.Structured, inputArr.Length, sizeof(T));
            input.SetData(inputArr);
            using var output = new GraphicsBuffer(GraphicsBuffer.Target.Structured | GraphicsBuffer.Target.Append, inputArr.Length, sizeof(U));
            using var immCounter = new GraphicsBuffer(GraphicsBuffer.Target.IndirectArguments, 4, sizeof(int));
            using (var cmd = new CommandBuffer() { name = $"{file}:{member}:{line-1}" })
            {
                cmd.SetBufferCounterValue(output, 0);

                var (shader, code) = shaderLibrary.Value.Resolve(file, member, line-1);
                cmd.SetComputeBufferParam(shader, 0, "_Input", input);
                cmd.SetComputeBufferParam(shader, 0, "_Output", output);
                cmd.SetComputeIntParam(shader, "_InputLength", inputArr.Length);
                cmd.DispatchCompute(shader, 0, Mathf.CeilToInt(inputArr.Length / 64f), 1, 1);

                cmd.CopyCounterValue(output, immCounter, 0);
                Graphics.ExecuteCommandBuffer(cmd);
            }

            var countBuff = new int[1];
            immCounter.GetData(countBuff);
            var count = useCounter ? countBuff[0] : inputArr.Length;

            var outputArr = new U[count];
            output.GetData(outputArr);
            return outputArr;
        }

        class GpuLinqEnumerable<T> : IGpuEnumerable<T> where T : unmanaged
        {
            public readonly IEnumerable<T> source;
            public GpuLinqEnumerable(IEnumerable<T> source) => this.source = source;
            public IEnumerator<T> GetEnumerator() => source.GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        readonly struct LazyEnumerable<T> : IGpuEnumerable<T> where T : unmanaged
        {
            readonly string Name;
            readonly Lazy<T[]> array;
            public LazyEnumerable(string name, Lazy<T[]> array) => (this.array, Name) = (array, name);
            public readonly IEnumerator<T> GetEnumerator() => array.Value.AsEnumerable().GetEnumerator();
            readonly IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
    }
}
