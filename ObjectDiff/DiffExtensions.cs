using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using System.Collections;
using System.Runtime.CompilerServices;

namespace ObjectDiff
{
    public static class DiffExtensions
    {
        public const string DiffIgnoreKey = "DiffIgnore";

        public static DiffMetadata Diff<TAfter, TBefore>(this TAfter after, TBefore before)
        {
            var beforeMeta = ModelMetadataProviders.Current.GetMetadataForType(() => before, typeof(TBefore));
            var afterMeta = ModelMetadataProviders.Current.GetMetadataForType(() => after, typeof(TAfter));

            var beforeVisited = new Stack<object>();
            var afterVisited = new Stack<object>();

            return DiffProperty(beforeMeta, beforeVisited, null, afterMeta, afterVisited, null);
        }

        static DiffMetadata DiffProperty(ModelMetadata beforeMeta, Stack<object> beforeVisited, int? beforeIndex, ModelMetadata afterMeta, Stack<object> afterVisited, int? afterIndex)
        {
            ICollection<DiffMetadata> properties = null;
            ICollection<DiffMetadata> items = null;

            var comparer = ObjectReferenceEqualityComparer<object>.Default;

            if (beforeMeta != null)
            {
                if (beforeVisited.Contains(beforeMeta.Model, comparer))
                {
                    //TODO: handle silently?
                    //throw new InvalidOperationException("Circular reference in before graph.");
                    return null;
                }

                if (beforeMeta.Model != null)
                {
                    beforeVisited.Push(beforeMeta.Model);
                }
            }

            if (afterMeta != null)
            {
                if (afterVisited.Contains(afterMeta.Model, comparer))
                {
                    //TODO: handle silently?
                    //throw new InvalidOperationException("Circular reference in after graph.");
                    return null;
                }

                if (afterMeta.Model != null)
                {
                    afterVisited.Push(afterMeta.Model);
                }
            }

            if ((beforeMeta != null && beforeMeta.IsComplexType && typeof(IDictionary).IsAssignableFrom(beforeMeta.ModelType))
                || (afterMeta != null && afterMeta.IsComplexType && typeof(IDictionary).IsAssignableFrom(afterMeta.ModelType)))
            {
                //its a dictionary, so do special handling
                throw new NotImplementedException();
            }

            if ((beforeMeta != null && beforeMeta.IsComplexType && typeof(IEnumerable).IsAssignableFrom(beforeMeta.ModelType))
                || (afterMeta != null && afterMeta.IsComplexType && typeof(IEnumerable).IsAssignableFrom(afterMeta.ModelType)))
            {
                var beforeItemType = beforeMeta != null ? GetElementType(beforeMeta.ModelType) : null;
                var afterItemType = afterMeta != null ? GetElementType(afterMeta.ModelType) : null;

                //its a collection, so do special handling
                var beforeList = (beforeMeta.Model as IEnumerable ?? new object[] { })
                    .Cast<object>()
                    .Select((o, i) => new { o, i });

                var afterList = (afterMeta.Model as IEnumerable ?? new object[] { })
                    .Cast<object>()
                    .Select((o, i) => new { o, i });

                items = beforeList
                    .FullOuterJoin(afterList, k => k.o, k => k.o, (before, after, k) => new { before = before != null ? before.o : null, after = after != null ? after.o : null, beforeIndex = before != null ? (int?) before.i : null, afterIndex = after != null ? (int?) after.i : null })
                    .OrderBy(i => i.afterIndex)
                    .ThenBy(i => i.beforeIndex)
                    .Select(item =>
                    {
                        var beforeItemMeta = beforeItemType != null ? ModelMetadataProviders.Current.GetMetadataForType(() => item.before, beforeItemType) : null;
                        var afterItemMeta = afterItemType != null ? ModelMetadataProviders.Current.GetMetadataForType(() => item.after, afterItemType) : null;

                        return DiffProperty(item.beforeIndex != null ? beforeItemMeta : null, beforeVisited, item.beforeIndex, item.afterIndex != null ? afterItemMeta : null, afterVisited, item.afterIndex);
                    })
                    .Where(d => d != null)
                    .ToList();
            }

            if (items == null)
            {
                //only do properties if no items and parent is complex type

                if (beforeMeta != null && afterMeta != null && beforeMeta.IsComplexType && afterMeta.IsComplexType)
                {
                    properties = beforeMeta.Properties
                        .Where(property => !property.AdditionalValues.ContainsKey(DiffIgnoreKey))
                        .FullOuterJoin(afterMeta.Properties.Where(property => !property.AdditionalValues.ContainsKey(DiffIgnoreKey)), m => m.PropertyName, m => m.PropertyName, (b, a, key) => new { before = b, after = a })
                        .Select(property => DiffProperty(property.before, beforeVisited, null, property.after, afterVisited, null))
                        .Where(d => d != null)
                        .ToList();
                }
                else if (afterMeta != null && afterMeta.IsComplexType)
                {
                    properties = afterMeta.Properties
                        .Where(property => !property.AdditionalValues.ContainsKey(DiffIgnoreKey))
                        .Select(p => DiffProperty(null, beforeVisited, null, p, afterVisited, null))
                        .Where(d => d != null)
                        .ToList();
                }
                else if (beforeMeta != null && beforeMeta.IsComplexType)
                {
                    properties = beforeMeta.Properties
                        .Where(property => !property.AdditionalValues.ContainsKey(DiffIgnoreKey))
                        .Select(p => DiffProperty(p, beforeVisited, null, null, afterVisited, null))
                        .Where(d => d != null)
                        .ToList();
                }
            }

            if (beforeMeta != null && beforeMeta.Model != null)
            {
                beforeVisited.Pop();
            }

            if (afterMeta != null && afterMeta.Model != null)
            {
                afterVisited.Pop();
            }

            return new DiffMetadata(beforeMeta, afterMeta, beforeIndex, afterIndex, properties, items, items != null);
        }

