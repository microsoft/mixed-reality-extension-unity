// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using MixedRealityExtension.Behaviors;
using MixedRealityExtension.Behaviors.Actions;
using MixedRealityExtension.Patching.Types;
using System;
using System.Collections.Generic;

namespace MixedRealityExtension.Messaging.Payloads
{
    /// <summary>
    /// The operation result code for the message payload.
    /// </summary>
    public enum OperationResultCode
    {
        /// <summary>
        /// The operation was a success.
        /// </summary>
        Success,

        /// <summary>
        /// The operation was a success but contained one or more warnings.
        /// </summary>
        Warning,

        /// <summary>
        /// The operation was not a success and contained one or more errors.
        /// </summary>
        Error
    }

    /// <summary>
    /// The types of components that can be added to an actor.
    /// </summary>
    [Flags]
    public enum ActorComponentType : uint
    {
        /// <summary>
        /// No subscriptions
        /// </summary>
        None = 0,

        /// <summary>
        /// The transform component flag.
        /// The app can subscribe to this component.
        /// </summary>
        Transform = 1 << 0,

        /// <summary>
        /// The rigid body component flag.
        /// The app can subscribe to this component.
        /// </summary>
        Rigidbody = 1 << 1,

        /// <summary>
        /// The light component flag.
        /// The app cannot subscribe to this component.
        /// </summary>
        Light = 1 << 2,

        /// <summary>
        /// The attachment component flag.
        /// The app cannot subscribe to this component.
        /// reall
        /// </summary>
        Attachment = 1 << 3,

        /// <summary>
        /// All subscribable component flags.
        /// </summary>
        AllSubscriptions = Transform | Rigidbody,

        /// <summary>
        /// All possible component flags.
        /// </summary>
        All = 0xffffffff
    }

    /// <summary>
    /// The kind of connection we've made with the app. This setting provides a hint to the client about how and when it should send updates to the app.
    /// </summary>
    public enum OperatingModel
    {
        /// <summary>
        /// The operating model is one with a single authoritative game server. The client is free to send any updates the app has registered for.
        /// </summary>
        ServerAuthoritative,

        /// <summary>
        /// The operating model is one without an authoritative simulation. The client should only send updates if the app has indicated this is the "authoritative" client.
        /// </summary>
        PeerAuthoritative
    }

    /// <summary>
    /// Payload that contains only traces.
    /// </summary>
    public class Traces : Payload
    {
    }

    /// <summary>
    /// The handshake payload from engine to app.
    /// </summary>
    public class Handshake : Payload
    {
    }

    /// <summary>
    /// The reply to the handshake from app to engine.
    /// </summary>
    public class HandshakeReply : Payload
    {
        /// <summary>
        /// The session id associated with this established runtime session.
        /// </summary>
        public string SessionId;

        /// <summary>
        /// The kind of connection this is. <see cref="OperatingModel"/>
        /// </summary>
        public OperatingModel OperatingModel;
    }

    /// <summary>
    /// The handshake complete payload.
    /// </summary>
    public class HandshakeComplete : Payload
    {
    }

    /// <summary>
    /// A payload containing an operation result from the engine to the app.
    /// </summary>
    public class OperationResult : Payload
    {
        /// <summary>
        /// The result code of the operation.
        /// </summary>
        public OperationResultCode ResultCode;

        /// <summary>
        /// The message coming along with the operation result.
        /// </summary>
        public string Message;
    }

    /// <summary>
    /// A payload containing one or more operation results from the engine to the app.
    /// </summary>
    public class MultiOperationResult : Payload
    {
        /// <summary>
        /// Enumeration of results from an operation.
        /// </summary>
        public IEnumerable<OperationResult> Results { get; set; }
    }

    /// <summary>
    /// A payload containing the remote procedure call that should be invoked from the engine to the app.
    /// </summary>
    public class EngineToAppRPC : Payload
    {
        /// <summary>
        /// The procedure name to be called.
        /// </summary>
        public string ProcName { get; set; }

        /// <summary>
        /// The arguments to that procedure call.
        /// </summary>
        public IEnumerable<object> Args { get; set; }
    }

    /// <summary>
    /// A payload containing the operational results of an object spawned command from engine to app.
    /// </summary>
    public class ObjectSpawned : Payload
    {
        /// <summary>
        /// The operation result information.
        /// </summary>
        public OperationResult Result;

        /// <summary>
        /// The enumeration of actors created during the object spawn command operation.
        /// </summary>
        public IEnumerable<ActorPatch> Actors { get; set; }
    }

    /// <summary>
    /// The payload for the client to request the latest application state (sent during handshake).
    /// </summary>
    public class SyncRequest : Payload
    {
    }

    /// <summary>
    /// The payload notifying the client that the application is done synchronizing application state.
    /// </summary>
    public class SyncComplete : Payload
    {
    }

    /// <summary>
    /// The payload containing user information for a user join from engine to app.
    /// </summary>
    public class UserJoined : Payload
    {
        /// <summary>
        /// The initial user patch for the user joining the app.
        /// </summary>
        public UserPatch User;
    }

    /// <summary>
    /// The payload containing the user information for a user leaving from engine to app.
    /// </summary>
    public class UserLeft : Payload
    {
        /// <summary>
        /// User patch of the user leaving the app.
        /// </summary>
        public Guid UserId;
    }

    /// <summary>
    /// Payload for when an action is performed for a behavior on an actor from engine to app.
    /// </summary>
    public class ActionPerformed : Payload 
    {
        /// <summary>
        /// The id of the user performing the action.
        /// </summary>
        public Guid UserId;

        /// <summary>
        /// The actor id for the target of the action.
        /// </summary>
        public Guid TargetId;

        /// <summary>
        /// The type of the behavior from the given behavior category.
        /// </summary>
        public BehaviorType BehaviorType;

        /// <summary>
        /// The name of the action being performed.
        /// </summary>
        public string ActionName;

        /// <summary>
        /// The state of the action being performed.
        /// </summary>
        public ActionState ActionState;
    }

    /// <summary>
    /// App => Engine, Engine => App
    /// Measures connection performance (health, latency).
    /// </summary>
    public class Heartbeat : Payload
    {
    }

    /// <summary>
    /// App => Engine, Engine => App
    /// </summary>
    public class HeartbeatReply : Payload
    {
    }
}
