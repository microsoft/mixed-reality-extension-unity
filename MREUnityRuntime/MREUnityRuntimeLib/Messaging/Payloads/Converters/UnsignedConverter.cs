// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Text;
using MixedRealityExtension.Util;
using Newtonsoft.Json;

namespace MixedRealityExtension.Messaging.Payloads.Converters
{
	/// <summary>
	/// Converts signed integers into bit-equivalent unsigned integers,
	/// because JS doesn't have a concept of unsigned.
	/// </summary>
	public class UnsignedConverter : JsonConverter
	{
		/// <inheritdoc />
		public override bool CanConvert(Type objectType)
		{
			return UtilMethods.GetActualType(objectType) == typeof(UInt32);
		}

		/// <inheritdoc />
		public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
		{
			if (reader.TokenType == JsonToken.Integer)
			{
				Int32 signed = Convert.ToInt32((Int64)reader.Value);
				byte[] bytes = BitConverter.GetBytes(signed);
				UInt32 unsigned = BitConverter.ToUInt32(bytes, 0);
				return unsigned;
			}
			else
			{
				UnityEngine.Debug.Log($"Failed to deserialize {reader.ValueType} {reader.Value}");
				return null;
			}
		}

		/// <inheritdoc />
		public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
		{
			UInt32 unsigned = (UInt32)value;
			byte[] bytes = BitConverter.GetBytes(unsigned);
			Int32 signed = BitConverter.ToInt32(bytes, 0);
			writer.WriteValue(signed);
		}
	}
}
