// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using MixedRealityExtension.Animation;
using MixedRealityExtension.Messaging.Events.Types;
using MixedRealityExtension.Messaging.Payloads;
using MixedRealityExtension.Patching;
using MixedRealityExtension.Patching.Types;
using MixedRealityExtension.Util;
using MixedRealityExtension.Util.Unity;

using UnityEngine;

using UnityAnimation = UnityEngine.Animation;

namespace MixedRealityExtension.Core.Components
{
    internal class AnimationComponent : ActorComponentBase
    {
        private UnityAnimation _animation;

        private class AnimationData
        {
            public bool Enabled;
            public bool IsInternal;
        }

        private Dictionary<string, AnimationData> _animationData = new Dictionary<string, AnimationData>();

        private bool GetAnimationData(string animationName, out AnimationData animationData) => _animationData.TryGetValue(animationName, out animationData);

        private void Update()
        {
            // Check for changes to an animation's enabled state and notify the server when a change is detected.
            var animation = GetUnityAnimationComponent();
            if (animation)
            {
                foreach (var item in animation)
                {
                    if (item is AnimationState)
                    {
                        var animationState = item as AnimationState;
                        if (GetAnimationData(animationState.name, out AnimationData animationData))
                        {
                            if (animationData.Enabled != animationState.enabled)
                            {
                                animationData.Enabled = animationState.enabled;

                                // Let the app know this animation (or interpolation) changed state.
                                NotifySetAnimationStateEvent(
                                    animationState.name,
                                    animationTime: null,
                                    animationSpeed: null,
                                    animationEnabled: animationData.Enabled);

                                // If the animation stopped, sync the actor's final transform.
                                if (!animationData.Enabled)
                                {
                                    AttachedActor.SynchronizeApp(ActorComponentType.Transform);
                                }

                                // If this was an internal one-shot animation (aka an interpolation), remove it.
                                if (!animationData.Enabled && animationData.IsInternal)
                                {
                                    _animationData.Remove(animationState.name);
                                    animation.RemoveClip(animationState.clip);
                                }
                            }
                        }
                    }
                }
            }
        }

