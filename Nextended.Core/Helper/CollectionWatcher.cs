using System;
using System.Collections;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Nextended.Core.Types;

namespace Nextended.Core.Helper
{
    /// <summary>
    /// Überwacht ob sich eine Collection ändert und wirft event
    /// </summary>
    public class CollectionWatcher<T> : IDisposable

    {
        private readonly ICollection collectionToWatch;

        /// <summary>
        /// Gibt zurük, ob der CollectionWatcher am überwachen ist
        /// </summary>
        public bool IsWatching { get; private set; }

        /// <summary>
        /// Wird ausgelöst, wenn sich der Count der Collection ändert
        /// </summary>
        public event EventHandler<EventArgs<ICollection>> CountChanged;

        /// <summary>
        /// Wird ausgelöst, wenn ein Eintrag hinzugefügt wird
        /// </summary>
        public event EventHandler<EventArgs<T>> ItemAdded;    
        
        /// <summary>
        /// Wird ausgelöst, wenn ein Eintrag entfernt wird
        /// </summary>
        public event EventHandler<EventArgs<ICollection>> ItemRemoved;


        /// <summary>
        /// Initializes a new instance of the <see cref="CollectionWatcher&lt;T&gt;"/> class.
        /// </summary>
        public CollectionWatcher(ICollection collection, bool autoStartWatching)
        {
            collectionToWatch = collection;
            if (autoStartWatching)
                StartWatching();
        }        
        
        /// <summary>
        /// Initializes a new instance of the <see cref="CollectionWatcher&lt;T&gt;"/> class.
        /// </summary>
        public CollectionWatcher(ICollection collection):this(collection,false)
        {}

        /// <summary>
        /// Startet das überwachen der Collection
        /// </summary>
        public void StartWatching()
        {
            IsWatching = true;
            Task.Factory.StartNew(WatchCollectionCount);
        }

        /// <summary>
        /// Startet das überwachen der Collection
        /// </summary>
        public void StopWatching()
        {
            IsWatching = false;
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            IsWatching = false;
        }

        private void WatchCollectionCount()
        {

            int lastCount = collectionToWatch.Count;
            while (IsWatching)
            {

                var count = collectionToWatch.Count;

                if (count != lastCount)
                {
                    var eventArgs = new EventArgs<ICollection>(collectionToWatch);
                    if (lastCount < count)
                    {
                        try
                        {
                            T obj = collectionToWatch.OfType<T>().Last();
                            var addedEventArgs = new EventArgs<T>(obj);
                            InvokeAdd(addedEventArgs);
                        }
                        catch (Exception e)
                        {
                            Trace.TraceError(e.Message);
                        }
                    }
                    else if (lastCount > count)
                        InvokeRemove(eventArgs);
                    
                    InvokeCountChanged(eventArgs);                  
                    lastCount = count;
                }
                Thread.Sleep(10);
            }
        }

        private void InvokeCountChanged(EventArgs<ICollection> e)
        {
            EventHandler<EventArgs<ICollection>> handler = CountChanged;
            if (handler != null)
            {
                handler(this, e);
            }
        }       
        
        private void InvokeAdd(EventArgs<T> e)
        {
            EventHandler<EventArgs<T>> handler = ItemAdded;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        private void InvokeRemove(EventArgs<ICollection> e)
        {
            EventHandler<EventArgs<ICollection>> handler = ItemRemoved;
            if (handler != null)
            {
                handler(this, e);
            }
        }

    }
}
