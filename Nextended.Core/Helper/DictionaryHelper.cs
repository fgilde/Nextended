using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Nextended.Core.Helper;

public static class DictionaryHelper
{
    public static IDictionary<string, object> GetValuesDictionary<T>(Action<T> options, bool removeDefaults,
  BindingFlags flags = BindingFlags.Public | BindingFlags.Instance)
      where T : new()
    {
        var instance = new T();
        options(instance);
        return GetValuesDictionary(instance, removeDefaults, flags);
    }

    public static IDictionary<string, object> GetValuesDictionary<T>(T o, bool removeDefaults, BindingFlags flags = BindingFlags.Public | BindingFlags.Instance)
        where T : new()
    {
        if (o == null)
            return null;
        var newInstance = removeDefaults ? new T() : default;
        return removeDefaults
            ? GetValuesFunc<T>(flags)(o).Where(pair => typeof(T).GetProperty(pair.Key, flags)?.GetValue(newInstance) != pair.Value).ToDictionary(pair => pair.Key, pair => pair.Value)
            : GetValuesFunc<T>(flags)(o);
    }

    public static Func<T, Dictionary<string, object>> GetValuesFunc<T>(BindingFlags flags = BindingFlags.Public | BindingFlags.Instance)
    {
        var objType = typeof(T);

        var dict = Expression.Variable(typeof(Dictionary<string, object>));
        var par = Expression.Parameter(typeof(T), "obj");

        var add = typeof(Dictionary<string, object>).GetMethod(nameof(Dictionary<string, object>.Add), flags, null, new[] { typeof(string), typeof(object) }, null);

        var body = new List<Expression> { Expression.Assign(dict, Expression.New(typeof(Dictionary<string, object>))) };

        var properties = objType.GetTypeInfo().GetProperties(flags);

        body.AddRange(from p in properties where p.CanRead && p.GetIndexParameters().Length == 0 let key = Expression.Constant(p.Name) let value = Expression.Property(par, p) let valueAsObject = Expression.Convert(value, typeof(object)) select Expression.Call(dict, add, key, valueAsObject));

        // Return value
        body.Add(dict);

        var block = Expression.Block(new[] { dict }, body);

        var lambda = Expression.Lambda<Func<T, Dictionary<string, object>>>(block, par);
        return lambda.Compile();
    }
}