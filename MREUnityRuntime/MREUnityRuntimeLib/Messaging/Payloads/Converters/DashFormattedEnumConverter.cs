// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace MixedRealityExtension.Messaging.Payloads.Converters
{
    /// <summary>
    /// Json converter for the OperatingModel enum.
    /// </summary>
    public class DashFormattedEnumConverter : JsonConverter
    {
        /// <inheritdoc />
        public override bool CanConvert(Type objectType)
        {
            return objectType.IsEnum;
        }

        /// <inheritdoc />
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var value = (string)reader.Value;
            value = value.Replace("-", "");
            return Enum.Parse(objectType, value, true);
        }

        /// <inheritdoc />
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var name = Enum.GetName(value.GetType(), value);

            var sb = new StringBuilder();

            for (var i = 0; i < name.Length; ++i)
            {
                if (i > 0 && Char.IsUpper(name[i]))
                {
                    sb.Append('-');
                }
                sb.Append(Char.ToLower(name[i]));
            }

            writer.WriteValue(sb.ToString());
        }
    }
}
