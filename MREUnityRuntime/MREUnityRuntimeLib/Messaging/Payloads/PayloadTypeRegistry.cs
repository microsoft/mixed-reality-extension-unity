// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using MixedRealityExtension.Assets;

namespace MixedRealityExtension.Messaging.Payloads
{
    [PayloadType(typeof(Traces), "traces")]
    [PayloadType(typeof(Handshake), "handshake")]
    [PayloadType(typeof(HandshakeReply), "handshake-reply")]
    [PayloadType(typeof(HandshakeComplete), "handshake-complete")]
    [PayloadType(typeof(EngineToAppRPC), "engine2app-rpc")]
    [PayloadType(typeof(AppToEngineRPC), "app2engine-rpc")]
    [PayloadType(typeof(CreateFromLibrary), "create-from-library")]
    [PayloadType(typeof(CreateFromGLTF), "create-from-gltf")]
    [PayloadType(typeof(CreatePrimitive), "create-primitive")]
    [PayloadType(typeof(CreateEmpty), "create-empty")]
    [PayloadType(typeof(CreateFromPrefab), "create-from-prefab")]
    [PayloadType(typeof(ObjectSpawned), "object-spawned")]
    [PayloadType(typeof(ActorUpdate), "actor-update")]
    [PayloadType(typeof(ActorCorrection), "actor-correction")]
    [PayloadType(typeof(DestroyActors), "destroy-actors")]
    [PayloadType(typeof(StateUpdate), "state-update")]
    [PayloadType(typeof(StateRestore), "state-restore")]
    [PayloadType(typeof(OperationResult), "operation-result")]
    [PayloadType(typeof(MultiOperationResult), "multi-operation-result")]
    [PayloadType(typeof(EnableLight), "enable-light")]
    [PayloadType(typeof(EnableText), "enable-text")]
    [PayloadType(typeof(EnableRigidBody), "enable-rigidbody")]
    [PayloadType(typeof(UpdateSubscriptions), "update-subscriptions")]
    [PayloadType(typeof(RigidBodyCommands), "rigidbody-commands")]
    [PayloadType(typeof(RBMovePosition), "rigidbody-move-position")]
    [PayloadType(typeof(RBMoveRotation), "rigidbody-move-rotation")]
    [PayloadType(typeof(RBAddForce), "rigidbody-add-force")]
    [PayloadType(typeof(RBAddForceAtPosition), "rigidbody-add-force-at-position")]
    [PayloadType(typeof(RBAddTorque), "rigidbody-add-torque")]
    [PayloadType(typeof(RBAddRelativeTorque), "rigidbody-add-relative-torque")]
    [PayloadType(typeof(UserUpdate), "user-update")]
    [PayloadType(typeof(UserJoined), "user-joined")]
    [PayloadType(typeof(UserLeft), "user-left")]
    [PayloadType(typeof(CreateAnimation), "create-animation")]
    [PayloadType(typeof(DEPRECATED_StartAnimation), "start-animation")]
    [PayloadType(typeof(DEPRECATED_StopAnimation), "stop-animation")]
    [PayloadType(typeof(DEPRECATED_ResetAnimation), "reset-animation")]
    [PayloadType(typeof(DEPRECATED_PauseAnimation), "pause-animation")]
    [PayloadType(typeof(DEPRECATED_ResumeAnimation), "resume-animation")]
    [PayloadType(typeof(SyncAnimations), "sync-animations")]
    [PayloadType(typeof(SetAnimationState), "set-animation-state")]
    [PayloadType(typeof(InterpolateActor), "interpolate-actor")]
    [PayloadType(typeof(ActionPerformed), "perform-action")]
    [PayloadType(typeof(SetBehavior), "set-behavior")]
    [PayloadType(typeof(SyncRequest), "sync-request")]
    [PayloadType(typeof(SyncComplete), "sync-complete")]
    [PayloadType(typeof(SetAuthoritative), "set-authoritative")]
    [PayloadType(typeof(Heartbeat), "heartbeat")]
    [PayloadType(typeof(HeartbeatReply), "heartbeat-reply")]
    [PayloadType(typeof(LoadAssets), "load-assets")]
    [PayloadType(typeof(AssetsLoaded), "assets-loaded")]
    [PayloadType(typeof(LookAt), "look-at")]
    [PayloadType(typeof(AssetUpdate), "asset-update")]
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
