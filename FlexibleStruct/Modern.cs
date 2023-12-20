using System.Runtime.InteropServices;
using System.Numerics;
using System.Reflection;

class Modern
{
    public static void Run()
    {
        Flex
        <
            float, // Position.x
            Flex
            <
                float, // Position.y
                Flex
                <
                    float, // Position.z
                    Flex
                    <
                        float, // UV.x
                        float // UV.y
                    >
                >
            >
        > vertex0, vertex1, vertex2;
        vertex0 = new(0, new(0, new(0, new(0, 0))));
        vertex1 = new(1, new(0, new(0, new(1, 0))));
        vertex2 = new(0, new(1, new(0, new(0, 1))));
        {
            var vertex_012 = Lerp(vertex0, vertex1, 0.5f, vertex2, 0.5f);
            Console.WriteLine($"Size of {vertex_012.GetType()} is {Marshal.SizeOf(vertex_012)}\n[{vertex_012}]");
        }

        {
            var typeList = new List<Type>()
            {
                typeof(float), typeof(float), typeof(float),
                typeof(float), typeof(float),
            };
            Type vertexType = typeList.AsEnumerable()
                .Reverse()
                .Skip(2)
                .Aggregate(typeof(Flex<,>)
                .MakeGenericType(typeList[^2], typeList[^1]), (acc, t) => typeof(Flex<,>).MakeGenericType(t, acc));
            var vertex_012 = typeof(Modern).GetMethod(nameof(Lerp), BindingFlags.Static | BindingFlags.NonPublic)!
                .MakeGenericMethod(vertexType)
                .Invoke(null, [vertex0, vertex1, 0.5f, vertex2, 0.5f, ]);    
            Console.WriteLine($"Size of {vertex_012!.GetType()} is {Marshal.SizeOf(vertex_012)}\n[{vertex_012}]");
        }
    }

    static T Lerp<T>(T a, T b, float s, T c, float t) where T : unmanaged, ILerpable<T>
        => a.Lerp(b, s, c, t);


    interface ILerpable<T> where T : unmanaged
    {
        public T Lerp(in T b, float s, in T c, float t);
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct Flex<T, U> : ILerpable<Flex<T, U>>,
        IAdditionOperators<Flex<T, U>, Flex<T, U>, Flex<T, U>>,
        IMultiplyOperators<Flex<T, U>, float, Flex<T, U>>
        where T : unmanaged, IAdditionOperators<T, T, T>, IMultiplyOperators<T, float, T>
        where U : unmanaged, IAdditionOperators<U, U, U>, IMultiplyOperators<U, float, U>
    {
        public readonly T Value;
        public readonly U Next;
        public Flex(T value, U next) => (Value, Next) = (value, next);

        public Flex<T, U> Lerp(in Flex<T, U> b, float s, in Flex<T, U> c, float t)
            => new (Value + b.Value * s + c.Value * t, Next + b.Next * s + c.Next * t);
        public static Flex<T, U> operator +(Flex<T, U> left, Flex<T, U> right)
            => new(left.Value + right.Value, left.Next + right.Next);
        public static Flex<T, U> operator *(Flex<T, U> left, float weight)
            => new(left.Value * weight, left.Next * weight);

        public override string ToString() => $"{Value}, {Next}";
    }
}