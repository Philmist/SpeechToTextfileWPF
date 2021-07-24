#nullable enable
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace SpeechToTextfileWPF
{
    class ViewModelBase : INotifyPropertyChanged
    {
        #region INotifyPropertyChanged boiler-plate

        /* from: https://stackoverflow.com/questions/1315621/implementing-inotifypropertychanged-does-a-better-way-exist */

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
            {
                return false;
            }
            if (propertyName != null)
            {
                OnPropertyChanged(propertyName);

            }
            return true;
        }

        #endregion

    }
}
