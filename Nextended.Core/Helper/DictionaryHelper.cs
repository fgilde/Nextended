﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Newtonsoft.Json.Linq;

namespace Nextended.Core.Helper;

public static class DictionaryHelper
{
    public static JObject ToJObject(this IDictionary<string, object> dictionary)
        => JsonDictionaryConverter.DictionaryToJObject(dictionary);

    public static T ToObject<T>(this IDictionary<string, object> dictionary)
        => dictionary != null ? dictionary.ToJObject().ToObject<T>() : default;
    
    public static object ToObject(this IDictionary<string, object> dictionary, Type objectType)
        => dictionary?.ToJObject()?.ToObject(objectType);

    public static IDictionary<string, object> GetValuesDictionary<T>(Action<T> options, bool removeDefaults, BindingFlags flags = BindingFlags.Public | BindingFlags.Instance)
      where T : new()
    {
        var instance = new T();
        options(instance);
        return GetValuesDictionary(instance, removeDefaults, flags);
    }

    public static IDictionary<string, object> GetValuesDictionary<T>(bool removeDefaults, params Action<T>[] options)
        where T : new()
    {
        var instance = new T();
        foreach (var option in options)
            option(instance);
        return GetValuesDictionary(instance, removeDefaults);
    }

    public static IDictionary<string, object> GetValuesDictionary<T>(T o, bool removeDefaults, BindingFlags flags = BindingFlags.Public | BindingFlags.Instance)
        where T : new()
    {
        if (o == null)
            return null;
        var newInstance = removeDefaults ? new T() : default;
        return removeDefaults 
            ? GetValuesFunc<T>(flags)(o).Where(pair => !Equals(typeof(T).GetProperty(pair.Key, flags)?.GetValue(newInstance), pair.Value)).ToDictionary(pair => pair.Key, pair => pair.Value)
            : GetValuesFunc<T>(flags)(o);
    }

    //public static Func<T, Dictionary<string, object>> GetValuesFunc<T>(BindingFlags flags = BindingFlags.Public | BindingFlags.Instance)
    //{
    //    var objType = typeof(T);

    //    var dict = Expression.Variable(typeof(Dictionary<string, object>));
    //    var par = Expression.Parameter(typeof(T), "obj");

    //    var add = typeof(Dictionary<string, object>).GetMethod(nameof(Dictionary<string, object>.Add), flags, null, new[] { typeof(string), typeof(object) }, null);

    //    var body = new List<Expression> { Expression.Assign(dict, Expression.New(typeof(Dictionary<string, object>))) };

    //    var properties = objType.GetTypeInfo().GetProperties(flags);

    //    body.AddRange(from p in properties where p.CanRead && p.GetIndexParameters().Length == 0 let key = Expression.Constant(p.Name) let value = Expression.Property(par, p) let valueAsObject = Expression.Convert(value, typeof(object)) select Expression.Call(dict, add, key, valueAsObject));

    //    // Return value
    //    body.Add(dict);

    //    var block = Expression.Block(new[] { dict }, body);

    //    var lambda = Expression.Lambda<Func<T, Dictionary<string, object>>>(block, par);
    //    return lambda.Compile();
    //}

    public static Func<T, Dictionary<string, object>> GetValuesFunc<T>(BindingFlags flags = BindingFlags.Public | BindingFlags.Instance)
    {
        var objType = typeof(T);

        var dict = Expression.Variable(typeof(Dictionary<string, object>));
        var par = Expression.Parameter(typeof(T), "obj");

        var add = typeof(Dictionary<string, object>).GetMethod(nameof(Dictionary<string, object>.Add), flags, null, new[] { typeof(string), typeof(object) }, null);

        var body = new List<Expression> { Expression.Assign(dict, Expression.New(typeof(Dictionary<string, object>))) };

        var properties = objType.GetTypeInfo().GetProperties(flags);

        body.AddRange((from p in properties
            where p.CanRead && p.GetIndexParameters().Length == 0
            let key = Expression.Constant(p.Name)
            let value = Expression.Property(par, p)
            let valueAsObject = Expression.Convert(value, typeof(object))
            select Expression.TryCatch(Expression.Call(dict, add, key, valueAsObject), Expression.Catch(typeof(Exception),
                // Do nothing on exception, effectively ignoring the problematic property.
                Expression.Empty()))).Cast<Expression>());

        // Return value
        body.Add(dict);

        var block = Expression.Block(new[] { dict }, body);

        var lambda = Expression.Lambda<Func<T, Dictionary<string, object>>>(block, par);
        return lambda.Compile();
    }

}