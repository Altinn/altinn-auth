using System.Collections;

namespace Altinn.ResourceRegistry.TestUtils;

public class AsyncList<T>
    : IAsyncEnumerable<T>
    , IEnumerable<T>
{
    private readonly List<T> _list = new();

    public AsyncList()
    {
    }

    public AsyncList(List<T> values)
    {
        _list = values;
    }

    public AsyncList(IEnumerable<T> values)
    {
        _list = values.ToList();
    }

    public void Add(T value)
    {
        _list.Add(value);
    }

    public async IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
    {
        foreach (var item in _list)
        {
            await Task.Yield();
            yield return item;
        }
    }

    public IEnumerator<T> GetEnumerator()
    {
        return _list.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
