using System.ComponentModel;
using System.Linq.Expressions;

namespace Jupiter.VMU
{
    /// <summary>
    /// Provides extension methods to subscribe to 
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
        /// <returns>The <see cref="PropertyChangedHelper{T}"/> for further subscriptions.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the getter is null.</exception>
        /// <exception cref="ArgumentException">Thrown when the expression isn't a member expression.</exception>
        /// <exception cref="NotSupportedException">Thrown when a anonymous type is the target of the <paramref name="action"/>.</exception>
        public static PropertyChangedHelper<T> Subscribe<T, R>(this T source, Expression<Func<T, R>> propertyAccessor, Action<R> action)
            where T : class, INotifyPropertyChanged
        {
            return PropertyChangedHelper<T>.GetHelper(source)
                .Subscribe(propertyAccessor, action);
        }

        /// <summary>
        /// Subscribes to a property update event.
        /// </summary>
        /// <typeparam name="R">The type of the property return value.</typeparam>
        /// <param name="source">The source for the <see cref="INotifyPropertyChanged.PropertyChanged"/> event.</param>
        /// <param name="propertyAccessor">The property accessor.</param>
        /// <param name="action">The action to execute </param>
        /// <returns>The <see cref="PropertyChangedHelper{T}"/> for further subscriptions.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the getter is null.</exception>
        /// <exception cref="ArgumentException">Thrown when the expression isn't a member expression.</exception>
        /// <remarks>Allows anonymous types as delegate targets for <paramref name="action"/>.</remarks>
        public static PropertyChangedHelper<T> SubscribeUnsafe<T, R>(this T source, Expression<Func<T, R>> propertyAccessor, Action<R> action)
            where T : class, INotifyPropertyChanged
        {
            return PropertyChangedHelper<T>.GetHelper(source)
                .SubscribeUnsafe(propertyAccessor, action);
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
        /// Unsubscribes from listening to the <see cref="INotifyPropertyChanged.PropertyChanged"/> event
        /// and clears all listeners and removes the instance from the cache.
        /// </summary>
        /// <param name="source">The source to unsubscribe from.</param>
        public static void Unsubscribe<T>(this T source)
            where T : class, INotifyPropertyChanged
        {
            source.GetHelper().Unsubscribe();
        }
    }
}