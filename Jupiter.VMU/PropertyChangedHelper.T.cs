using System.Collections.Concurrent;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using static Jupiter.VMU.PropertyChangedHelper;

namespace Jupiter.VMU
{
    /// <summary>
    /// Provides helper methods for subscription to the <see cref="INotifyPropertyChanged.PropertyChanged"/> event.
    /// </summary>
    /// <typeparam name="T">The type which raises the event.</typeparam>
    public sealed partial class PropertyChangedHelper<T>
        where T : class, INotifyPropertyChanged
    {
        #region #### VARIABLES ##########################################################
        static readonly Type ListenerType = typeof(Listener<,>);
        readonly PropertyChangedHelper _PropertyChangedHelper;
        #endregion
        #region #### CTOR ###############################################################
        /// <summary>
        /// Initalizes a new instance of the <see cref="PropertyChangedHelper{T}"/> class.
        /// </summary>
        /// <param name="source">The source to listen to.</param>
        private PropertyChangedHelper(PropertyChangedHelper propertyChangedHelper)
        {
            _PropertyChangedHelper = propertyChangedHelper;
        }
        #endregion
        #region #### PUBLIC #############################################################
        /// <summary>
        /// Unsubscribes from listening to the <see cref="INotifyPropertyChanged.PropertyChanged"/> event
        /// and clears all listeners and removes the instance from the cache.
        /// </summary>
        public void Unsubscribe() 
            => _PropertyChangedHelper.Unsubscribe();

        /// <summary>
        /// Unsubscribes from listening to any update for the <paramref name="target"/> object.
        /// </summary>
        /// <param name="source">The source for which the <paramref name="target"/> should unsubscribe.</param>
        /// <param name="target">The target for which the subscriptions should be removed.</param>
        public void Unsubscribe(object target)
            => _PropertyChangedHelper.Unsubscribe(target);

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
            String propertyName = ValidateProperty(propertyAccessor);

            if (action.Target == null)
                _PropertyChangedHelper.AddListener(propertyName, new ListenerStatic<R>(propertyAccessor.Compile(), action));
            else
            {
                Type targetType = action.Target.GetType();
                if (targetType.IsValueType)
                {
                    throw new NotSupportedException("Structs are not supported");
                }
                else
                {
                    _PropertyChangedHelper.AddListener(propertyName, (Listener)Activator.CreateInstance(
                        ListenerType.MakeGenericType(typeof(T), targetType, typeof(R)),
                        propertyAccessor.Compile(), action));
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
            return new PropertyChangedHelper<T>(PropertyChangedHelper.GetHelper(source));
        }

        /// <summary>
        /// Gets the <see cref="PropertyChangedHelper{T}"/> for the specified instance if any helper exists.
        /// </summary>
        /// <param name="source">The source to listen to.</param>
        /// <param name="helper">The helper if any could be found.</param>
        /// <returns>A boolean indicating whether a helper was found; otherwise false.</returns>
        public static bool TryGetHelper(T source, out PropertyChangedHelper<T> helper)
        {
            if (PropertyChangedHelper.TryGetHelper(source, out PropertyChangedHelper propertyChangedHelper))
            {
                helper = new PropertyChangedHelper<T>(propertyChangedHelper);
                return true;
            }

            helper = null;
            return false;
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
        #endregion
        #region #### NESTED TYPES #######################################################
        /// <summary>
        /// Defines a listener to a property changed event.
        /// </summary>
        abstract class Listener : PropertyChangedHelper.Listener
        {
            /// <summary>
            /// Called when the target property has been changed.
            /// </summary>
            /// <param name="source">The source object.</param>
            /// <param name="listeners">The list used to add listeners to which lost their reference.</param>
            public override void OnPropertyChanged(object source, IList<PropertyChangedHelper.Listener> listeners)
                => OnPropertyChangedCore((T)source, listeners);

            /// <summary>
            /// Called when the target property has been changed.
            /// </summary>
            /// <param name="source">The source object.</param>
            /// <param name="listeners">The list used to add listeners to which lost their reference.</param>
            public abstract void OnPropertyChangedCore(T source, IList<PropertyChangedHelper.Listener> listeners);
        }
        #endregion
    }
}