        internal void CreateAnimation(
            string animationName,
            IEnumerable<MWAnimationKeyframe> keyframes,
            IEnumerable<MWAnimationEvent> events,
            MWAnimationWrapMode wrapMode,
            MWSetAnimationStateOptions initialState,
            bool isInternal,
            Action onCreatedCallback)
        {
            var continuation = new MWContinuation(AttachedActor, null, (result) =>
            {
                var clip = new AnimationClip
                {
                    legacy = true,
                    wrapMode = wrapMode.ToUnityWrapMode(),
                };

                var curves = new Dictionary<string, CurveInfo>();

                CurveInfo GetOrCreateCurve(Type type, string propertyName)
                {
                    if (!curves.TryGetValue(propertyName, out CurveInfo info))
                    {
                        info = new CurveInfo()
                        {
                            Curve = new AnimationCurve(),
                            Type = type
                        };

                        curves.Add(propertyName, info);
                    }

                    return info;
                }

                void AddFloatPatch(Type type, string propertyName, float time, float? value)
                {
                    if (value.HasValue)
                    {
                        var curveInfo = GetOrCreateCurve(type, propertyName);
                        var keyframe = new Keyframe(time, value.Value, 0, 0, 0, 0);
                        curveInfo.Curve.AddKey(keyframe);
                    }
                }

                void AddVector3Patch(Type type, string propertyName, float time, Vector3Patch value)
                {
                    AddFloatPatch(type, String.Format("{0}.x", propertyName), time, value?.X);
                    AddFloatPatch(type, String.Format("{0}.y", propertyName), time, value?.Y);
                    AddFloatPatch(type, String.Format("{0}.z", propertyName), time, value?.Z);
                }

                void AddQuaternionPatch(Type type, string propertyName, float time, QuaternionPatch value)
                {
                    AddFloatPatch(type, String.Format("{0}.x", propertyName), time, value?.X);
                    AddFloatPatch(type, String.Format("{0}.y", propertyName), time, value?.Y);
                    AddFloatPatch(type, String.Format("{0}.z", propertyName), time, value?.Z);
                    AddFloatPatch(type, String.Format("{0}.w", propertyName), time, value?.W);
                }

                void AddLocalTransformPatch(float time, ScaledTransformPatch value)
                {
                    // Work around a Unity bug/feature where all position components must be specified
                    // in the keyframe or the missing fields get set to zero.
                    Vector3Patch position = value?.Position;
                    if (position != null && position.IsPatched())
                    {
                        if (!position.X.HasValue) { position.X = transform.localPosition.x; }
                        if (!position.Y.HasValue) { position.Y = transform.localPosition.y; }
                        if (!position.Z.HasValue) { position.Z = transform.localPosition.z; }
                    }
                    // Work around a Unity bug/feature where all scale components must be specified
                    // in the keyframe or the missing fields get set to one.
                    Vector3Patch scale = value?.Scale;
                    if (scale != null && scale.IsPatched())
                    {
                        if (!scale.X.HasValue) { scale.X = transform.localScale.x; }
                        if (!scale.Y.HasValue) { scale.Y = transform.localScale.y; }
                        if (!scale.Z.HasValue) { scale.Z = transform.localScale.z; }
                    }
                    AddVector3Patch(typeof(Transform), "m_LocalPosition", time, value?.Position);
                    AddQuaternionPatch(typeof(Transform), "m_LocalRotation", time, value?.Rotation);
                    AddVector3Patch(typeof(Transform), "m_LocalScale", time, value?.Scale);
                }

                void AddAppTransformPatch(float time, TransformPatch value)
                {
                    // Work around a Unity bug/feature where all position components must be specified
                    // in the keyframe or the missing fields get set to zero.
                    Vector3Patch position = value?.Position;
                    if (position != null && position.IsPatched())
                    {
                        if (!position.X.HasValue) { position.X = transform.position.x; }
                        if (!position.Y.HasValue) { position.Y = transform.position.y; }
                        if (!position.Z.HasValue) { position.Z = transform.position.z; }
                    }

                    if (value != null)
                    {
                        var appRootTransform = AttachedActor.App.SceneRoot.transform;

                        if (position != null)
                        {
                            // We need the patch to be a unity vector 3.
                            Vector3 unityAppPosition;
                            unityAppPosition.x = position.X.Value;
                            unityAppPosition.y = position.Y.Value;
                            unityAppPosition.z = position.Z.Value;

                            // Convert the app space position to a world space position.
                            var worldPosition = appRootTransform.TransformPoint(unityAppPosition);

                            // Update the position patch to be world space for the animation.
                            position.X = worldPosition.x;
                            position.Y = worldPosition.y;
                            position.Z = worldPosition.z;

                            // Add the world space vector to the animation.
                            AddVector3Patch(typeof(Transform), "Position", time, position);
                        }

                        if (value.Rotation != null)
                        {
                            QuaternionPatch rotation = value.Rotation;

                            // We need the patch to be a unity quaternion.
                            Quaternion unityAppRotation;
                            unityAppRotation.w = rotation.W.Value;
                            unityAppRotation.x = rotation.X.Value;
                            unityAppRotation.y = rotation.Y.Value;
                            unityAppRotation.z = rotation.Z.Value;

                            // Convert the app space rotation to a world space rotation.
                            var worldRotation = appRootTransform.rotation * unityAppRotation;

                            // Update the rotation patch to be world space for the animation.
                            rotation.W = worldRotation.w;
                            rotation.X = worldRotation.x;
                            rotation.Y = worldRotation.y;
                            rotation.Z = worldRotation.z;

                            // Add the world space rotation to the animation.
                            AddQuaternionPatch(typeof(Transform), "Rotation", time, rotation);
                        }
                    }
                }

                void AddActorPatch(float time, ActorPatch value)
                {
                    if (value == null)
                    {
                        return;
                    }

                    if (value.Transform.App != null)
                    {
                        AddAppTransformPatch(time, value.Transform.App);
                    }

                    if (value.Transform.Local != null)
                    {
                        AddLocalTransformPatch(time, value?.Transform.Local);
                    }
                }

                void AddKeyframe(MWAnimationKeyframe keyframe)
                {
                    AddActorPatch(keyframe.Time, keyframe.Value);
                }

                foreach (var keyframe in keyframes)
                {
                    AddKeyframe(keyframe);
                }

                foreach (var kv in curves)
                {
                    clip.SetCurve("", kv.Value.Type, kv.Key, kv.Value.Curve);
                }

                _animationData[animationName] = new AnimationData()
                {
                    IsInternal = isInternal
                };

                float initialTime = 0f;
                float initialSpeed = 1f;
                bool initialEnabled = false;

                if (initialState != null)
                {
                    initialTime = initialState.Time ?? initialTime;
                    initialSpeed = initialState.Speed ?? initialSpeed;
                    initialEnabled = initialState.Enabled ?? initialEnabled;
                }

                var animation = GetOrCreateUnityAnimationComponent();
                animation.AddClip(clip, animationName);

                SetAnimationState(animationName, initialTime, initialSpeed, initialEnabled);

                onCreatedCallback?.Invoke();
            });

            continuation.Start();
        }

