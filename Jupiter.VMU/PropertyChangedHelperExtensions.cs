using System.ComponentModel;
using System.Linq.Expressions;

namespace Jupiter.VMU
{
    /// <summary>
    /// Provides extension methods to subscribe to <see cref="INotifyPropertyChanged.PropertyChanged"/>.
    /// </summary>
    public static class PropertyChangedHelperExtensions
    {
        /// <summary>
        /// Subscribes to a property update event.
        /// </summary>
        /// <typeparam name="R">The type of the property return value.</typeparam>
        /// <param name="source">The source for the <see cref="INotifyPropertyChanged.PropertyChanged"/> event.</param>
        /// <param name="propertyAccessor">The property accessor.</param>
        /// <param name="action">The action to execute </param>
        /// <param name="initialUpdate">Determines if the subscriber should receive a update upon subscription.</param>
        /// <returns>The <see cref="PropertyChangedHelper{T}"/> for further subscriptions.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the getter is null.</exception>
        /// <exception cref="ArgumentException">Thrown when the expression isn't a member expression.</exception>
        /// <exception cref="NotSupportedException">Thrown when a anonymous type is the target of the <paramref name="action"/>.</exception>
        public static PropertyChangedHelper<T> Subscribe<T, R>(this T source, Expression<Func<T, R>> propertyAccessor, Action<R> action, Boolean initialUpdate = false)
            where T : class, INotifyPropertyChanged
        {
            return PropertyChangedHelper<T>.GetHelper(source)
                .Subscribe(propertyAccessor, action, initialUpdate);
        }

        /// <summary>
        /// Subscribes to a property update event.
        /// </summary>
        /// <typeparam name="R">The type of the property return value.</typeparam>
        /// <param name="source">The source for the <see cref="INotifyPropertyChanged.PropertyChanged"/> event.</param>
        /// <param name="propertyAccessor">The property accessor.</param>
        /// <param name="action">The action to execute </param>
        /// <param name="initialUpdate">Determines if the subscriber should receive a update upon subscription.</param>
        /// <returns>The <see cref="PropertyChangedHelper{T}"/> for further subscriptions.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the getter is null.</exception>
        /// <exception cref="ArgumentException">Thrown when the expression isn't a member expression.</exception>
        /// <remarks>Allows anonymous types as delegate targets for <paramref name="action"/>.</remarks>
        public static PropertyChangedHelper<T> SubscribeUnsafe<T, R>(this T source, Expression<Func<T, R>> propertyAccessor, Action<R> action, Boolean initialUpdate = false)
            where T : class, INotifyPropertyChanged
        {
            return PropertyChangedHelper<T>.GetHelper(source)
                .SubscribeUnsafe(propertyAccessor, action, initialUpdate);
        }

        /// <summary>
        /// Gets the <see cref="PropertyChangedHelper{T}"/> for the specified instance.
        /// </summary>
        /// <param name="source">The source to listen to.</param>
        /// <returns>The <see cref="PropertyChangedHelper{T}"/> which has been generated.</returns>
        public static PropertyChangedHelper<T> GetHelper<T>(this T source)
            where T : class, INotifyPropertyChanged
        {
            return PropertyChangedHelper<T>.GetHelper(source);
        }

        /// <summary>
        /// Gets the <see cref="PropertyChangedHelper{T}"/> for the specified instance if any helper exists.
        /// </summary>
        /// <param name="source">The source to listen to.</param>
        /// <param name="helper">The helper if any could be found.</param>
        /// <returns>A boolean indicating whether a helper was found; otherwise false.</returns>
        public static bool TryGetHelper<T>(this T source, out PropertyChangedHelper<T> helper)
            where T : class, INotifyPropertyChanged
        {
            return PropertyChangedHelper<T>.TryGetHelper(source, out helper);
        }

        /// <summary>
        /// Unsubscribes from listening to the <see cref="INotifyPropertyChanged.PropertyChanged"/> event
        /// and clears all listeners and removes the instance from the cache.
        /// </summary>
        /// <param name="source">The source to unsubscribe from.</param>
        public static void Unsubscribe<T>(this T source)
            where T : class, INotifyPropertyChanged
        {
            if (source.TryGetHelper(out PropertyChangedHelper<T> helper))
            {
                helper.Unsubscribe();
            }
        }

        /// <summary>
        /// Unsubscribes from listening to any update for the <paramref name="target"/> object.
        /// </summary>
        /// <param name="source">The source for which the <paramref name="target"/> should unsubscribe.</param>
        /// <param name="target">The target for which the subscriptions should be removed.</param>
        public static void Unsubscribe<T>(this T source, object target)
            where T : class, INotifyPropertyChanged
        {
            if (source.TryGetHelper(out PropertyChangedHelper<T> helper))
            {
                helper.Unsubscribe(target);
            }
        }
    }
}
