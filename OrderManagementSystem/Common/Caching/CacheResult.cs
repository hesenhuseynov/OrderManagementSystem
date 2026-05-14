using System.Data.SqlTypes;

namespace OrderManagementSystem.Common.Caching
{

    public readonly record struct CacheResult<T>
    {
        public bool IsHit { get; }

        public T? Value { get; }


        private CacheResult(bool isHit, T? value)
        {
            IsHit = isHit;
            Value = value;  
        }

        public static CacheResult<T> Hit(T value) => new(true, value);

        public static CacheResult<T> Miss() => new(false, default);
    }
}
