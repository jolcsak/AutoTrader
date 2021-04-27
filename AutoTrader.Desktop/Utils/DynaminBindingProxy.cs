using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Dynamic;
using System.Linq;
using System.Reflection;

namespace AutoTrader.Desktop.Utils
{
    // http://www.deanchalk.me.uk/post/WPF-e28093-Easy-INotifyPropertyChanged-Via-DynamicObject-Proxy.aspx
    public class DynamicBindingProxy<T> : DynamicObject, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private static readonly Dictionary<string, Dictionary<string, PropertyInfo>> properties =
            new Dictionary<string, Dictionary<string, PropertyInfo>>();
        private readonly T instance;
        private readonly string typeName;

        public DynamicBindingProxy(T instance)
        {
            this.instance = instance;
            var type = typeof(T);
            typeName = type.FullName;
            if (!properties.ContainsKey(typeName))
                SetProperties(type, typeName);
        }

        private static void SetProperties(Type type, string typeName)
        {
            var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            var dict = props.ToDictionary(prop => prop.Name);
            properties.Add(typeName, dict);
        }

        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            if (properties[typeName].ContainsKey(binder.Name))
            {
                result = properties[typeName][binder.Name].GetValue(instance, null);
                return true;
            }
            result = null;
            return false;
        }

        public override bool TrySetMember(SetMemberBinder binder, object value)
        {
            if (properties[typeName].ContainsKey(binder.Name))
            {
                properties[typeName][binder.Name].SetValue(instance, value, null);
                if (PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs(binder.Name));
                return true;
            }
            return false;
        }
    }
}
