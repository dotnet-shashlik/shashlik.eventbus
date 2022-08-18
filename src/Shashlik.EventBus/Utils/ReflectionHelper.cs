using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyModel;

namespace Shashlik.EventBus.Utils
{
    /// <summary>
    /// 反射相关方法
    /// </summary>
    public class ReflectionHelper
    {
        /// <summary>
        /// 获取引用了<paramref name="assembly"/>程序集的所有的程序集
        /// </summary>
        /// <param name="assembly">引用这个程序集</param>
        /// <param name="dependencyContext">依赖上下文,null则使用默认</param>
        /// <returns></returns>
        public static List<Assembly> GetReferredAssemblies(Assembly assembly,
            DependencyContext? dependencyContext = null)
        {
            var res = CacheAssemblyReferred.GetOrDefault(assembly);
            if (res is not null)
                return res;

            var allLib = (dependencyContext ?? DependencyContext.Default)
                .RuntimeLibraries
                .OrderBy(r => r.Name)
                .ToList();

            var name = assembly.GetName().Name;
            if (name is null)
                return new List<Assembly>();

            Dictionary<string, HashSet<string>> allDependencies = new Dictionary<string, HashSet<string>>();
            foreach (var item in allLib)
            {
                allDependencies.Add(item.Name, new HashSet<string>());
                LoadAllDependency(allLib, item.Name, new HashSet<string>(), item, allDependencies);
            }

            var list = allDependencies
                .Where(r => r.Value.Contains(name))
                .Select(r =>
                {
                    try
                    {
                        return Assembly.Load(r.Key);
                    }
                    catch
                    {
                        return null;
                    }
                })
                .Where(r => r is not null)
                .ToList();

            res = list!;
            CacheAssemblyReferred.TryAdd(assembly, res);
            return res;
        }

        /// <summary>
        /// 获取引用了<typeparamref name="TType"/>的程序集
        /// </summary>
        /// <param name="dependencyContext">依赖上下文,null则使用默认</param>
        /// <returns></returns>
        public static List<Assembly> GetReferredAssemblies<TType>(DependencyContext? dependencyContext = null)
        {
            return GetReferredAssemblies(typeof(TType).Assembly, dependencyContext);
        }

        /// <summary>
        /// 获取引用了<param name="type"></param>的程序集
        /// </summary>
        /// <param name="type"></param>
        /// <param name="dependencyContext">依赖上下文,null则使用默认</param>
        /// <returns></returns>
        public static List<Assembly> GetReferredAssemblies(Type type, DependencyContext? dependencyContext = null)
        {
            return GetReferredAssemblies(type.Assembly, dependencyContext);
        }

        /// <summary>
        /// 获取所有的子类,不包括接口和抽象类,包含泛型定义
        /// </summary>
        /// <param name="baseType">基类,可以是泛型定义</param>
        /// <param name="assembly"></param>
        /// <returns></returns>
        public static List<TypeInfo> GetFinalSubTypes(Type baseType, Assembly assembly)
        {
            return assembly.DefinedTypes.Where(r =>
                !r.IsAbstract && r.IsClass && (r.IsSubTypeOrEqualsOf(baseType) || r.IsSubTypeOfGenericType(baseType))).ToList();
        }

        /// <summary>
        /// 获取所有的子类和自身,不包括接口和抽象类,包含泛型定义
        /// </summary>
        /// <param name="baseType">基类,可以是泛型定义</param>
        /// <param name="dependencyContext"></param>
        /// <returns></returns>
        public static List<TypeInfo> GetFinalSubTypes(Type baseType, DependencyContext? dependencyContext = null)
        {
            List<TypeInfo> types = new List<TypeInfo>();
            foreach (var item in GetReferredAssemblies(baseType, dependencyContext))
            {
                types.AddRange(GetFinalSubTypes(baseType, item));
            }

            types.AddRange(GetFinalSubTypes(baseType, baseType.Assembly));
            return types;
        }

        /// <summary>
        /// 获取所有的子类和自身,不包括接口和抽象类,包含泛型定义
        /// </summary>
        /// <param name="assembly"></param>
        /// <returns></returns>
        public static List<TypeInfo> GetFinalSubTypes<TBaseType>(Assembly assembly)
        {
            return GetFinalSubTypes(typeof(TBaseType), assembly);
        }

        /// <summary>
        /// 获取所有的子类和自身,不包括接口和抽象类,包含泛型定义
        /// </summary>
        /// <typeparam name="TBaseType"></typeparam>
        /// <param name="dependencyContext"></param>
        /// <returns></returns>
        public static List<TypeInfo> GetFinalSubTypes<TBaseType>(DependencyContext? dependencyContext = null)
        {
            return GetFinalSubTypes(typeof(TBaseType), dependencyContext);
        }