        //from http://stackoverflow.com/questions/1890058/iequalitycomparert-that-uses-referenceequals
        class ObjectReferenceEqualityComparer<T> : EqualityComparer<T> where T : class
        {
            private static IEqualityComparer<T> _defaultComparer;

            public new static IEqualityComparer<T> Default
            {
                get { return _defaultComparer ?? (_defaultComparer = new ObjectReferenceEqualityComparer<T>()); }
            }

            #region IEqualityComparer<T> Members

            public override bool Equals(T x, T y)
            {
                return ReferenceEquals(x, y);
            }

            public override int GetHashCode(T obj)
            {
                return RuntimeHelpers.GetHashCode(obj);
            }

            #endregion
        }

        private static Type GetElementType(Type seqType)
        {
            Type ienum = FindIEnumerable(seqType);
            if (ienum == null)
                return null;
            return ienum.GetGenericArguments()[0];
        }
        private static Type FindIEnumerable(Type seqType)
        {
            if (seqType == null || seqType == typeof(string))
                return null;
            if (seqType.IsArray)
                return typeof(IEnumerable<>).MakeGenericType(seqType.GetElementType());
            if (seqType.IsGenericType)
            {
                foreach (Type arg in seqType.GetGenericArguments())
                {
                    Type ienum = typeof(IEnumerable<>).MakeGenericType(arg);
                    if (ienum.IsAssignableFrom(seqType))
                    {
                        return ienum;
                    }
                }
            }
            Type[] ifaces = seqType.GetInterfaces();
            if (ifaces != null && ifaces.Length > 0)
            {
                foreach (Type iface in ifaces)
                {
                    Type ienum = FindIEnumerable(iface);
                    if (ienum != null)
                        return ienum;
                }
            }
            if (seqType.BaseType != null && seqType.BaseType != typeof(object))
            {
                return FindIEnumerable(seqType.BaseType);
            }
            return null;
        }
    }
}
