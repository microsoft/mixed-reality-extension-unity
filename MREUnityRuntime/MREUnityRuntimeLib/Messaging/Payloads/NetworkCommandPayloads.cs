// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using MixedRealityExtension.Animation;
using MixedRealityExtension.Behaviors;
using MixedRealityExtension.Core;
using MixedRealityExtension.Patching.Types;
using System;
using System.Collections.Generic;
using MixedRealityExtension.Core.Types;
using Newtonsoft.Json.Linq;

namespace MixedRealityExtension.Messaging.Payloads
{
    /// <summary>
    /// App => Engine
    /// Payload that contains a remote procedure call to be made in the engine.
    /// </summary>
    public class AppToEngineRPC : NetworkCommandPayload
    {
        /// <summary>
        /// The name of the remote procedure call.
        /// </summary>
        public string ProcName { get; set; }

        /// <summary>
        /// The arguments to the remote procedure call.
        /// </summary>
        public JArray Args { get; set; } = new JArray();
    }

    /// <summary>
    /// App => Engine
    /// Payload for when the app wants to create an asset.
    /// </summary>
    public abstract class CreateActor : NetworkCommandPayload
    {
        /// <summary>
        /// The initial actor patch to apply to the newly created actor.
        /// </summary>
        public ActorPatch Actor { get; set; }
    }

    /// <summary>
    /// App => Engine
    /// Payload for when the app wants to create an asset from the host library.
    /// </summary>
    public class CreateFromLibrary : CreateActor
    {
        /// <summary>
        /// The resource url for the asset bundle.
        /// </summary>
        public string ResourceId { get; set; }
    }

    /// <summary>
    /// App => Engine
    /// Payload for when the app wants to create an asset from a GLTF.
    /// </summary>
    public class CreateFromGLTF : CreateActor
    {
        /// <summary>
        /// The resource url for the GLTF.
        /// </summary>
        public string ResourceUrl { get; set; }

        /// <summary>
        /// The assent name within the GLTF to instantiate.
        /// </summary>
        public string AssetName { get; set; }

        /// <summary>
        /// The type of collider to add to the actor upon creation.
        /// </summary>
        public ColliderType ColliderType { get; set; }
    }

    /// <summary>
    /// App => Engine
    /// Payload for when the app wants to create a primitive.
    /// </summary>
    public class CreatePrimitive : CreateActor
    {
        /// <summary>
        /// The primitive shape to create.
        /// </summary>
        public PrimitiveDefinition Definition { get; set; }

        /// <summary>
        /// Whether to add a collider to the primitive upon creation.
        /// </summary>
        public bool AddCollider { get; set; }
    }

    /// <summary>
    /// App => Engine
    /// Payload for when the app wants to create an empty actor.
    /// </summary>
    public class CreateEmpty : CreateActor
    {
    }

    /// <summary>
    /// App => Engine
    /// Payload for when the app wants to update an actor with a patch.
    /// </summary>
    public class ActorUpdate : NetworkCommandPayload
    {
        /// <summary>
        /// The actor patch to apply to the actor associated with the patch.
        /// </summary>
        public ActorPatch Actor { get; set; }
    }

    /// <summary>
    /// App => Engine
    /// Payload for when the app wants to update an actor with a patch, interpolated.
    /// </summary>
    public class ActorCorrection : NetworkCommandPayload
    {
        public Guid ActorId { get; set; }

        public MWTransform AppTransform { get; set; }
    }

    /// <summary>
    /// App => Engine
    /// Payload for when the app wants to destroy one or more actors.
    /// </summary>
    public class DestroyActors : NetworkCommandPayload
    {
        /// <summary>
        /// The enumeration of ids for the actors to be destroyed.
        /// </summary>
        public IEnumerable<Guid> ActorIds { get; set; }
    }

    /// <summary>
    /// Payload for when the app needs to restore the state of a set of actors.
    /// </summary>
    public class StateRestore : NetworkCommandPayload
    {
        /// <summary>
        /// The enumeration of actor patches to apply to their corresponding actors.
        /// </summary>
        public IEnumerable<ActorPatch> Actors { get; set; }
    }