        /// <summary>
        /// 获取所有的子类和自身,包括接口和抽象类,包含泛型定义
        /// </summary>
        /// <param name="baseType">基类,可以是泛型定义</param>
        /// <param name="assembly"></param>
        /// <returns></returns>
        public static List<TypeInfo> GetSubTypes(Type baseType, Assembly assembly)
        {
            return assembly
                .DefinedTypes
                .Where(r => r.IsSubTypeOrEqualsOf(baseType) || r.IsSubTypeOfGenericType(baseType))
                .ToList();
        }

        /// <summary>
        /// 获取所有的子类和自身,包括接口和抽象类,包含泛型定义
        /// </summary>
        /// <param name="baseType">基类,可以是泛型定义</param>
        /// <param name="dependencyContext"></param>
        /// <returns></returns>
        public static List<TypeInfo> GetSubTypes(Type baseType, DependencyContext? dependencyContext = null)
        {
            var types = new List<TypeInfo>();
            foreach (var item in GetReferredAssemblies(baseType, dependencyContext))
                types.AddRange(GetSubTypes(baseType, item));

            types.AddRange(GetSubTypes(baseType, baseType.Assembly));
            return types;
        }

        /// <summary>
        /// 获取所有的子类和自身,包括接口和抽象类,包含泛型定义
        /// </summary>
        /// <param name="assembly"></param>
        /// <returns></returns>
        public static List<TypeInfo> GetSubTypes<TBaseType>(Assembly assembly)
        {
            return GetSubTypes(typeof(TBaseType), assembly);
        }

        /// <summary>
        /// 获取所有的子类和自身,包括接口和抽象类,包含泛型定义
        /// </summary>
        /// <typeparam name="TBaseType"></typeparam>
        /// <param name="dependencyContext"></param>
        /// <returns></returns>
        public static List<TypeInfo> GetSubTypes<TBaseType>(DependencyContext? dependencyContext = null)
        {
            return GetSubTypes(typeof(TBaseType), dependencyContext);
        }

        /// <summary>
        /// 根据特性获取类和特性值,不支持多特性Multiple
        /// </summary>
        /// <param name="baseType"></param>
        /// <param name="assembly"></param>
        /// <param name="inherit"></param>
        /// <returns></returns>
        public static IDictionary<TypeInfo, Attribute> GetTypesAndAttribute(Type baseType, Assembly assembly,
            bool inherit = true)
        {
            Dictionary<TypeInfo, Attribute> dic = new Dictionary<TypeInfo, Attribute>();

            foreach (var item in assembly.DefinedTypes)
            {
                var attr = item.GetCustomAttribute(baseType, inherit);
                if (attr is null)
                    continue;

                dic.Add(item, attr);
            }

            return dic;
        }

        /// <summary>
        /// 根据特性获取类和特性值,不支持多特性Multiple
        /// </summary>
        /// <param name="baseType">基类,可以是泛型定义</param>
        /// <param name="dependencyContext"></param>
        /// <param name="inherit"></param>
        /// <returns></returns>
        public static IDictionary<TypeInfo, Attribute> GetTypesAndAttribute(Type baseType,
            DependencyContext? dependencyContext = null, bool inherit = true)
        {
            Dictionary<TypeInfo, Attribute> dic = new Dictionary<TypeInfo, Attribute>();
            foreach (var item in GetReferredAssemblies(baseType, dependencyContext))
            {
                dic.Merge(GetTypesAndAttribute(baseType, item, inherit));
            }

            dic.Merge(GetTypesAndAttribute(baseType, baseType.Assembly, inherit));

            return dic;
        }

        /// <summary>
        /// 根据特性获取类和特性值,不支持多特性Multiple
        /// </summary>
        /// <param name="assembly"></param>
        /// <param name="inherit"></param>
        /// <returns></returns>
        public static IDictionary<TypeInfo, TAttribute> GetTypesAndAttribute<TAttribute>(Assembly assembly,
            bool inherit = true)
            where TAttribute : Attribute
        {
            Dictionary<TypeInfo, TAttribute> dic = new Dictionary<TypeInfo, TAttribute>();

            foreach (var item in assembly.DefinedTypes)
            {
                var attr = item.GetCustomAttribute<TAttribute>(inherit);
                if (attr is null)
                    continue;

                dic.Add(item, attr);
            }

            return dic;
        }

        /// <summary>
        /// 根据特性获取类和特性值,不支持多特性Multiple
        /// </summary>
        /// <typeparam name="TAttribute"></typeparam>
        /// <param name="dependencyContext"></param>
        /// <param name="inherit"></param>
        /// <returns></returns>
        public static IDictionary<TypeInfo, TAttribute> GetTypesAndAttribute<TAttribute>(
            DependencyContext? dependencyContext = null, bool inherit = true)
            where TAttribute : Attribute
        {
            Dictionary<TypeInfo, TAttribute> dic = new Dictionary<TypeInfo, TAttribute>();
            foreach (var item in GetReferredAssemblies<TAttribute>(dependencyContext))
            {
                dic.Merge(GetTypesAndAttribute<TAttribute>(item, inherit));
            }

            dic.Merge(GetTypesAndAttribute<TAttribute>(typeof(TAttribute).Assembly, inherit));

            return dic;
        }

