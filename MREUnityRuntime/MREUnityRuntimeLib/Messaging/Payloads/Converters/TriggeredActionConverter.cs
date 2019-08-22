// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using MixedRealityExtension.API;
using MixedRealityExtension.Triggers.TriggeredActions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;

namespace MixedRealityExtension.Messaging.Payloads.Converters
{
    /// <summary>
    /// Json converter for triggered action serialization data.
    /// </summary>
    class TriggeredActionConverter : JsonConverter
    {
        /// <inheritdoc />
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(TriggeredActionBase);
        }

        /// <inheritdoc />
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            try
            {
                JObject jObject = JObject.Load(reader);
                var triggeredActionType = jObject["type"].ToObject<string>();

                TriggeredActionBase triggeredAction = null;
                switch (triggeredActionType)
                {
                    case "play-animation":
                        triggeredAction = new PlayAnimationTriggeredAction();
                        break;
                    case "play-sound":
                        triggeredAction = new PlaySoundTriggeredAction();
                        break;
                    default:
                        MREAPI.Logger.LogError($"Failed to deserialize triggered action.  Invalid triggered action type <{triggeredActionType}>.");
                        break;
                }

                serializer.Populate(jObject.CreateReader(), triggeredAction);

                return triggeredAction;
            }
            catch (Exception e)
            {
                MREAPI.Logger.LogError($"Failed to create triggered action from json.  Exception: {e.Message}\nStack Trace: {e.StackTrace}");
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
