
namespace ChaosServer.Utility;

public class ObjectPool<T> where T : class
{
    private readonly Logger _mLogger;
    private struct Element
    {
        public T value;
    }

    private T _mFirstItem;
    private readonly Element[] _mElementItems;
    
    private readonly Func<T> _mFactory;

    public ObjectPool(Func<T> factory) : this(factory, Environment.ProcessorCount * 2) {}

    public ObjectPool(Func<T> mFactory, int size)
    {
        _mLogger = new Logger(GetType());
        _mFactory = mFactory;
        _mElementItems = new Element[size - 1];
        
    }

    private T CreateInstance()
    {
        return _mFactory();
    }
    
    public T Allocate()
    {
        T inst = _mFirstItem;
        if (inst == null || inst != Interlocked.CompareExchange(ref _mFirstItem, null, inst))
        {
            inst = AllocateSlow();
        }
        return inst;
    }

    private T AllocateSlow()
    {
        Element[] items = _mElementItems;
        for (int i = 0; i < items.Length; i++)
        {
            T inst = items[i].value;
            if (inst != null)
            {
                if (inst == Interlocked.CompareExchange(ref items[i].value, null, inst))
                {
                    return inst;
                }
            }
        }
        return CreateInstance();
    }

    public void Release(T obj)
    {
        // Validate(obj);

        if (_mFirstItem == null)
        {
            _mFirstItem = obj;
        }
        else
        {
            ReleaseSlow(obj);
        }
    }

    private void ReleaseSlow(T obj)
    {
        Element[] items = _mElementItems;
        for (int i = 0; i < items.Length; i++)
        {
            if (items[i].value == null)
            {
                items[i].value = obj;
                break;
            }
        }
    }

    private void Validate(object obj)
    {
        if (obj == null)
        {
            _mLogger.LogError("free obj is null?");
            return;
        }

        if (_mFirstItem == obj)
        {
            _mLogger.LogError("free obj twice?");
            return;
        }

        var items = _mElementItems;
        for (int i = 0; i < items.Length; i++)
        {
            var value = items[i].value;
            if (value == null)
            {
                return;
            }

            if (value == obj)
            {
                _mLogger.LogError("free obj twice?");
            }
        }
    }
    
}