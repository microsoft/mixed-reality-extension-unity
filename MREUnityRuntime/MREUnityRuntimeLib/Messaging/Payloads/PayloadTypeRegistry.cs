// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.Linq;

namespace MixedRealityExtension.Messaging.Payloads
{
	[PayloadType(typeof(ActionPerformed), "perform-action")]
	[PayloadType(typeof(ActorCorrection), "actor-correction")]
	[PayloadType(typeof(ActorUpdate), "actor-update")]
	[PayloadType(typeof(AnimationUpdate), "animation-update")]
	[PayloadType(typeof(AppToEngineRPC), "app2engine-rpc")]
	[PayloadType(typeof(AssetsLoaded), "assets-loaded")]
	[PayloadType(typeof(AssetUpdate), "asset-update")]
	[PayloadType(typeof(CreateAnimation), "create-animation")]
	[PayloadType(typeof(CreateAnimation2), "create-animation-2")]
	[PayloadType(typeof(CreateAsset), "create-asset")]
	[PayloadType(typeof(CreateEmpty), "create-empty")]
	[PayloadType(typeof(CreateFromLibrary), "create-from-library")]
	[PayloadType(typeof(CreateFromPrefab), "create-from-prefab")]
	[PayloadType(typeof(CollisionEventRaised), "collision-event-raised")]
	[PayloadType(typeof(DestroyActors), "destroy-actors")]
	[PayloadType(typeof(DestroyAnimations), "destroy-animations")]
	[PayloadType(typeof(DialogResponse), "dialog-response")]
	[PayloadType(typeof(EngineToAppRPC), "engine2app-rpc")]
	[PayloadType(typeof(Handshake), "handshake")]
	[PayloadType(typeof(HandshakeReply), "handshake-reply")]
	[PayloadType(typeof(HandshakeComplete), "handshake-complete")]
	[PayloadType(typeof(Heartbeat), "heartbeat")]
	[PayloadType(typeof(HeartbeatReply), "heartbeat-reply")]
	[PayloadType(typeof(InterpolateActor), "interpolate-actor")]
	[PayloadType(typeof(LoadAssets), "load-assets")]
	[PayloadType(typeof(LocalCommand), "local-command")]
	[PayloadType(typeof(MultiOperationResult), "multi-operation-result")]
	[PayloadType(typeof(ObjectSpawned), "object-spawned")]
	[PayloadType(typeof(OperationResult), "operation-result")]
	[PayloadType(typeof(PhysicsBridgeUpdate), "physicsbridge-transforms-update")]
	[PayloadType(typeof(PhysicsTranformServerUpload), "physicsbridge-server-transforms-upload")]
	[PayloadType(typeof(RBAddForce), "rigidbody-add-force")]
	[PayloadType(typeof(RBAddForceAtPosition), "rigidbody-add-force-at-position")]
	[PayloadType(typeof(RBAddRelativeTorque), "rigidbody-add-relative-torque")]
	[PayloadType(typeof(RBAddTorque), "rigidbody-add-torque")]
	[PayloadType(typeof(RBMovePosition), "rigidbody-move-position")]
	[PayloadType(typeof(RBMoveRotation), "rigidbody-move-rotation")]
	[PayloadType(typeof(RigidBodyCommands), "rigidbody-commands")]
	[PayloadType(typeof(SetAnimationState), "set-animation-state")]
	[PayloadType(typeof(SetAuthoritative), "set-authoritative")]
	[PayloadType(typeof(SetBehavior), "set-behavior")]
	[PayloadType(typeof(SetMediaState), "set-media-state")]
	[PayloadType(typeof(ShowDialog), "show-dialog")]
	[PayloadType(typeof(StateRestore), "state-restore")]
	[PayloadType(typeof(SyncAnimations), "sync-animations")]
	[PayloadType(typeof(SyncComplete), "sync-complete")]
	[PayloadType(typeof(SyncRequest), "sync-request")]
	[PayloadType(typeof(Traces), "traces")]
	[PayloadType(typeof(TriggerEventRaised), "trigger-event-raised")]
	[PayloadType(typeof(UnloadAssets), "unload-assets")]
	[PayloadType(typeof(UserJoined), "user-joined")]
	[PayloadType(typeof(UserLeft), "user-left")]
	[PayloadType(typeof(UserUpdate), "user-update")]
	internal static class PayloadTypeRegistry
	{
		private static Dictionary<string, Type> _stringToPayloadMap;
		private static Dictionary<Type, string> _payloadToStringMap;

		static PayloadTypeRegistry()
		{
			_stringToPayloadMap = new Dictionary<string, Type>();
			_payloadToStringMap = new Dictionary<Type, string>();

			var payloadTypes = typeof(PayloadTypeRegistry)
				.GetCustomAttributes(typeof(PayloadType), false)
				.Select(attr => attr as PayloadType);

			foreach (var payloadType in payloadTypes)
			{
				_stringToPayloadMap.Add(payloadType.NetworkType, payloadType.ClassType);
				_payloadToStringMap.Add(payloadType.ClassType, payloadType.NetworkType);
			}
		}

		public static Payload CreatePayloadFromNetwork(string networkType)
		{
			if (_stringToPayloadMap.ContainsKey(networkType))
			{
				return (Payload)Activator.CreateInstance(_stringToPayloadMap[networkType]);
			}

			// We do not have this type registered.
			return null;
		}

		public static string GetNetworkType(Type payloadType)
		{
			return _payloadToStringMap[payloadType];
		}
	}
}
