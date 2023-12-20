using System.Runtime.InteropServices;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;

class Legacy
{
    public static void Run()
    {
        Flex
        <
            Vector3, // Position
            Flex
            <
                Vector2, // UV
                Vector4 // Color
            >
        > vertex0, vertex1, vertex2;
        vertex0 = new(new Vector3(0, 0, 0), new(new Vector2(0, 0), new Vector4(1, 0, 0, 1)));
        vertex1 = new(new Vector3(1, 0, 0), new(new Vector2(1, 0), new Vector4(0, 1, 0, 1)));
        vertex2 = new(new Vector3(0, 1, 0), new(new Vector2(0, 1), new Vector4(0, 0, 1, 1)));
        {
            var vertex_012 = Lerp(vertex0, vertex1, 0.5f, vertex2, 0.5f);
            Console.WriteLine($"Size of {vertex_012.GetType()} is {Marshal.SizeOf(vertex_012)}\n[{vertex_012}]");
        }

        {
            var typeList = new List<Type>() 
            {
                typeof(Vector3), typeof(Vector2), typeof(Vector4),
            };
            Type vertexType = typeList.AsEnumerable()
                .Reverse()
                .Skip(2)
                .Aggregate(typeof(Flex<,>)
                .MakeGenericType(typeList[^2], typeList[^1]), (acc, t) => typeof(Flex<,>).MakeGenericType(t, acc));
            var vertex_012 = typeof(Legacy).GetMethod(nameof(Lerp), BindingFlags.Static | BindingFlags.NonPublic)!
                .MakeGenericMethod(vertexType)
                .Invoke(null, [vertex0, vertex1, 0.5f, vertex2, 0.5f, ]);    
            Console.WriteLine($"Size of {vertex_012!.GetType()} is {Marshal.SizeOf(vertex_012)}\n[{vertex_012}]");
        }
    }

    static T Lerp<T>(T a, T b, float s, T c, float t) where T : unmanaged, ILerpable<T>
        => a.Lerp(b, s, c, t);

    interface ILerpable<T> where T : unmanaged
    {
        public T Get();
        public T Lerp(in T b, float s, in T c, float t);
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct Flex<T, U> : ILerpable<Flex<T, U>>
        where T : unmanaged
        where U : unmanaged
    {
        public readonly T Value;
        public readonly U Next;
        public Flex(T value, U next) => (Value, Next) = (value, next);

        public readonly Flex<T, U> Get() => this;

        public Flex<T, U> Lerp(in Flex<T, U> b, float s, in Flex<T, U> c, float t)
        {
            var value = Lerp(Value, b.Value, s, c.Value, t);
            var next = Lerp(Next, b.Next, s, c.Next, t);
            return new(value, next);
            
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static unsafe V Lerp<V>(V a, V b, float s, V c, float t) where V : unmanaged
            {
                V result;
                switch((a, b, c))
                {
                    case (ILerpable<V> a_l, ILerpable<V> b_l, ILerpable<V> c_l):
                        result = a_l.Lerp(b_l.Get(), s, c_l.Get(), t);
                        break;
                    case (float a_32, float b_32, float c_32):
                        var r32 = a_32 + b_32 * s + c_32 * t;
                        result = *(V*)&r32;
                        break;
                    case (double a_64, double b_64, double c_64):
                        var r64 = a_64 + b_64 * s + c_64 * t;
                        result = *(V*)&r64;
                        break;
                    case (Vector2 a_v2, Vector2 b_v2, Vector2 c_v2):
                        var r_v2 = a_v2 + b_v2 * s + c_v2 * t;
                        result = *(V*)&r_v2;
                        break;
                    case (Vector3 a_v3, Vector3 b_v3, Vector3 c_v3):
                        var r_v3 = a_v3 + b_v3 * s + c_v3 * t;
                        result = *(V*)&r_v3;
                        break;
                    case (Vector4 a_v4, Vector4 b_v4, Vector4 c_v4):
                        var r_v4 = a_v4 + b_v4 * s + c_v4 * t;
                        result = *(V*)&r_v4;
                        break;
                    default:
                        Throw<V>();
                        result = default;
                        break;
                }
                return result;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static void Throw<V>() => throw new NotSupportedException($"{typeof(V)} is not supported");
        }

        public override string ToString() => $"{Value}, {Next}";

    }
}