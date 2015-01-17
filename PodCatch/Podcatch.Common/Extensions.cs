using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace PodCatch.Common
{
    public static class Extensions
    {
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

        public static void Clear<T>(this ConcurrentBag<T> bag)
        {
            T element;
            while (!bag.IsEmpty)
            {
                bag.TryTake(out element);
            }
        }

        public static void AddAll<T>(this ConcurrentBag<T> bag, IEnumerable<T> elements)
        {
            foreach(T element in elements)
            {
                bag.Add(element);
            }
        }
    }
}
