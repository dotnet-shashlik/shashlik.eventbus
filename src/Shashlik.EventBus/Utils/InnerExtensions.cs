using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text;

// ReSharper disable ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract

namespace Shashlik.EventBus.Utils;

/// <summary>
/// 内部用的一些扩展方法
/// </summary>
public static class InnerExtensions
{
    /// <summary>
    /// 循环集合元素
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="list"></param>
    /// <param name="action"></param>
    public static void ForEachItem<T>(this IEnumerable<T> list, Action<T> action)
    {
        if (list == null) throw new ArgumentNullException(nameof(list));
        if (action == null) throw new ArgumentNullException(nameof(action));

        foreach (var item in list)
        {
            action(item);
        }
    }

    /// <summary>
    /// where id
    /// </summary>
    /// <param name="list"></param>
    /// <param name="condition">条件值</param>
    /// <param name="where">where</param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    public static IEnumerable<T> WhereIf<T>(this IEnumerable<T> list, bool condition, Func<T, bool> where)
    {
        if (list is null) throw new ArgumentNullException(nameof(list));
        if (where is null) throw new ArgumentNullException(nameof(where));

        if (condition)
            return list.Where(where);
        return list;
    }


    /// <summary>
    /// 判断集合是否为null或者空
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="list"></param>
    /// <returns></returns>
    public static bool IsNullOrEmpty<T>(this IEnumerable<T>? list)
    {
        return list is null || !list.Any();
    }


    /// <summary>
    /// 获取字典值,没有则返回默认值
    /// </summary>
    /// <typeparam name="TKey"></typeparam>
    /// <typeparam name="TValue"></typeparam>
    /// <param name="dic"></param>
    /// <param name="key"></param>
    /// <returns></returns>
    public static TValue? GetOrDefault<TKey, TValue>(this IDictionary<TKey, TValue> dic, TKey key)
    {
        if (key is null)
            return default;
        if (dic.TryGetValue(key, out var value))
            return value;

        return default;
    }

    /// <summary>
    /// 合并两个字典，并覆盖相同键名的值
    /// </summary>
    /// <typeparam name="TKey"></typeparam>
    /// <typeparam name="TValue"></typeparam>
    /// <param name="to"></param>
    /// <param name="from"></param>
    public static void Merge<TKey, TValue>(this IDictionary<TKey, TValue> to, IDictionary<TKey, TValue>? from)
    {
        if (to == null) throw new ArgumentNullException(nameof(to));
        if (from is null) return;
        foreach (var kv in from)
        {
            if (to.ContainsKey(kv.Key))
            {
                to[kv.Key] = kv.Value;
            }
            else
            {
                to.Add(kv.Key, kv.Value);
            }
        }
    }

    /// <summary>
    /// 是否为类型<paramref name="parentType"/>的子类或自身
    /// </summary>
    /// <param name="type"></param>
    /// <param name="parentType">父类</param>
    /// <returns></returns>
    public static bool IsSubTypeOrEqualsOf(this Type type, Type parentType)
    {
        return parentType.IsAssignableFrom(type);
    }

    /// <summary>
    /// 判断给定的类型是否继承自<paramref name="genericType"/>泛型类型,
    /// <para>
    /// e.g.: typeof(Child&lt;&gt;).IsSubTypeOfGenericType(typeof(IParent&lt;&gt;));  result->true 
    /// </para>
    /// <para>
    /// e.g.: typeof(Child&lt;int&gt;).IsSubTypeOfGenericType(typeof(IParent&lt;&gt;));  result->true 
    /// </para>
    /// </summary>
    /// <param name="childType">子类型</param>
    /// <param name="genericType">泛型父级,例: typeof(IParent&lt;&gt;)</param>
    /// <returns></returns>
    public static bool IsSubTypeOfGenericType(this Type childType, Type genericType)
    {
        if (childType == genericType)
            return false;
        if (!genericType.IsGenericTypeDefinition)
            return false;
        var interfaceTypes = childType.GetTypeInfo().ImplementedInterfaces;

        foreach (var it in interfaceTypes)
        {
            if (it.IsGenericType && it.GetGenericTypeDefinition() == genericType)
                return true;
        }

        if (childType.IsGenericType && childType.GetGenericTypeDefinition() == genericType)
            return true;

        var baseType = childType.BaseType;
        if (baseType is null) return false;

        return IsSubTypeOfGenericType(baseType, genericType);
    }


    /// <summary>
    /// 类型转换
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="value"></param>
    /// <returns></returns>
    public static T? ParseTo<T>(this object value)
    {
        var res = ParseTo(value, typeof(T));
        if (res is null) return default;
        return (T)res;
    }