        internal void Interpolate(
            ActorPatch finalFrame,
            string animationName,
            float duration,
            float[] curve,
            bool enabled)
        {
            // Ensure duration is in range [0...n].
            duration = Math.Max(0, duration);

            const int FPS = 10;
            float timeStep = duration / FPS;

            // If the curve is malformed, fall back to linear.
            if (curve.Length != 4)
            {
                curve = new float[] { 0, 0, 1, 1 };
            }

            // Are we patching the transform?
            bool animateTransform = finalFrame.Transform != null;
            bool animateLocalTransform = animateTransform && finalFrame.Transform.Local != null && finalFrame.Transform.Local.IsPatched();
            bool animateAppTransform = animateTransform && finalFrame.Transform.App != null && finalFrame.Transform.App.IsPatched();
            animateTransform = finalFrame.Transform != null && (animateAppTransform || animateLocalTransform);

            // Start with local, and override with app values
            TransformPatch finalTransform = finalFrame.Transform.Local;
            bool animateLocalPosition = animateLocalTransform && finalTransform.Position != null && finalTransform.Position.IsPatched();
            bool animateLocalRotation = animateLocalTransform && finalTransform.Rotation != null && finalTransform.Rotation.IsPatched();
            bool animateLocalScale = animateLocalTransform &&
                (finalTransform as ScaledTransformPatch).Scale != null &&
                (finalTransform as ScaledTransformPatch).Scale.IsPatched();

            // Override with app transform.
            bool animateAppPosition = false;
            bool animateAppRotation = false;
            if (animateAppTransform)
            {
                // Ensure we have a transform.  If there was no local supplied, set it to the app transform.
                finalTransform = finalTransform ?? finalFrame.Transform.App;

                // Override the individual elements of the transform if there are present in the app transform.
                if (finalFrame.Transform.App.Position != null)
                {
                    finalTransform.Position = finalFrame.Transform.App.Position;
                    animateAppPosition = finalTransform.Position != null && finalTransform.Position.IsPatched();
                }

                if (finalFrame.Transform.App.Rotation != null)
                {
                    finalTransform.Rotation = finalFrame.Transform.App.Rotation;
                    animateAppRotation = finalTransform.Rotation != null && finalTransform.Rotation.IsPatched();
                }
            }

            bool animatePosition = animateAppPosition || animateLocalPosition;
            bool animateRotation = animateAppRotation || animateLocalRotation;

            // Ensure we have a well-formed rotation quaternion.
            for (; animateRotation;)
            {
                var rotation = finalTransform.Rotation;
                bool hasAllComponents =
                    rotation.X.HasValue &&
                    rotation.Y.HasValue &&
                    rotation.Z.HasValue &&
                    rotation.W.HasValue;

                // If quaternion is incomplete, fall back to the identity.
                if (!hasAllComponents)
                {
                    finalTransform.Rotation = new QuaternionPatch(Quaternion.identity);
                    break;
                }

                // Ensure the quaternion is normalized.
                var lengthSquared =
                    (rotation.X.Value * rotation.X.Value) +
                    (rotation.Y.Value * rotation.Y.Value) +
                    (rotation.Z.Value * rotation.Z.Value) +
                    (rotation.W.Value * rotation.W.Value);
                if (lengthSquared == 0)
                {
                    // If the quaternion is length zero, fall back to the identity.
                    finalTransform.Rotation = new QuaternionPatch(Quaternion.identity);
                    break;
                }
                else if (lengthSquared != 1.0f)
                {
                    // If the quaternion length is not 1, normalize it.
                    var inverseLength = 1.0f / Mathf.Sqrt(lengthSquared);
                    rotation.X *= inverseLength;
                    rotation.Y *= inverseLength;
                    rotation.Z *= inverseLength;
                    rotation.W *= inverseLength;
                }
                break;
            }

            // Create the sampler to calculate ease curve values.
            var sampler = new CubicBezier(curve[0], curve[1], curve[2], curve[3]);

            var keyframes = new List<MWAnimationKeyframe>();

            // Generate keyframes
            float currTime = 0;

            do
            {
                var keyframe = NewKeyframe(currTime);
                var unitTime = duration > 0 ? currTime / duration : 1;
                BuildKeyframe(keyframe, unitTime);
                keyframes.Add(keyframe);
                currTime += timeStep;
            }
            while (currTime <= duration && timeStep > 0);

            // Final frame (if needed)
            if (currTime - duration > 0)
            {
                var keyframe = NewKeyframe(duration);
                BuildKeyframe(keyframe, 1);
                keyframes.Add(keyframe);
            }

            // Create and optionally start the animation.
            CreateAnimation(
                animationName,
                keyframes,
                events: null,
                wrapMode: MWAnimationWrapMode.Once,
                initialState: new MWSetAnimationStateOptions { Enabled = enabled },
                isInternal: true,
                onCreatedCallback: null);

            bool LerpFloat(out float dest, float start, float? end, float t)
            {
                if (end.HasValue)
                {
                    dest = Mathf.LerpUnclamped(start, end.Value, t);
                    return true;
                }
                dest = 0;
                return false;
            }

            bool SlerpQuaternion(out Quaternion dest, Quaternion start, QuaternionPatch end, float t)
            {
                if (end != null)
                {
                    dest = Quaternion.SlerpUnclamped(start, new Quaternion(end.X.Value, end.Y.Value, end.Z.Value, end.W.Value), t);
                    return true;
                }
                dest = Quaternion.identity;
                return false;
            }

            void BuildKeyframePosition(MWAnimationKeyframe keyframe, float t)
            {
                float value;
                if (animateAppPosition)
                {
                    Vector3 appPosition = AttachedActor.App.SceneRoot.transform.InverseTransformPoint(transform.localPosition);
                    if (LerpFloat(out value, appPosition.x, finalTransform.Position.X, t))
                    {
                        keyframe.Value.Transform.App.Position.X = value;
                    }
                    if (LerpFloat(out value, appPosition.y, finalTransform.Position.Y, t))
                    {
                        keyframe.Value.Transform.App.Position.Y = value;
                    }
                    if (LerpFloat(out value, appPosition.z, finalTransform.Position.Z, t))
                    {
                        keyframe.Value.Transform.App.Position.Z = value;
                    }
                }
                else
                {
                    if (LerpFloat(out value, transform.localPosition.x, finalTransform.Position.X, t))
                    {
                        keyframe.Value.Transform.Local.Position.X = value;
                    }
                    if (LerpFloat(out value, transform.localPosition.y, finalTransform.Position.Y, t))
                    {
                        keyframe.Value.Transform.Local.Position.Y = value;
                    }
                    if (LerpFloat(out value, transform.localPosition.z, finalTransform.Position.Z, t))
                    {
                        keyframe.Value.Transform.Local.Position.Z = value;
                    }
                }
            }

            void BuildKeyframeScale(MWAnimationKeyframe keyframe, float t)
            {
                if (!(animateLocalTransform && animateLocalScale))
                {
                    return;
                }

                float value;
                ScaledTransformPatch localTransform = finalTransform as ScaledTransformPatch;
                if (LerpFloat(out value, transform.localScale.x, localTransform.Scale.X, t))
                {
                    keyframe.Value.Transform.Local.Scale.X = value;
                }
                if (LerpFloat(out value, transform.localScale.y, localTransform.Scale.Y, t))
                {
                    keyframe.Value.Transform.Local.Scale.Y = value;
                }
                if (LerpFloat(out value, transform.localScale.z, localTransform.Scale.Z, t))
                {
                    keyframe.Value.Transform.Local.Scale.Z = value;
                }
            }

            void BuildKeyframeRotation(MWAnimationKeyframe keyframe, float t)
            {
                Quaternion value;
                if (animateAppRotation)
                {
                    var appRotation = transform.rotation * AttachedActor.App.SceneRoot.transform.rotation;
                    if (SlerpQuaternion(out value, appRotation, finalTransform.Rotation, t))
                    {
                        keyframe.Value.Transform.App.Rotation.W = value.w;
                        keyframe.Value.Transform.App.Rotation.X = value.x;
                        keyframe.Value.Transform.App.Rotation.Y = value.y;
                        keyframe.Value.Transform.App.Rotation.Z = value.z;
                    }
                }
                else
                {
                    if (SlerpQuaternion(out value, transform.localRotation, finalTransform.Rotation, t))
                    {
                        keyframe.Value.Transform.Local.Rotation.W = value.w;
                        keyframe.Value.Transform.Local.Rotation.X = value.x;
                        keyframe.Value.Transform.Local.Rotation.Y = value.y;
                        keyframe.Value.Transform.Local.Rotation.Z = value.z;
                    }
                }
            }

            void BuildKeyframe(MWAnimationKeyframe keyframe, float unitTime)
            {
                float curveTime = sampler.Sample(unitTime);

                if (animatePosition)
                {
                    BuildKeyframePosition(keyframe, curveTime);
                }
                if (animateRotation)
                {
                    BuildKeyframeRotation(keyframe, curveTime);
                }
                if (animateLocalScale)
                {
                    BuildKeyframeScale(keyframe, curveTime);
                }
            }

            MWAnimationKeyframe NewKeyframe(float time)
            {
                var keyframe = new MWAnimationKeyframe
                {
                    Time = time,
                    Value = new ActorPatch()
                };

                if (animateTransform)
                {
                    if (animateLocalTransform)
                    {
                        keyframe.Value.Transform = new ActorTransformPatch()
                        {
                            Local = new ScaledTransformPatch()
                        };
                    }

                    if (animateAppTransform)
                    {
                        keyframe.Value.Transform = keyframe.Value.Transform ?? new ActorTransformPatch();
                        keyframe.Value.Transform.App = new TransformPatch();
                    }
                }

                if (animateLocalTransform)
                {
                    if (animateLocalPosition && !animateAppPosition)
                    {
                        keyframe.Value.Transform.Local.Position = new Vector3Patch();
                    }
                    if (animateLocalRotation && !animateAppRotation)
                    {
                        keyframe.Value.Transform.Local.Rotation = new QuaternionPatch();
                    }
                    if (animateLocalScale)
                    {
                        keyframe.Value.Transform.Local.Scale = new Vector3Patch();
                    }
                }

                if (animateAppTransform)
                {
                    if (animateAppPosition)
                    {
                        keyframe.Value.Transform.App.Position = new Vector3Patch();
                    }
                    if (animateAppRotation)
                    {
                        keyframe.Value.Transform.App.Rotation = new QuaternionPatch();
                    }
                }

                return keyframe;
            }
        }

