// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using MixedRealityExtension.API;
using MixedRealityExtension.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;

namespace MixedRealityExtension.Messaging.Payloads.Converters
{
	/// <summary>
	/// Json converter for collision geometry serialization data.
	/// </summary>
	public class CollisionGeometryConverter : JsonConverter
	{
		/// <inheritdoc />
		public override bool CanConvert(Type objectType)
		{
			return objectType == typeof(ColliderGeometry);
		}

		/// <inheritdoc />
		public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
		{
			try
			{
				JObject jObject = JObject.Load(reader);
				var colliderType = jObject["shape"].ToObject<string>();

				ColliderGeometry colliderGeometry = null;
				switch (colliderType)
				{
					case "sphere":
						colliderGeometry = new SphereColliderGeometry();
						break;
					case "box":
						colliderGeometry = new BoxColliderGeometry();
						break;
					case "capsule":
						colliderGeometry = new CapsuleColliderGeometry();
						break;
					case "mesh":
						colliderGeometry = new MeshColliderGeometry();
						break;
					case "auto":
						colliderGeometry = new AutoColliderGeometry();
						break;
					default:
						MREAPI.Logger.LogError($"Failed to deserialize collider geometry.  Invalid collider type <{colliderType}>.");
						break;
				}

				serializer.Populate(jObject.CreateReader(), colliderGeometry);

				return colliderGeometry;
			}
			catch (Exception e)
			{
				MREAPI.Logger.LogError($"Failed to create collider geometry from json.  Exception: {e.Message}\nStack Trace: {e.StackTrace}");
				throw;
			}
		}

		/// <inheritdoc />
		public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
		{
			writer.WriteValue(value);
		}
	}
}
