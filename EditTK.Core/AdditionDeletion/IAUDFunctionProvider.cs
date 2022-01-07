namespace EditTK.Core.AdditionDeletion
{
    /// <summary>
    /// Provides functions for adding, updating and deleting items to/from a specific collection type with a specific key type
    /// </summary>
    /// <typeparam name="TCollection">The collection type</typeparam>
    /// <typeparam name="TKey">The key type</typeparam>
    public interface IAUDFunctionProvider<TCollection, TKey>
    {
        public void Add(TCollection collection, TKey key, object item);
        public void Update(TCollection collection, TKey key, object newValue);
        public TKey Delete(TCollection collection, object item);
    }
}
