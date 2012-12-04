using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using System.Globalization;

namespace ObjectDiff
{
    public sealed class DiffMetadata
    {
        ModelMetadata _beforeMeta;
        ModelMetadata _afterMeta;

        public DiffMetadata(ModelMetadata beforeMeta, ModelMetadata afterMeta, int? beforeIndex, int? afterIndex, ICollection<DiffMetadata> properties, ICollection<DiffMetadata> items, bool isCollection)
        {
            _beforeMeta = beforeMeta;
            _afterMeta = afterMeta;

            BeforeIndex = beforeIndex;
            AfterIndex = afterIndex;
            Properties = properties ?? new DiffMetadata[] { };
            Items = items ?? new DiffMetadata[] { };
            IsCollection = isCollection;
        }

        public ICollection<DiffMetadata> Properties { get; private set; }
        public ICollection<DiffMetadata> Items { get; private set; }
        public int? BeforeIndex { get; private set; }
        public int? AfterIndex { get; private set; }
        public bool IsCollection { get; private set; }

        public bool IsCollectionItem { get { return BeforeIndex != null || AfterIndex != null; } }
        public bool PropertyInBefore { get { return _beforeMeta != null; } }
        public bool PropertyInAfter { get { return _afterMeta != null; } }
        public string PropertyName { get { return _beforeMeta != null ? _beforeMeta.PropertyName : _afterMeta.PropertyName; } }
        public bool IsComplexType { get { return (_beforeMeta != null && _beforeMeta.IsComplexType) || (_afterMeta != null && _afterMeta.IsComplexType); } }
        public Type BeforeType { get { return _beforeMeta != null ? _beforeMeta.ModelType : null; } }
        public Type AfterType { get { return _afterMeta != null ? _afterMeta.ModelType : null; } }
        public int Order { get { return _afterMeta != null ? _afterMeta.Order : int.MaxValue; } }
        public object Before
        {
            get
            {
                if (_beforeMeta == null)
                    return null;

                return _beforeMeta.Model;
            }
        }
        public object After
        {
            get
            {
                if (_afterMeta == null)
                    return null;

                return _afterMeta.Model;
            }
        }

        public string DisplayName { get { return _afterMeta != null ? _afterMeta.GetDisplayName() : _beforeMeta.GetDisplayName(); } }

        public object BeforeFormattedValue
        {
            get
            {
                return GetDisplayFormattedValue(_beforeMeta);
            }
        }

        public object AfterFormattedValue
        {
            get
            {
                return GetDisplayFormattedValue(_afterMeta);
            }
        }

        static object GetDisplayFormattedValue(ModelMetadata meta)
        {
            if (meta == null)
            {
                return null;
            }

            if (meta.Model == null)
            {
                return meta.NullDisplayText;
            }

            if (!String.IsNullOrEmpty(meta.DisplayFormatString))
            {
                return String.Format(CultureInfo.CurrentCulture, meta.DisplayFormatString, meta.Model);
            }

            return meta.Model;
        }
    }
}
