﻿using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MzLite.Json
{
    public class ConvertibleConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return typeof(IConvertible).IsAssignableFrom(objectType);
        }

        IConvertible ReadValue(JsonReader reader, TypeCode tc, JToken jtval, JsonSerializer serializer)
        {
            switch (tc)
            {
                case TypeCode.Boolean:
                    return jtval.ToObject<bool>(serializer);
                case TypeCode.Byte:
                    return jtval.ToObject<byte>(serializer);
                case TypeCode.Char:
                    return jtval.ToObject<char>(serializer);
                case TypeCode.DateTime:
                    return jtval.ToObject<DateTime>(serializer);
                case TypeCode.DBNull:
                    return null;
                case TypeCode.Decimal:
                    return jtval.ToObject<decimal>(serializer);
                case TypeCode.Double:
                    return jtval.ToObject<double>(serializer);
                case TypeCode.Empty:
                    return null;
                case TypeCode.Int16:
                    return jtval.ToObject<Int16>(serializer);
                case TypeCode.Int32:
                    return jtval.ToObject<Int32>(serializer);
                case TypeCode.Int64:
                    return jtval.ToObject<Int64>(serializer);
                case TypeCode.Object:
                    throw new JsonSerializationException("Object type not supported.");
                case TypeCode.SByte:
                    return jtval.ToObject<sbyte>(serializer);
                case TypeCode.Single:
                    return jtval.ToObject<float>(serializer);
                case TypeCode.String:
                    return jtval.ToObject<string>(serializer);
                case TypeCode.UInt16:
                    return jtval.ToObject<UInt16>(serializer);
                case TypeCode.UInt32:
                    return jtval.ToObject<UInt32>(serializer);
                case TypeCode.UInt64:
                    return jtval.ToObject<UInt64>(serializer);
                default:
                    throw new JsonSerializationException("Type not supported: " + tc.ToString());
            }
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            JToken jt = JToken.Load(reader);

            if (jt.Type == JTokenType.Null)
            {
                return null;
            }
            else if (jt.Type == JTokenType.Object)
            {
                JObject jo = (JObject)jt;
                JToken jtval;

                if (jo.TryGetValue("$tc", out jtval))
                {
                    TypeCode tc = jtval.ToObject<TypeCode>(serializer);

                    if (jo.TryGetValue("$val", out jtval))
                    {
                        return ReadValue(reader, tc, jtval, serializer);
                    }
                    else
                    {
                        throw new JsonSerializationException("$val property expected.");
                    }
                }
                else
                {
                    throw new JsonSerializationException("$tc property expected.");
                }
            }
            else
            {
                throw new JsonSerializationException("Object token expected.");
            }
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {

            if (value == null)
            {
                writer.WriteNull();
                return;
            }

            IConvertible ic = value as IConvertible;

            if (ic.GetTypeCode() == TypeCode.Object)
                throw new JsonSerializationException("Object type code not supported.");

            writer.WriteStartObject();
            writer.WritePropertyName("$tc");
            serializer.Serialize(writer, ic.GetTypeCode());
            writer.WritePropertyName("$val");
            serializer.Serialize(writer, ic);
            writer.WriteEndObject();
        }
    }
}
