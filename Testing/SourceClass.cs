using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Testing
{
    public class SourceClass : INotifyPropertyChanged
    {
        #region #### VARIABLES ##########################################################
        private bool _ValueToSet;
        #endregion
        #region #### PROPERTIES #########################################################
        public bool ValueToSet
        {
            get => _ValueToSet;
            set => SetProperty(ref _ValueToSet, value);
        }
        #endregion
        #region #### EVENTS #############################################################
        public event PropertyChangedEventHandler? PropertyChanged;
        #endregion
        #region #### CTOR ###############################################################
        #endregion
        #region #### PUBLIC #############################################################
        #endregion
        #region #### PRIVATE ############################################################
        bool SetProperty<T>(ref T storage, T value, [CallerMemberName]string propertyName = null!)
        {
            if (!Object.Equals(storage, value))
            {
                storage = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
                return true;
            }

            return false;
        }
        #endregion
        #region #### NESTED TYPES #######################################################
        #endregion
    }
}