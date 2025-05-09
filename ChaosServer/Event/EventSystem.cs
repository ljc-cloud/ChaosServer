using System;
using System.Collections.Generic;

namespace ChaosServer.Event
{
    public class EventSystem
    {
        private static readonly object Lock = new();
        private static EventSystem _mInstance;
        public static EventSystem Instance {
            get
            {
                lock (Lock)
                {
                    if (_mInstance == null)
                    {
                        _mInstance = new EventSystem();
                    }
                }
                return _mInstance;
            }
        }
        
        private readonly Dictionary<Type, Delegate> _mEvents = new();
        private readonly Dictionary<Type, List<IEvent>> _mEventPool = new();
        
        public void Subscribe<T>(Action<T> handler) where T : IEvent
        {
            // string key = GetKey(handler);

            var @event = typeof(T);

            if (_mEventPool.TryGetValue(@event, out var eventList))
            {
                foreach (var e in eventList)
                {
                    handler.Invoke((T)e);
                }
                _mEventPool[@event].Clear();
            }
            
            if (_mEvents.TryGetValue(@event, out var existHandlers))
            {
                _mEvents[@event] = Delegate.Combine(existHandlers, handler);
            }
            else
            {
                _mEvents[@event] = handler;
            }
        }

        public void Unsubscribe<T>(Action<T> handler) where T : IEvent
        {
            // string key = GetKey(handler);
            var key = typeof(T);
            if (_mEvents.TryGetValue(key, out var existHandlers))
            {
                Delegate newHandlers = Delegate.Remove(existHandlers, handler);
                if (newHandlers == null)
                {
                    _mEvents.Remove(key);
                }
                else
                {
                    _mEvents[key] = newHandlers;
                }
            }
        }

        public void Publish<T>(T e) where T : IEvent
        {
            var @event = typeof(T);
            if (_mEvents.TryGetValue(@event, out var existHandlers))
            {
                (existHandlers as Action<T>)?.Invoke(e);
            }
            if (_mEventPool.TryGetValue(@event, out var eventList))
            {
                eventList.Add(e);
            }
            else
            {
                _mEventPool[@event] = new List<IEvent> { e };
            }
        }

        public void Publish<T>() where T : IEvent, new()
        {
            T e = new T();
            var @event = typeof(T);
            if (_mEvents.TryGetValue(@event, out var existHandlers))
            {
                (existHandlers as Action<T>)?.Invoke(e);
            }
            if (_mEventPool.TryGetValue(@event, out var eventList))
            {
                eventList.Add(e);
            }
            else
            {
                _mEventPool[@event] = new List<IEvent> { e };
            }
        }
    }
}