        internal void SetAnimationState(string animationName, float? time, float? speed, bool? enabled)
        {
            var animation = GetOrCreateUnityAnimationComponent();
            if (animation)
            {
                if (animation[animationName] != null)
                {
                    // Create the animationData if it doesn't already exist. This is the case for gltf animations.
                    if (!GetAnimationData(animationName, out AnimationData animationData))
                    {
                        _animationData[animationName] = animationData = new AnimationData();
                    }

                    if (speed.HasValue)
                    {
                        animation[animationName].speed = speed.Value;
                    }
                    if (time.HasValue)
                    {
                        SetAnimationTime(animation[animationName], time.Value);
                    }
                    if (enabled.HasValue)
                    {
                        EnableAnimation(animationName, enabled.Value);
                    }
                }
            }
        }

        private void EnableAnimation(string animationName, bool? enabled)
        {
            if (enabled.HasValue)
            {
                var animation = GetUnityAnimationComponent();
                if (animation)
                {
                    if (animation[animationName] != null)
                    {
                        var animState = animation[animationName];
                        var wasEnabled = animState.enabled;
                        if (wasEnabled != enabled.Value)
                        {
                            animation[animationName].enabled = enabled.Value;
                            // NOTE: animationData.Enabled will be set in the next call to Update()

                            // When stopping an animation, send an update to the app letting it know the final animation state.
                            if (!enabled.Value && (AttachedActor.App.IsAuthoritativePeer || AttachedActor.App.OperatingModel == OperatingModel.ServerAuthoritative))
                            {
                                // FUTURE: Add additional animatable properties as support for them is added (light color, etc).
                                AttachedActor.SendActorUpdate(ActorComponentType.Transform);
                            }
                        }
                        animation[animationName].weight = enabled.Value ? 1.0f : 0.0f;
                    }
                }
            }
        }

