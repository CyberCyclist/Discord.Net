﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Formatting;
using System.Text.Json;

namespace Discord.Serialization.Json
{
    public class JsonSerializer : Serializer
    {
        private static readonly Lazy<JsonSerializer> _singleton = new Lazy<JsonSerializer>();
        public static JsonSerializer Global => _singleton.Value;

        public JsonSerializer()
        {
            AddConverter<sbyte, Converters.Int8PropertyConverter>();
            AddConverter<short, Converters.Int16PropertyConverter>();
            AddConverter<int, Converters.Int32PropertyConverter>();
            AddConverter<long, Converters.Int64PropertyConverter>();

            AddConverter<byte, Converters.UInt8PropertyConverter>();
            AddConverter<ushort, Converters.UInt16PropertyConverter>();
            AddConverter<uint, Converters.UInt32PropertyConverter>();
            AddConverter<ulong, Converters.UInt64PropertyConverter>();

            AddConverter<float, Converters.SinglePropertyConverter>();
            AddConverter<double, Converters.DoublePropertyConverter>();
            AddConverter<decimal, Converters.DecimalPropertyConverter>();
            
            //AddConverter<char, Converters.CharPropertyConverter>(); //TODO: char.Parse does not support Json.Net's serialization
            AddConverter<string, Converters.StringPropertyConverter>();

            AddConverter<DateTime, Converters.DateTimePropertyConverter>();
            AddConverter<DateTimeOffset, Converters.DateTimeOffsetPropertyConverter>();

            AddConverter<bool, Converters.BooleanPropertyConverter>();
            AddConverter<Guid, Converters.GuidPropertyConverter>();

            AddConverter<object, Converters.DynamicPropertyConverter>(
                (type, prop) => prop.GetCustomAttributes<ModelSelectorAttribute>().Any());

            AddGenericConverter(typeof(List<>), typeof(Converters.ListPropertyConverter<>));
            AddGenericConverter(typeof(Nullable<>), typeof(Converters.NullablePropertyConverter<>));

            AddGenericConverter(typeof(Converters.StringEnumPropertyConverter<>), 
                (type, prop) => type.IsEnum && prop.GetCustomAttribute<ModelStringEnumAttribute>() != null);
            AddGenericConverter(typeof(Converters.Int64EnumPropertyConverter<>), 
                (type, prop) => type.IsEnum && IsSignedEnum(Enum.GetUnderlyingType(type.AsType())));
            AddGenericConverter(typeof(Converters.UInt64EnumPropertyConverter<>),
                (type, prop) => type.IsEnum && IsUnsignedEnum(Enum.GetUnderlyingType(type.AsType())));
            AddGenericConverter(typeof(Converters.ObjectPropertyConverter<>), 
                (type, prop) => type.IsClass);
        }
        protected JsonSerializer(JsonSerializer parent) : base(parent) { }
        public JsonSerializer CreateScope() => new JsonSerializer(this);

        public void AddConverter<TValue, TConverter>()
            where TConverter : class, IJsonPropertyConverter<TValue>
            => AddConverter(typeof(TValue), typeof(TConverter));
        public void AddConverter<TValue, TConverter>(Func<TypeInfo, PropertyInfo, bool> condition)
            where TConverter : class, IJsonPropertyConverter<TValue>
            => AddConverter(typeof(TValue), typeof(TConverter), condition);

        protected override PropertyMap CreatePropertyMap<TModel, TValue>(PropertyInfo propInfo)
        {
            var converter = (IJsonPropertyConverter<TValue>)GetConverter(typeof(TValue), propInfo);
            return new JsonPropertyMap<TModel, TValue>(this, propInfo, converter);
        }

        public override TModel Read<TModel>(ReadOnlyBuffer<byte> data)
        {
            var reader = new JsonReader(data.Span, SymbolTable.InvariantUtf8);
            if (!reader.Read())
                return default;
            var converter = GetConverter(typeof(TModel)) as IJsonPropertyConverter<TModel>;
            return converter.Read(null, null, ref reader, false);
        }
        public override void Write<TModel>(ArrayFormatter stream, TModel model)
        {
            var writer = new JsonWriter(stream);
            var converter = GetConverter(typeof(TModel)) as IJsonPropertyConverter<TModel>;
            converter.Write(null, null, ref writer, model, false);
        }

        private static bool IsSignedEnum(Type underlyingType)
            => underlyingType == typeof(sbyte) ||
                underlyingType == typeof(short) ||
                underlyingType == typeof(int) ||
                underlyingType == typeof(long);
        private static bool IsUnsignedEnum(Type underlyingType)
            => underlyingType == typeof(byte) ||
                underlyingType == typeof(ushort) ||
                underlyingType == typeof(uint) ||
                underlyingType == typeof(ulong);
    }
}
