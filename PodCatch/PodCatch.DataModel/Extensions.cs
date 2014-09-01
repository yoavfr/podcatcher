using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PodCatch.DataModel
{
    using System.Diagnostics.Contracts;

    public static class Extensions
    {
        public static int IndexOfOccurence(this string s, string match, int occurence)
        {
            int i = 1;
            int index = 0;

            while (i <= occurence && index < s.Length && (index = s.IndexOf(match, index + 1)) != -1)
            {
                if (i == occurence) return index;

                i++;
            }

            return -1;
        }

        public static void AddAll<T>(this ICollection<T> dest, Collection<T> src)
        {
            foreach (T t in src)
            {
                dest.Add(t);
            }
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

        public static void AddAll<T>(this ICollection<T> dest, IEnumerable<T> src)
        {
            foreach (T t in src)
            {
                dest.Add(t);
            }
        }

        public static void ForEach<T>(this IEnumerable<T> source, Action<T, int> action)
        {
            int index = 0;
            foreach (T element in source)
            {
                action(element, index++);
            }
        }

        public static int GetFixedHashCode(this string str)
        {
            int hash1 = 5381;
            int hash2 = hash1;
            int i = 0;
            char c;
            while (i<str.Length)
            {
                c = str[i];
                hash1 = ((hash1 << 5) + hash1) ^ c;
                c = str[1];
                if (c == 0) break;
                hash2 = ((hash2 << 5) + hash2) ^ c;
                i++;
            }
            return hash1 + (hash2 * 1566083941);
        }

    }
}