        internal IList<MWActorAnimationState> GetAnimationStates()
        {
            var animation = GetUnityAnimationComponent();
            if (animation)
            {
                var animationStates = new List<MWActorAnimationState>();
                foreach (var item in animation)
                {
                    if (item is AnimationState)
                    {
                        var animationState = item as AnimationState;

                        animationStates.Add(GetAnimationState(animationState));
                    }
                }

                return animationStates;
            }

            return null;
        }

        MWActorAnimationState GetAnimationState(AnimationState animationState)
        {
            return new MWActorAnimationState()
            {
                ActorId = this.AttachedActor.Id,
                AnimationName = animationState.name,
                State = new MWSetAnimationStateOptions
                {
                    Time = animationState.time,
                    Speed = animationState.speed,
                    Enabled = animationState.enabled
                }
            };
        }

        internal void ApplyAnimationState(MWActorAnimationState animationState)
        {
            SetAnimationState(animationState.AnimationName, animationState.State.Time, animationState.State.Speed, animationState.State.Enabled);
        }

        private void NotifySetAnimationStateEvent(string animationName, float? animationTime, float? animationSpeed, bool? animationEnabled)
        {
            AttachedActor.App.EventManager.QueueEvent(new SetAnimationStateEvent(AttachedActor.Id, animationName, animationTime, animationSpeed, animationEnabled));
        }

        private void SetAnimationTime(AnimationState animState, float animationTime)
        {
            if (animationTime < 0)
            {
                animationTime = animState.length;
            }

            animState.time = animationTime;
        }

        private UnityAnimation GetOrCreateUnityAnimationComponent()
        {
            if (_animation == null)
            {
                _animation = gameObject.GetComponent<UnityAnimation>();
                if (_animation == null)
                {
                    _animation = gameObject.AddComponent<UnityAnimation>();
                }
            }
            return _animation;
        }

        private UnityAnimation GetOrLookUpUnityAnimationComponent()
        {
            if (_animation == null)
            {
                _animation = gameObject.GetComponent<UnityAnimation>();
            }
            return _animation;
        }

        private UnityAnimation GetUnityAnimationComponent()
        {
            return _animation;
        }

        private class CurveInfo
        {
            public Type Type;
            public AnimationCurve Curve;
        }
    }
}