    /// <summary>
    /// App => Engine
    /// Payload for when the app wants to execute one or more commands on the rigid body of an actor.
    /// </summary>
    public class RigidBodyCommands : NetworkCommandPayload
    {
        /// <summary>
        /// The id of the actor to execute rigid body commands on.
        /// </summary>
		public Guid ActorId { get; set; }

        /// <summary>
        /// The enumeration of command payloads to executed on the rigid body.
        /// </summary>
        public IEnumerable<Payload> CommandPayloads { get; set; }
    }

    /// <summary>
    /// App => Engine
    /// Payload for when the app wants to execute an add force command on an actor's rigid body.
    /// </summary>
    public class RBAddForce : NetworkCommandPayload
    {
        /// <summary>
        /// The force patch to apply to the rigid body.
        /// </summary>
        public Vector3Patch Force { get; set; }
    }

    /// <summary>
    /// App => Engine
    /// Payload for when the app wants to execute an add force at position command on an actor's rigid body.
    /// </summary>
    public class RBAddForceAtPosition : NetworkCommandPayload
    {
        /// <summary>
        /// The force patch to apply to the rigid body.
        /// </summary>
        public Vector3Patch Force { get; set; }

        /// <summary>
        /// The position at which to apply the force to the rigid body.
        /// </summary>
        public Vector3Patch Position { get; set; }
    }

    /// <summary>
    /// App => Engine
    /// Payload for when the app wants to execute an add torque command on an actor's rigid body.
    /// </summary>
    public class RBAddTorque : NetworkCommandPayload
    {
        /// <summary>
        /// The torque patch to add to the rigid body.
        /// </summary>
        public Vector3Patch Torque { get; set; }
    }

    /// <summary>
    /// App => Engine
    /// Payload for when the app wants to execute an add relative torque command on an actor's rigid body.
    /// </summary>
    public class RBAddRelativeTorque : NetworkCommandPayload
    {
        /// <summary>
        /// The relative torque patch to add to the rigid body.
        /// </summary>
        public Vector3Patch RelativeTorque { get; set; }
    }

    /// <summary>
    /// App => Engine
    /// Payload for when the app wants to execute a move position command on an actor's rigid body.
    /// </summary>
    public class RBMovePosition : NetworkCommandPayload
    {
        /// <summary>
        /// The position patch to move the rigid body to.
        /// </summary>
        public Vector3Patch Position { get; set; }
    }

    /// <summary>
    /// App => Engine
    /// Payload for when the app wants to execute a move rotation command on an actor's rigid body.
    /// </summary>
    public class RBMoveRotation : NetworkCommandPayload
    {
        /// <summary>
        /// The rotation patch to move the rigid body to.
        /// </summary>
        public QuaternionPatch Rotation { get; set; }
    }

    /// <summary>
    /// App => Engine
    /// Payload for when the app wants to create an animation on a specific actor.
    /// </summary>
    public class CreateAnimation : NetworkCommandPayload
    {
        /// <summary>
        /// The id of the actor to create the animation on.
        /// </summary>
        public Guid ActorId { get; set; }

        /// <summary>
        /// The name of the animation to create.
        /// </summary>
        public string AnimationName { get; set; }

        /// <summary>
        /// The enumeration of animation key frames to set to the animation. See <see cref="MWAnimationKeyframe"/>.
        /// </summary>
        public IEnumerable<MWAnimationKeyframe> Keyframes { get; set; }

        /// <summary>
        /// The enumeration of animation events to set to the animation. See <see cref="MWAnimationEvent"/>.
        /// </summary>
        public IEnumerable<MWAnimationEvent> Events { get; set; }

        /// <summary>
        /// The wrap mode of the animation. See <see cref="MWAnimationWrapMode"/>.
        /// </summary>
        public MWAnimationWrapMode WrapMode { get; set; }

        /// <summary>
        /// (Optional) The initial time, speed, and enable state of the animation (all values also optional). See <see cref="MWSetAnimationStateOptions"/>.
        /// </summary>
        public MWSetAnimationStateOptions InitialState { get; set; }
    }

