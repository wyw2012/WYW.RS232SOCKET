using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace WYW.Communication
{
    /// <inheritdoc />
    /// <summary>
    /// Object that Implements INotifyPropertyChanged
    /// </summary>
    [Serializable]
    public class ObservableObject : INotifyPropertyChanged
    {
        protected bool SetProperty<T>(ref T field, T newValue, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, newValue))
                return false;

            field = newValue;
            OnPropertyChanged(propertyName);
            return true;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void CopyFrom(object source)
        {
            if(this.GetType()==source.GetType())
            {
                var from = source.GetType().GetProperties();
                var to = GetType().GetProperties();
                if (to.Length == from.Length)
                {
                    for (int i = 0; i < to.Length; i++)
                    {
                        var value = from[i].GetValue(source, null);
                        to[i].SetValue(this, value);
                    }
                }
            }
            else
            {
                var from = source.GetType().GetProperties();
                var to = GetType().GetProperties();
                for (int i = 0; i < to.Length; i++)
                {
                    for (int j = 0; j < from.Length; j++)
                    {
                        if (to[i].Name == from[j].Name && to[i].PropertyType == from[j].PropertyType)
                        {
                            var value = from[j].GetValue(source, null);
                            to[i].SetValue(this, value);
                            break;
                        }
                    }
                }
            }
        }

        public void CopyTo(object target)
        {
            var from = GetType().GetProperties();
            var to = target.GetType().GetProperties();
            for (int i = 0; i < to.Length; i++)
            {
                for (int j = 0; j < from.Length; j++)
                {
                    if (to[i].Name == from[j].Name && to[i].PropertyType == from[j].PropertyType)
                    {
                        var value = from[j].GetValue(this, null);
                        to[i].SetValue(target, value);
                        break;
                    }
                }

            }
        }
    }
}
