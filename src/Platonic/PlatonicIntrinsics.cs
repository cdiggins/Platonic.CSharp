namespace PlatonicIntrinsics
{
    public class MutableAttribute : Attribute { }
    public class ImmutableAttribute : Attribute { }

    public class ImpureWrite : Attribute { }
    public class ImpureEffectAttribute : Attribute { }
    
    [Immutable]
    public interface IObject
    {
        int GetHashCode();
        String ToString(); 
    }

    [Immutable]
    public interface IString : IObject
    { }

    [Immutable]
    public class Type 
    { }

    [Immutable]
    public static class Math 
    { }

    [Immutable]
    public static class File
    { }

    [Immutable]
    public static class Path
    { }

    [Immutable]
    public interface IEnumerator<T>
    {
        T Current { get; }

        [ImpureWrite]
        void MoveNext();
    }

    [Immutable]
    public interface IEnumerable<T>
    {
        public IEnumerator<T> GetEnumerator();
    }

    [Immutable]
    public interface IReadOnlyList<T>
    {
        T[] Data { get; }
        int Count { get; }
        T this[int index] { get; }
    }

    public static class Extensions
    {
        [ImpureEffect]
        public static void CopyTo<T>(this IReadOnlyList<T> self, IMutableArray<T> xs, int srcOffset, int destOffset, int count)
        {
            for (var i = 0; i < count; i++)
            {
                xs.Set(i + destOffset, self[i + srcOffset]);
            }
        }
    }

    [Mutable]
    public interface IMutableArray<T>
    {
        int Count { get; }
        T this[int index] { get; }
        void Set(int index, T value);
    }

    [Mutable]
    public class MutableArray<T>
    {
        public T[] Data { get; }
        public int Count => Data.Length;
        public T this[int index] => Data[index];

        public void Set(int index, T value)
        {
            Data[index] = value;
        }

        public MutableArray(int count)
        {
            Data = new T[count];
        }
    }

    [Mutable]
    public interface IList<T>
    {
        [ImpureWrite]
        void Add(T item);
    }

    [Mutable]
    public class List<T> : IList<T>
    {
        public IMutableArray<T> Array { get; private set; }
        public int Count { get; private set; }
        
        [ImpureWrite]
        public void Add(T item)
        {
            Count += 1;
            if (Count > Array.Count)
            {
                var newArray = new MutableArray<T>(Array.Count * 2 );
                // 
            }
            throw new NotImplementedException();
        }
    }

    [Mutable]
    public class Stream
    {
        // Read
        // Write 
    }
}