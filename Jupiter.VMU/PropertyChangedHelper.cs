using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Jupiter.VMU
{
    /// <summary>
    /// Provides a way to subscribe to the <see cref="INotifyPropertyChanged.PropertyChanged"/> event.
    /// </summary>
    internal sealed class PropertyChangedHelper
    {
        #region #### VARIABLES ##########################################################
        static readonly ConditionalWeakTable<INotifyPropertyChanged, PropertyChangedHelper> CachedHelpers = new ConditionalWeakTable<INotifyPropertyChanged, PropertyChangedHelper>();

        readonly ConcurrentDictionary<string, IList<Listener>> _PropertyChangedListeners = new ConcurrentDictionary<string, IList<Listener>>();
        readonly INotifyPropertyChanged _Source;
        #endregion
        #region #### CTOR ###############################################################
        private PropertyChangedHelper(INotifyPropertyChanged source)
        {
            _Source = source;
            source.PropertyChanged += Target_PropertyChanged;
        }
        #endregion
        #region #### PUBLIC #############################################################
        /// <summary>
        /// Gets the <see cref="PropertyChangedHelper"/> for the specified instance.
        /// </summary>
        /// <param name="source">The source to listen to.</param>
        /// <returns>The <see cref="PropertyChangedHelper"/> which has been generated.</returns>
        public static PropertyChangedHelper GetHelper(INotifyPropertyChanged source)
        {
            /* lock cached helpers as ConditionalWeakTable possible can create values
             * even if the value isn't later used anymore */
            lock (CachedHelpers)
            {
                return CachedHelpers.GetValue(source, k => new PropertyChangedHelper(k));
            }
        }

        /// <summary>
        /// Gets the <see cref="PropertyChangedHelper{T}"/> for the specified instance if any helper exists.
        /// </summary>
        /// <param name="source">The source to listen to.</param>
        /// <param name="helper">The helper if any could be found.</param>
        /// <returns>A boolean indicating whether a helper was found; otherwise false.</returns>
        public static bool TryGetHelper(INotifyPropertyChanged source, [MaybeNullWhen(false)] out PropertyChangedHelper? helper)
        {
            return CachedHelpers.TryGetValue(source, out helper);
        }

        /// <summary>
        /// Unsubscribes from listening to the <see cref="INotifyPropertyChanged.PropertyChanged"/> event
        /// and clears all listeners and removes the instance from the cache.
        /// </summary>
        public void Unsubscribe()
        {
            if (CachedHelpers.Remove(_Source))
            {
                _Source.PropertyChanged -= Target_PropertyChanged;
                _PropertyChangedListeners.Clear();
            }
        }

        /// <summary>
        /// Unsubscribes from listening to any update for the <paramref name="target"/> object.
        /// </summary>
        /// <param name="target">The target for which the subscriptions should be removed.</param>
        public void Unsubscribe(object target)
        {
            foreach (var listeners in _PropertyChangedListeners.Values)
            {
                Listener[] list;
                // Copy listeners to prevent any deadlocks
                lock (listeners) list = listeners.ToArray();

                List<Listener> listenersToRemove = new List<Listener>();
                foreach (var item in list)
                {
                    if (item.IsTargetOrCollected(target))
                    {
                        listenersToRemove.Add(item);
                    }
                }

                lock (listeners)
                {
                    foreach (var item in listenersToRemove)
                    {
                        listeners.Remove(item);
                    }
                }
            }
        }

        /// <summary>
        /// Adds a listener for the specified property.
        /// </summary>
        /// <param name="propertyName">The name of the property to listen to.</param>
        /// <param name="listener">The listener to add.</param>
        /// <param name="initialUpdate">Determines if the subscriber should receive a update upon subscription.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="listener"/> or <paramref name="propertyName"/> is null.</exception>
        public void AddListener(String propertyName, Listener listener, Boolean initialUpdate = false)
        {
            if (propertyName == null) throw new ArgumentNullException(nameof(propertyName));
            if (listener == null) throw new ArgumentNullException(nameof(listener));

            IList<Listener> listeners = _PropertyChangedListeners.GetOrAdd(propertyName, p => new List<Listener>());

            lock (listeners)
            {
                listeners.Add(listener);
            }

            if (initialUpdate)
            {
                listener.OnPropertyChanged(_Source, new List<Listener>());
            }
        }
        #endregion
        #region #### PRIVATE ############################################################
        /// <summary>
        /// Handles when <see cref="INotifyPropertyChanged.PropertyChanged"/> has been raised.
        /// </summary>
        /// <param name="sender">The sender of the event.</param>
        /// <param name="e">The event arguments.</param>
        void Target_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != null && _PropertyChangedListeners.TryGetValue(e.PropertyName, out var listeners))
            {
                Listener[] list;
                // Copy listeners to prevent any deadlocks
                lock (listeners) list = listeners.ToArray();

                List<Listener> deadListeners = new List<Listener>();
                foreach (var item in list)
                {
                    item.OnPropertyChanged(_Source, deadListeners);
                }

                lock (listeners)
                {
                    foreach (var item in deadListeners)
                    {
                        listeners.Remove(item);
                    }
                }
            }
        }
        #endregion
        #region #### NESTED TYPES #######################################################
        /// <summary>
        /// Defines a listener to a property changed event.
        /// </summary>
        public abstract class Listener
        {
            /// <summary>
            /// Checks whether the target of the listener is collected or matches <paramref name="target"/>.
            /// </summary>
            /// <param name="target">The target to check.</param>
            /// <returns>True when the target is </returns>
            public abstract bool IsTargetOrCollected(object target);

            /// <summary>
            /// Called when the target property has been changed.
            /// </summary>
            /// <param name="source">The source object.</param>
            /// <param name="listeners">The list used to add listeners to which lost their reference.</param>
            public abstract void OnPropertyChanged(object source, IList<Listener> listeners);
        }
        #endregion
    }
}