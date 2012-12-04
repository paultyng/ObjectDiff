using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ObjectDiff
{
    static class InternalExtensions
    {
        //from http://stackoverflow.com/questions/5489987/linq-full-outer-join
        public static IEnumerable<TReturn> FullOuterJoin<TA, TB, TKey, TReturn>(
            this IEnumerable<TA> a,
            IEnumerable<TB> b,
            Func<TA, TKey> selectKeyA, Func<TB, TKey> selectKeyB,
            Func<TA, TB, TKey, TReturn> projection,
            TA defaultA = default(TA), TB defaultB = default(TB))
        {
            var alookup = a.ToLookup(selectKeyA);
            var blookup = b.ToLookup(selectKeyB);

            var keys = new HashSet<TKey>(alookup.Select(p => p.Key));
            keys.UnionWith(blookup.Select(p => p.Key));

            var join = from key in keys
                       from xa in alookup[key].DefaultIfEmpty(defaultA)
                       from xb in blookup[key].DefaultIfEmpty(defaultB)
                       select projection(xa, xb, key);

            return join;
        }

        public static IEnumerable<T> Append<T>(this IEnumerable<T> source, T toAppend)
        {
            foreach (var item in source)
            {
                yield return item;
            }

            yield return toAppend;
        }

        public static IEnumerable<TReturn> FlattenHierarchy<T, TReturn>(this T node, Func<T, IEnumerable<T>> getChildEnumerator, Func<Stack<T>, T, TReturn> projection) where T : class
        {
            var stack = new Stack<T>();
            return node.FlattenHierarchy<T, TReturn>(getChildEnumerator, projection, stack);
        }

        private static IEnumerable<TReturn> FlattenHierarchy<T, TReturn>(this T node, Func<T, IEnumerable<T>> getChildEnumerator, Func<Stack<T>, T, TReturn> projection, Stack<T> stack) where T : class
        {
            //clone stack in projection to avoid modification of original
            var projected = projection(new Stack<T>(stack.Reverse()), node);

            yield return projected;

            stack.Push(node);

            var enumerator = getChildEnumerator(node);

            if (enumerator != null)
            {
                foreach (var child in enumerator)
                {
                    foreach (var childOrDescendant in child.FlattenHierarchy(getChildEnumerator, projection, stack))
                    {
                        yield return childOrDescendant;
                    }
                }
            }

            stack.Pop();
        }
    }
}
