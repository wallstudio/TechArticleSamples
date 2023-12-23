using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using DotNext.Runtime;

static class Program
{
    // static ref int reference; // コンパイル不可
    static Reference<int> reference; // OK
    static void Main()
    {
        var hoge = new Hoge();
        reference = Reference2.Create(hoge, static h => ref h._field);
        
        reference.Target = 20;
        Console.WriteLine(hoge._field); // 20
    }
}

class Hoge
{
    public int _field = 0;
}

static class Reference2
{
    public delegate ref TValue Accessor<TOwner, TValue>(TOwner owner) where TOwner : class;
    
    public static unsafe Reference<TValue> Create<TOwner, TValue>(TOwner owner, Accessor<TOwner, TValue> accessor) where TOwner : class
    {
        var s = Marshal.GetFunctionPointerForDelegate(accessor);
        var p = (delegate*<TOwner, ref TValue>)s;
        return Reference.Create(owner, p);
    }
}