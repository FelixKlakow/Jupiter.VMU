namespace Jupiter.VMU
{
    public sealed partial class PropertyChangedHelper<T>
    {
        sealed class Listener<TThis, R> : Listener
            where TThis : class
        {
            #region #### VARIABLES ##########################################################
            static Type CallbackType = typeof(Action<TThis, R>);
            static Type ValueType = typeof(R);

            private readonly WeakReference<TThis> _TargetObject;
            private readonly Func<T, R> _PropertyGetter;
            private readonly Action<TThis, R> _Callback;
            #endregion
            #region #### CTOR ###############################################################
            public Listener(Func<T, R> func, Action<R> action)
            {
                _PropertyGetter = func;
                _Callback = (Action<TThis, R>)Delegate.CreateDelegate(CallbackType, null, action.Method);
                _TargetObject = new WeakReference<TThis>((TThis)action.Target);
            }
            #endregion
            #region #### PUBLIC #############################################################
            /// <inheritdoc/>
            public override bool IsTargetOrCollected(object target)
            {
                return !_TargetObject.TryGetTarget(out TThis listenerTarget) 
                    || target == listenerTarget;
            }

            /// <inheritdoc/>
            public override void OnPropertyChanged(T source, IList<Listener> listeners)
            {
                if (_TargetObject.TryGetTarget(out TThis targetObject))
                {
                    _Callback(targetObject, _PropertyGetter(source));
                }
                // Listener doesn't exist anymore, add it to the list to remove
                else
                {
                    listeners.Add(this);
                }
            }
            #endregion
        }
        sealed class ListenerStatic<R> : Listener
        {
            #region #### VARIABLES ##########################################################
            private readonly Func<T, R> _PropertyGetter;
            private readonly Action<R> _Callback;
            #endregion
            #region #### CTOR ###############################################################
            public ListenerStatic(Func<T, R> func, Action<R> action)
            {
                _PropertyGetter = func;
                _Callback = action;
            }
            #endregion
            #region #### PUBLIC #############################################################
            /// <inheritdoc/>
            public override bool IsTargetOrCollected(object target) => false;

            /// <inheritdoc/>
            public override void OnPropertyChanged(T source, IList<Listener> listeners)
            {
                _Callback(_PropertyGetter(source));
            }
            #endregion
        }
    }
}