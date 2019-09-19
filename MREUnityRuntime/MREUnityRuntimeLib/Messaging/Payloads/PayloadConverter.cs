// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using MixedRealityExtension.API;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;

namespace MixedRealityExtension.Messaging.Payloads
{
	public class PayloadConverter : JsonConverter
	{    
		public override bool CanConvert(Type objectType)
		{
			return objectType == typeof(Payload);
		}
	
		public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
		{
			try
			{
				JObject jObject = JObject.Load(reader);
				var networkType = jObject["type"].ToObject<string>();
				Payload payload = PayloadTypeRegistry.CreatePayloadFromNetwork(networkType);
				serializer.Populate(jObject.CreateReader(), payload);

				return payload;
			}
			catch (Exception e)
			{
				MREAPI.Logger.LogError($"Failed to create payload from json.  Exception: {e.Message}\nStack Trace: {e.StackTrace}");
				throw;
			}
		}
	
		public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
		{
			JToken token = JToken.FromObject(value);
			token.WriteTo(writer);
		}
	}

	[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
	public class PayloadType : Attribute
	{
		public string NetworkType { get; }

		public Type ClassType { get; }

		public PayloadType(Type classType, string networkType)
		{
			ClassType = classType;
			NetworkType = networkType;
		}
	}
}
