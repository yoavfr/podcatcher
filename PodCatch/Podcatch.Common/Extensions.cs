using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Windows.Storage;

namespace PodCatch.Common
{
    public static class Extensions
    {
        /// <summary>
        /// Type.GetMethod
        /// </summary>
        /// <param name="type"></param>
        /// <param name="methodName"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public static MethodInfo GetMethod(this Type type, string methodName, Type[] parameters)
        {
            var results = from m in type.GetTypeInfo().DeclaredMethods
                          where m.Name == methodName
                          let methodParameters = m.GetParameters().Select(_ => _.ParameterType).ToArray()
                          where methodParameters.Length == parameters.Length &&
                            !methodParameters.Except(parameters).Any() &&
                            !parameters.Except(methodParameters).Any()
                          select m;
            return results.FirstOrDefault();
        }

        /// <summary>
        /// Type.GetConstructor
        /// </summary>
        /// <param name="type"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public static ConstructorInfo GetConstructor(this Type type, Type[] parameters)
        {
            var results = from c in type.GetTypeInfo().DeclaredConstructors
                          let constructorParameters = c.GetParameters().Select(_ => _.ParameterType).ToArray()
                          where constructorParameters.Length == parameters.Length &&
                            !constructorParameters.Except(parameters).Any() &&
                            !parameters.Except(constructorParameters).Any()
                          select c;
            return results.FirstOrDefault();
        }

        /// <summary>
        /// ICollection.RemoveAll
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="collection"></param>
        /// <param name="predicate"></param>
        public static void RemoveAll<T>(this ICollection<T> collection, Func<T, bool> predicate)
        {
            T element;

            for (int i = 0; i < collection.Count; i++)
            {
                element = collection.ElementAt(i);
                if (predicate(element))
                {
                    collection.Remove(element);
                    i--;
                }
            }
        }

        /// <summary>
        /// ICollection.RemoveFirst
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="collection"></param>
        /// <param name="predicate"></param>
        public static void RemoveFirst<T>(this ICollection<T> collection, Func<T, bool> predicate)
        {
            T element;

            for (int i = 0; i < collection.Count; i++)
            {
                element = collection.ElementAt(i);
                if (predicate(element))
                {
                    collection.Remove(element);
                    return;
                }
            }
        }

        /// <summary>
        /// ConcurrentBag.Clear
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="bag"></param>
        public static void Clear<T>(this ConcurrentBag<T> bag)
        {
            T element;
            while (!bag.IsEmpty)
            {
                bag.TryTake(out element);
            }
        }

        /// <summary>
        /// ConcurrentBag.AddAll
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="bag"></param>
        /// <param name="elements"></param>
        public static void AddAll<T>(this ConcurrentBag<T> bag, IEnumerable<T> elements)
        {
            foreach (T element in elements)
            {
                bag.Add(element);
            }
        }

        public static object ConsumeValue(this ApplicationDataContainer dataContainer, string key)
        {
            if (dataContainer.Values.ContainsKey(key))
            {
                var value = dataContainer.Values[key];
                dataContainer.Values.Remove(key);
                return value;
            }
            return null;
        }

        public static void PutValue(this ApplicationDataContainer dataContainer, string key, object value)
        {
            if (dataContainer.Values.ContainsKey(key))
            {
                dataContainer.Values[key] = value;
                return;
            }
            dataContainer.Values.Add(key, value);
        }
    }
}