        /// <summary>
        /// 根据特性获取类和特性值,不支持多特性Multiple
        /// </summary>
        /// <param name="baseType"></param>
        /// <param name="assembly"></param>
        /// <param name="inherit"></param>
        /// <returns></returns>
        public static IDictionary<TypeInfo, IEnumerable<object>> GetTypesByAttributes(Type baseType, Assembly assembly,
            bool inherit = true)
        {
            Dictionary<TypeInfo, IEnumerable<object>> dic = new Dictionary<TypeInfo, IEnumerable<object>>();

            foreach (var item in assembly.DefinedTypes)
            {
                var attrs = item.GetCustomAttributes(baseType, inherit);
                if (attrs.IsNullOrEmpty())
                    continue;

                dic.Add(item, attrs);
            }

            return dic;
        }

        /// <summary>
        /// 根据特性获取类和特性值,不支持多特性Multiple
        /// </summary>
        /// <param name="baseType"></param>
        /// <param name="dependencyContext"></param>
        /// <param name="inherit"></param>
        /// <returns></returns>
        public static IDictionary<TypeInfo, IEnumerable<object>> GetTypesByAttributes(Type baseType,
            DependencyContext? dependencyContext = null, bool inherit = true)
        {
            Dictionary<TypeInfo, IEnumerable<object>> dic = new Dictionary<TypeInfo, IEnumerable<object>>();
            foreach (var item in GetReferredAssemblies(baseType, dependencyContext))
            {
                dic.Merge(GetTypesByAttributes(baseType, item, inherit));
            }

            dic.Merge(GetTypesByAttributes(baseType, baseType.Assembly, inherit));

            return dic;
        }

        /// <summary>
        /// 根据特性获取类和特性值,不支持多特性Multiple
        /// </summary>
        /// <param name="assembly"></param>
        /// <param name="inherit"></param>
        /// <returns></returns>
        public static IDictionary<TypeInfo, IEnumerable<TAttribute>> GetTypesByAttributes<TAttribute>(Assembly assembly,
            bool inherit = true)
            where TAttribute : Attribute
        {
            Dictionary<TypeInfo, IEnumerable<TAttribute>> dic = new Dictionary<TypeInfo, IEnumerable<TAttribute>>();

            foreach (var item in assembly.DefinedTypes)
            {
                var attrs = item.GetCustomAttributes<TAttribute>(inherit).ToList();
                if (attrs.IsNullOrEmpty())
                    continue;

                dic.Add(item, attrs);
            }

            return dic;
        }

        /// <summary>
        /// 根据特性获取类和特性值,不支持多特性Multiple
        /// </summary>
        /// <typeparam name="TAttribute"></typeparam>
        /// <param name="dependencyContext"></param>
        /// <param name="inherit"></param>
        /// <returns></returns>
        public static IDictionary<TypeInfo, IEnumerable<TAttribute>> GetTypesByAttributes<TAttribute>(
            DependencyContext? dependencyContext = null, bool inherit = true)
            where TAttribute : Attribute
        {
            Dictionary<TypeInfo, IEnumerable<TAttribute>> dic = new Dictionary<TypeInfo, IEnumerable<TAttribute>>();
            foreach (var item in GetReferredAssemblies<TAttribute>(dependencyContext))
            {
                dic.Merge(GetTypesByAttributes<TAttribute>(item, inherit));
            }

            dic.Merge(GetTypesByAttributes<TAttribute>(typeof(TAttribute).Assembly, inherit));

            return dic;
        }

        #region private

        /// <summary>
        /// 缓存程序集被引用数据
        /// </summary>
        private static readonly ConcurrentDictionary<Assembly, List<Assembly>> CacheAssemblyReferred =
            new ConcurrentDictionary<Assembly, List<Assembly>>();

        /// <summary>
        /// 加载所有的依赖
        /// </summary>
        /// <param name="allLibs">应用包含的所有程序集</param>
        /// <param name="key">当前计算的程序集name</param>
        /// <param name="handled">当前计算的程序集,已经处理过的依赖程序集名称</param>
        /// <param name="current">递归正在处理的程序集名称</param>
        /// <param name="allDependencies">所有的依赖数据</param>
        private static void LoadAllDependency(IEnumerable<RuntimeLibrary> allLibs, string key, HashSet<string> handled,
            RuntimeLibrary current, Dictionary<string, HashSet<string>> allDependencies)
        {
            if (current.Dependencies.IsNullOrEmpty())
                return;
            if (handled.Contains(current.Name))
                return;
            handled.Add(current.Name);
            var runtimeLibraries = allLibs.ToList();
            foreach (var item in current.Dependencies)
            {
                allDependencies[key].Add(item.Name);

                var next = runtimeLibraries.FirstOrDefault(r => r.Name == item.Name);
                if (next is null || next.Dependencies.IsNullOrEmpty())
                    continue;
                LoadAllDependency(runtimeLibraries, key, handled, next, allDependencies);
            }
        }

        #endregion
    }
}