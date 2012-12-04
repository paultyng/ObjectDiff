using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ObjectDiff
{
    public static class ReadableDiffExtensions
    {

        const string ReadablePropertySeparator = " - ";
        const string ReadablePropertyValueSeparator = ": ";

        public static string ReadableDiff<TAfter, TBefore>(this TAfter after, TBefore before)
        {
            var sb = new StringBuilder();
            var diff = DiffExtensions.Diff(after, before);

            ReadableDiffInternal(sb, diff);

            return sb.ToString();
        }

        static bool ReadablePropertyFilter(Tuple<Stack<DiffMetadata>, DiffMetadata> property)
        {
            //null / empty strings are effectively equal for readable diff
            if (typeof(string) == property.Item2.BeforeType
                && typeof(string) == property.Item2.AfterType
                && String.IsNullOrWhiteSpace((string) property.Item2.Before)
                && String.IsNullOrWhiteSpace((string) property.Item2.After))
            {
                return false;
            }

            return (!property.Item2.PropertyInAfter
                || !property.Item2.PropertyInBefore
                || !Object.Equals(property.Item2.Before, property.Item2.After))
                && !property.Item2.IsCollection
                && !property.Item2.IsComplexType;
        }

        static void BuildDisplayName(StringBuilder sb, DiffMetadata property)
        {
            if (!property.IsCollectionItem)
            {
                sb.Append(property.DisplayName);
            }
            else
            {
                if (property.AfterIndex != null)
                {
                    if (property.AfterIndex == property.BeforeIndex)
                    {
                        sb.AppendFormat("[{0}]", property.AfterIndex + 1);
                    }
                    else
                    {
                        if (property.BeforeIndex == null)
                        {
                            sb.AppendFormat("[{0}, added]", property.AfterIndex + 1);
                        }
                        else
                        {
                            sb.AppendFormat("[{0}, was {1}]", property.AfterIndex + 1, property.BeforeIndex + 1);
                        }
                    }
                }
                else if (property.BeforeIndex != null)
                {
                    sb.Append("[removed]");
                }
            }
        }

        static void BuildDisplayName(StringBuilder sb, Tuple<Stack<DiffMetadata>, DiffMetadata> property)
        {
            if (property.Item1.Count > 0)
            {
                foreach (var parent in property.Item1.Reverse().Skip(1))
                {
                    BuildDisplayName(sb, parent);
                    sb.Append(ReadablePropertySeparator);
                }
            }

            BuildDisplayName(sb, property.Item2);
        }

        static void OutputValue(StringBuilder sb, object formattedValue)
        {
            var stringValue = formattedValue as string;

            if (!String.IsNullOrWhiteSpace(stringValue))
            {
                if (stringValue.Contains('\n'))
                {
                    //output multi line value special case

                    sb.AppendLine();
                    sb.AppendLine("-----");
                    sb.AppendLine(stringValue);
                    sb.AppendLine("-----");
                    return;
                }
            }

            sb.Append("'");
            sb.Append(formattedValue);
            sb.Append("'");
        }

        sealed class DiffStackComparer : IComparer<Tuple<Stack<DiffMetadata>, DiffMetadata>>
        {
            public DiffStackComparer()
            {
            }

            public int Compare(Tuple<Stack<DiffMetadata>, DiffMetadata> x, Tuple<Stack<DiffMetadata>, DiffMetadata> y)
            {
                var xList = x.Item1.Reverse().Append(x.Item2).ToList();
                var yList = y.Item1.Reverse().Append(y.Item2).ToList();

                for (var i = 0; i < Math.Min(xList.Count, yList.Count) + 1; i++)
                {
                    if (xList.Count <= i)
                    {
                        if (xList.Count == yList.Count)
                        {
                            return 0;
                        }

                        return -1;
                    }

                    if (yList.Count <= i)
                    {
                        return 1;
                    }

                    var compare = Comparer<int>.Default.Compare(xList[i].AfterIndex.GetValueOrDefault(int.MaxValue), yList[i].AfterIndex.GetValueOrDefault(int.MaxValue));

                    if (compare != 0)
                        return compare;

                    compare = Comparer<int>.Default.Compare(xList[i].Order, yList[i].Order);

                    if (compare != 0)
                        return compare;

                    compare = StringComparer.CurrentCulture.Compare(xList[i].DisplayName, yList[i].DisplayName);

                    if (compare != 0)
                        return compare;
                }

                return 0;
            }
        }

        static void ReadableDiffInternal(StringBuilder sb, DiffMetadata diff)
        {
            var filteredProperties = diff
                .FlattenHierarchy<DiffMetadata, Tuple<Stack<DiffMetadata>, DiffMetadata>>(child => child.Properties.Concat(child.Items), (stack, child) => Tuple.Create(stack, child))
                .Where(ReadablePropertyFilter);

            var properties = filteredProperties.OrderBy(s => s, new DiffStackComparer());

            //.ThenBy(p => p.Item1.Count > 0 ? (int?) p.Item1.Peek().AfterIndex.GetValueOrDefault(int.MaxValue) : null)
            //.ThenBy(p => p.Item1.Count > 0 ? (int?) p.Item1.Peek().Order : null)
            //.ThenBy(p => p.Item1.Count > 0 ? p.Item1.Peek().DisplayName : null)
            //.ThenBy(p => p.Item2.AfterIndex.GetValueOrDefault(int.MaxValue))
            //.ThenBy(p => p.Item2.Order)
            //.ThenBy(p => p.Item2.DisplayName);

            foreach (var property in properties)
            {
                BuildDisplayName(sb, property);
                sb.Append(ReadablePropertyValueSeparator);

                if (!property.Item2.PropertyInBefore)
                {
                    OutputValue(sb, property.Item2.AfterFormattedValue);

                    if (sb[sb.Length - 1] != '\n')
                    {
                        sb.Append(", ");
                    }

                    sb.Append("was not present");
                }
                else if (!property.Item2.PropertyInAfter)
                {
                    sb.Append("No value present, was ");
                    OutputValue(sb, property.Item2.BeforeFormattedValue);
                }
                else
                {
                    if (property.Item2.AfterIndex != null || property.Item2.BeforeIndex == null)
                    {
                        OutputValue(sb, property.Item2.AfterFormattedValue);

                        if (property.Item2.AfterIndex == null || property.Item2.BeforeIndex != null)
                        {
                            if (sb[sb.Length - 1] != '\n')
                            {
                                sb.Append(", ");
                            }
                        }
                    }

                    if (property.Item2.AfterIndex == null || property.Item2.BeforeIndex != null)
                    {
                        sb.Append("was ");
                        OutputValue(sb, property.Item2.BeforeFormattedValue);
                    }
                }

                if (sb[sb.Length - 1] != '\n')
                {
                    sb.AppendLine();
                }
            }
        }
    }

}