    /// <summary>
    /// 类型转换
    /// </summary>
    /// <param name="value"></param>
    /// <param name="destinationType">目标类型</param>
    /// <returns></returns>
    public static object? ParseTo(this object? value, Type destinationType)
    {
        if (value is null)
            return null;

        if (destinationType.IsInstanceOfType(value))
            return value;

        var sourceType = value.GetType();
        if (destinationType == typeof(bool) || destinationType == typeof(bool?))
            return Convert.ToBoolean(value);

        var destinationConverter = TypeDescriptor.GetConverter(destinationType);
        var sourceConverter = TypeDescriptor.GetConverter(sourceType);
        if (destinationConverter.CanConvertFrom(sourceType))
            return destinationConverter.ConvertFrom(value);
        if (sourceConverter.CanConvertTo(destinationType))
            return sourceConverter.ConvertTo(value, destinationType);
        if (destinationType.IsEnum)
        {
            var str = value.ToString();
            if (str is not null)
                return Enum.Parse(destinationType, str);
        }

        throw new InvalidCastException($"Invalid cast to type {destinationType} from {sourceType}");
    }

    /// <summary>
    /// 类型转换
    /// </summary>
    /// <param name="value"></param>
    /// <param name="destinationType">目标类型</param>
    /// <param name="result">转换结果</param>
    /// <returns></returns>
    public static bool TryParse(this object? value, Type destinationType, out object? result)
    {
        try
        {
            result = value.ParseTo(destinationType);
            return true;
        }
        catch
        {
            result = null;
            return false;
        }
    }

    /// <summary>
    /// 类型转换
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="value"></param>
    /// <param name="result">转换后的值</param>
    /// <returns></returns>
    public static bool TryParse<T>(this object value, out T result) where T : struct
    {
        try
        {
            result = value.ParseTo<T>();
            return true;
        }
        catch
        {
            result = default;
            return false;
        }
    }

    /// <summary>
    /// 是否为null或者空字符串
    /// </summary>
    /// <param name="str"></param>
    /// <returns></returns>
    public static bool IsNullOrWhiteSpace(this string? str)
    {
        return string.IsNullOrWhiteSpace(str);
    }

    /// <summary>
    /// 获取1970-1-1 到现在的秒数 使用UTC标准
    /// </summary>
    /// <param name="datetime"></param>
    /// <returns></returns>
    public static long GetLongDate(this DateTime datetime)
    {
        return new DateTimeOffset(datetime).ToUnixTimeSeconds();
    }

    /// <summary>
    /// 获取1970-1-1 到现在的秒数 使用UTC标准
    /// </summary>
    /// <param name="datetime"></param>
    /// <returns></returns>
    public static long GetLongDate(this DateTimeOffset datetime)
    {
        return datetime.ToUnixTimeSeconds();
    }

    private static readonly DateTimeOffset StartTimeOffset = new(1970, 1, 1, 0, 0, 0, TimeSpan.Zero);

    /// <summary>
    /// long转换为DateTimeOffset,本地时间,0转换为null
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public static DateTimeOffset? LongToDateTimeOffset(this long value)
    {
        if (value is 0L)
            return null;
        return StartTimeOffset.AddSeconds(value).ToLocalTime();
    }

    /// <summary>
    /// 反序列化
    /// </summary>
    /// <param name="messageSerializer"></param>
    /// <param name="text"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static T? Deserialize<T>(this IMessageSerializer messageSerializer, string text)
    {
        return (T?)messageSerializer.Deserialize(text, typeof(T));
    }

    /// <summary>
    /// 反序列化
    /// </summary>
    /// <param name="messageSerializer"></param>
    /// <param name="bytes"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static T? Deserialize<T>(this IMessageSerializer messageSerializer, byte[] bytes)
    {
        return (T?)messageSerializer.Deserialize(Encoding.UTF8.GetString(bytes), typeof(T));
    }

    /// <summary>
    /// 序列化为bytes数组
    /// </summary>
    /// <param name="messageSerializer"></param>
    /// <param name="obj"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static byte[] SerializeToBytes<T>(this IMessageSerializer messageSerializer, T obj)
    {
        ArgumentNullException.ThrowIfNull(obj);
        return Encoding.UTF8.GetBytes(messageSerializer.Serialize(obj));
    }

    /// <summary>
    /// 获取column的值
    /// </summary>
    /// <param name="row">row</param>
    /// <param name="col">column name</param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static T? GetRowValue<T>(this DataRow row, string col)
    {
        if (row is null) throw new ArgumentNullException(nameof(row));
        if (string.IsNullOrWhiteSpace(col))
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(col));
        var v = row[col];
        if (v is null || v == DBNull.Value)
            return default;
        return v.ParseTo<T>();
    }
}