    /// <summary>
    /// Bidirectional
    /// Payload to sync animation states between peers.
    /// </summary>
    public class SyncAnimations : NetworkCommandPayload
    {
        public IEnumerable<MWActorAnimationState> AnimationStates { get; set; }
    }

    /// <summary>
    /// App => Engine
    /// Payload for when the app wants to set animation state.
    /// </summary>
    public class SetAnimationState : NetworkCommandPayload
    {
        /// <summary>
        /// The id of the actor to reset the animation on.
        /// </summary>
        public Guid ActorId { get; set; }

        /// <summary>
        /// The name of the animation to reset.
        /// </summary>
        public string AnimationName { get; set; }

        /// <summary>
        /// The animation state to set. All fields are optional.
        /// </summary>
        public MWSetAnimationStateOptions State { get; set; }
    }

    /// <summary>
    /// App => Engine
    /// Payload for when the app wants to set animation state.
    /// </summary>
    public class SetMediaState : NetworkCommandPayload
    {
        /// <summary>
        /// The id of the sound instance - used to manipulate the sound after instantiation
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// The id of the actor to play the sound on.
        /// </summary>
        public Guid ActorId { get; set; }

        /// <summary>
        /// The GUID of the sound asset to start playing
        /// </summary>
        public Guid SoundAssetId { get; set; }

        /// <summary>
        /// Command type (start, update, or stop)
        /// </summary>
        public SoundCommand SoundCommand { get; set; }

        /// <summary>
        /// Time in seconds since sound was started
        /// </summary>
        public float? StartTimeOffset { get; set; }

        /// <summary>
        /// runtime configurable options.
        /// </summary>
        public MediaStateOptions Options { get; set; }

    }

    /// <summary>
    /// App => Engine
    /// Payload for when the app wants to interpolate actor properties (position, rotation, scale. Other fields in the future).
    /// </summary>
    public class InterpolateActor : NetworkCommandPayload
    {
        /// <summary>
        /// The id of the actor.
        /// </summary>
        public Guid ActorId { get; set; }
        /// <summary>
        /// The name given to the animation representing this interpolation.
        /// </summary>
        public string AnimationName { get; set; }
        /// <summary>
        /// The desired state to interpolate to.
        /// </summary>
        public ActorPatch Value { get; set; }
        /// <summary>
        /// The ease cubic-bezier curve parameters this interpolation will follow.
        /// </summary>
        public float[] Curve { get; set; }
        /// <summary>
        /// The duration of this interpolation (in seconds).
        /// </summary>
        public float Duration { get; set; }
        /// <summary>
        /// Whether or not to start the interpolation immediately.
        /// </summary>
        public bool Enabled { get; set; }
    }

    /// <summary>
    /// App => Engine
    /// Payload for when the app wants to set the behavior on an actor.
    /// </summary>
    public class SetBehavior : NetworkCommandPayload
    {
        /// <summary>
        /// The id of the actor to add the behaviors to.
        /// </summary>
        public Guid ActorId { get; set; }

        /// <summary>
        /// The type of behavior to set as the primary behavior. See <see cref="BehaviorType"/>.
        /// </summary>
        public BehaviorType BehaviorType { get; set; }
    }

    /// <summary>
    /// App => Engine
    /// Specific to multi-peer adapter: Sets whether this client is "authoritative". When authoritative, the client
    /// sends additional actor updates to the app (rigidbody updates, animation events, etc).
    /// </summary>
    public class SetAuthoritative : NetworkCommandPayload
    {
        /// <summary>
        /// Whether or not this client is authoritative.
        /// </summary>
        public bool Authoritative { get; set; }
    }

    /// <summary>
    /// Local-only. Not sent over network.
    /// Execute a command.
    /// </summary>
    public class LocalCommand : NetworkCommandPayload
    {
        /// <summary>
        /// The command to execute when this payload is processed.
        /// </summary>
        public Action Command;
    }

    /// <summary>
    /// The payload containing the user patch produced during user update from engine to app.
    /// </summary>
    public class UserUpdate : NetworkCommandPayload
    {
        /// <summary>
        /// The user patch generated during the user update.
        /// </summary>
        public UserPatch User;
    }
}
