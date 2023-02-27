using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace WYW.RS232SOCKET.Common
{
     class ObservableCollectionEx<T> : Collection<T>, INotifyCollectionChanged
    {
        protected override void InsertItem(int index, T item)
        {
            base.InsertItem(index, item);

            Application.Current.Dispatcher.InvokeAsync(() =>
            {
                CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item, index));
            });
        }

        protected override void RemoveItem(int index)
        {
            var item = this[index];

            base.RemoveItem(index);

            Application.Current.Dispatcher.InvokeAsync(() =>
            {
                CollectionChanged?.Invoke(this,
              new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, item, index));
            });
        }

        protected override void SetItem(int index, T item)
        {
            var oldItem = this[index];
            base.SetItem(index, item);
            Application.Current.Dispatcher.InvokeAsync(() =>
            {
                CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, item, oldItem, index));
            });
        }

        public event NotifyCollectionChangedEventHandler CollectionChanged;
    }
}
