using System.Collections.Concurrent;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Jupiter.VMU
{
    /// <summary>
    /// Provides helper methods for subscription to the <see cref="INotifyPropertyChanged.PropertyChanged"/> event.
    /// </summary>
    /// <typeparam name="T">The type which raises the event.</typeparam>
    /// <remarks>If <typeparamref name="T"/> does not provide a weak implementation of <see cref="INotifyPropertyChanged.PropertyChanged"/>, <see cref="Unsubscribe"/> must be called when the object is not longer in use.</remarks>
    public sealed partial class PropertyChangedHelper<T>
        where T : class, INotifyPropertyChanged
    {
        #region #### VARIABLES ##########################################################
        static readonly ConditionalWeakTable<T, PropertyChangedHelper<T>> CachedHelpers = new ConditionalWeakTable<T, PropertyChangedHelper<T>>();
        static readonly Type ListenerType = typeof(Listener<,>);

        readonly WeakReference<T> _Source;
        readonly ConcurrentDictionary<string, IList<Listener>> _PropertyChangedListeners = new ConcurrentDictionary<string, IList<Listener>>();
        #endregion
        #region #### CTOR ###############################################################
        /// <summary>
        /// Initalizes a new instance of the <see cref="PropertyChangedHelper{T}"/> class.
        /// </summary>
        /// <param name="source">The source to listen to.</param>
        private PropertyChangedHelper(T source)
        {
            source.PropertyChanged += Target_PropertyChanged;
            _Source = new WeakReference<T>(source);
        }
        #endregion
        #region #### PUBLIC #############################################################
        /// <summary>
        /// Unsubscribes from listening to the <see cref="INotifyPropertyChanged.PropertyChanged"/> event
        /// and clears all listeners and removes the instance from the cache.
        /// </summary>
        public void Unsubscribe()
        {
            
            if (_Source.TryGetTarget(out T target) &&
                CachedHelpers.Remove(target))
            {
                target.PropertyChanged -= Target_PropertyChanged;
                _PropertyChangedListeners.Clear();
            }
        }

        /// <summary>
        /// Subscribes to a property update event.
        /// </summary>
        /// <typeparam name="R">The type of the property return value.</typeparam>
        /// <param name="propertyAccessor">The property accessor.</param>
        /// <param name="action">The action to execute </param>
        /// <returns>The <see cref="PropertyChangedHelper{T}"/> for further subscriptions.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the getter is null.</exception>
        /// <exception cref="ArgumentException">Thrown when the expression isn't a member expression.</exception>
        /// <exception cref="NotSupportedException">Thrown when a anonymous type is the target of the <paramref name="action"/>.</exception>
        public PropertyChangedHelper<T> Subscribe<R>(Expression<Func<T, R>> propertyAccessor, Action<R> action)
        {
            ValidateDelegate(action);
            return SubscribeUnsafe(propertyAccessor, action);
        }

        /// <summary>
        /// Subscribes to a property update event.
        /// </summary>
        /// <typeparam name="R">The type of the property return value.</typeparam>
        /// <param name="propertyAccessor">The property accessor.</param>
        /// <param name="action">The action to execute </param>
        /// <returns>The <see cref="PropertyChangedHelper{T}"/> for further subscriptions.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the getter is null.</exception>
        /// <exception cref="ArgumentException">Thrown when the expression isn't a member expression.</exception>
        /// <remarks>Allows anonymous types as delegate targets for <paramref name="action"/>.</remarks>
        public PropertyChangedHelper<T> SubscribeUnsafe<R>(Expression<Func<T, R>> propertyAccessor, Action<R> action)
        {
            String name = ValidateProperty(propertyAccessor);
            IList<Listener> listeners = _PropertyChangedListeners.GetOrAdd(name, p => new List<Listener>());
            lock (listeners)
            {
                if (action.Target == null)
                    listeners.Add(new ListenerStatic<R>(propertyAccessor.Compile(), action));
                else
                {
                    Type targetType = action.Target.GetType();
                    if (targetType.IsValueType)
                    {
                        throw new NotSupportedException("Structs are currently not supported");
                    }
                    else
                    {
                        listeners.Add((Listener)Activator.CreateInstance(
                            ListenerType.MakeGenericType(typeof(T), targetType, typeof(R)),
                            propertyAccessor.Compile(), action));
                    }
                    
                }
            }

            return this;
        }

        /// <summary>
        /// Gets the <see cref="PropertyChangedHelper{T}"/> for the specified instance.
        /// </summary>
        /// <param name="source">The source to listen to.</param>
        /// <returns>The <see cref="PropertyChangedHelper{T}"/> which has been generated.</returns>
        public static PropertyChangedHelper<T> GetHelper(T source)
        {
            /* lock cached helpers as ConditionalWeakTable possible can create values
             * even if the value isn't later used anymore */
            lock (CachedHelpers)
            {
                return CachedHelpers.GetValue(source, k => new PropertyChangedHelper<T>(k));
            }
        }
        #endregion
        #region #### PRIVATE ############################################################
        /// <summary>
        /// Validates the property and gets the name.
        /// </summary>
        /// <typeparam name="R">The return type of the property.</typeparam>
        /// <param name="getter">The getter to validate.</param>
        /// <returns>The name of the property.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the getter is null.</exception>
        /// <exception cref="ArgumentException">Thrown when the expression isn't a member expression.</exception>
        [StackTraceHidden]
        [DebuggerHidden]
        private string ValidateProperty<R>(Expression<Func<T, R>> getter)
        {
            // Validate getter expression 
            if (getter == null) throw new ArgumentNullException(nameof(getter));
            if (!(getter.Body is MemberExpression)) throw new ArgumentException(nameof(getter) + "." + nameof(getter.Body) + " is not a valid MemberExpression");

            // Cast body as member expression
            MemberExpression expression = (MemberExpression)getter.Body;
            // Validate member, must be a property
            if (!(expression.Member is PropertyInfo)) throw new ArgumentException("Expression must access a property");

            // Return the member name
            return expression.Member.Name;
        }

        /// <summary>
        /// Ensures that the type of the delegate isn't an anonymous.
        /// </summary>
        /// <param name="delegate">The delegate to check.</param>
        [StackTraceHidden]
        [DebuggerHidden]
        void ValidateDelegate(Delegate @delegate)
        {
            if (@delegate.Target is Object target)
            {
                Type type = target.GetType();
                if ((type.GetTypeInfo().Attributes & TypeAttributes.NotPublic) == TypeAttributes.NotPublic
                   && (type.Name.StartsWith("<>", StringComparison.OrdinalIgnoreCase) || type.Name.StartsWith("VB$", StringComparison.OrdinalIgnoreCase))
                   && type.GetTypeInfo().GetCustomAttributes(typeof(CompilerGeneratedAttribute)).Any())
                {
                    throw new NotSupportedException("Anonymous types can cause memory leaks.");
                }
            }
        }

        /// <summary>
        /// Handles when <see cref="INotifyPropertyChanged.PropertyChanged"/> has been raised.
        /// </summary>
        /// <param name="sender">The sender of the event.</param>
        /// <param name="e">The event arguments.</param>
        void Target_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (_PropertyChangedListeners.TryGetValue(e.PropertyName, out var listeners) &&
                _Source.TryGetTarget(out T target))
            {
                Listener[] list;
                // Copy listeners to prevent any deadlocks
                lock (listeners) list = listeners.ToArray();

                List<Listener> deadListeners = new List<Listener>();
                foreach (var item in list)
                {
                    item.OnPropertyChanged(target, deadListeners);
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
        abstract class Listener
        {
            /// <summary>
            /// Called when the target property has been changed.
            /// </summary>
            /// <param name="source">The source object.</param>
            /// <param name="listeners">The list used to add listeners to which lost their reference.</param>
            public abstract void OnPropertyChanged(T source, IList<Listener> listeners);
        }
        #endregion

    }
}