using System;
using System.ComponentModel;
using System.Linq.Expressions;
using Windows.ApplicationModel.Core;

namespace PodCatch.Common
{
    public abstract class BaseViewModel<DT> : ServiceConsumer, INotifyPropertyChanged
    {
        public DT Data { get; set; }

        public BaseViewModel(DT data, IServiceContext serviceContext)
            : base(serviceContext)
        {
            SetData(data);
        }

        public void SetData(DT data)
        {
            if (data != null)
            {
                Data = data;
                if (data is INotifyPropertyChanged)
                {
                    ((INotifyPropertyChanged)data).PropertyChanged += OnDataPropertyChanged;
                }
                ThreadManager.DispatchOnUIthread(() => UpdateFields());
            }
        }

        protected void OnDataPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            ThreadManager.DispatchOnUIthread(() => UpdateFields());
        }

        protected abstract void UpdateFields();

        public event PropertyChangedEventHandler PropertyChanged;

        protected void NotifyPropertyChanged<TValue>(Expression<Func<TValue>> propertyId)
        {
            string propertyName = ((MemberExpression)propertyId.Body).Member.Name;
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler == null || CoreApplication.Views.Count == 0)
            {
                return;
            }
            ThreadManager.DispatchOnUIthread(() => handler(this, new PropertyChangedEventArgs(propertyName)));
        }
    }
}