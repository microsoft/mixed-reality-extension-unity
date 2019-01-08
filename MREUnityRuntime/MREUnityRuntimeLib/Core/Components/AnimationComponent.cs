// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;

using MixedRealityExtension.Animation;
using MixedRealityExtension.Messaging.Events.Types;
using MixedRealityExtension.Messaging.Payloads;
using MixedRealityExtension.Patching.Types;
using MixedRealityExtension.Util.Unity;

using UnityEngine;

using UnityAnimation = UnityEngine.Animation;

namespace MixedRealityExtension.Core.Components
{
    internal class AnimationComponent : ActorComponentBase
    {
        private UnityAnimation _animation;
        private Dictionary<string, bool> _hasRootMotion = new Dictionary<string, bool>();

        internal bool Animating
        {
            get
            {
                var animation = GetUnityAnimationComponent();
                if (animation != null)
                {
                    if (animation.isPlaying)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        internal void CreateAnimation(
            string animationName,
            IEnumerable<MWAnimationKeyframe> keyframes,
            IEnumerable<MWAnimationEvent> events,
            MWAnimationWrapMode wrapMode,
            Action callback)
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

                void AddTransformPatch(float time, TransformPatch value)
                {
                    AddVector3Patch(typeof(Transform), "m_LocalPosition", time, value?.Position);
                    AddQuaternionPatch(typeof(Transform), "m_LocalRotation", time, value?.Rotation);
                    AddVector3Patch(typeof(Transform), "m_LocalScale", time, value?.Scale);
                }

                void AddActorPatch(float time, ActorPatch value)
                {
                    AddTransformPatch(time, value?.Transform);
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

                clip.AddEvent(new AnimationEvent()
                {
                    functionName = "AnimationEndedEvent",
                    time = clip.length
                });

                var animation = GetOrCreateUnityAnimationComponent();
                animation.AddClip(clip, animationName);
                animation[animationName].speed = 0f;
                animation[animationName].weight = 0f;
                // Animations are always enabled. Playing vs. not playing is controlled by the speed and weight properties.
                animation[animationName].enabled = true;

                callback?.Invoke();
            });

            continuation.Start();
        }

        internal void StartAnimation(string animationName, float? animationTime, bool? paused, bool? hasRootMotion)
        {
            var animation = GetOrLookUpUnityAnimationComponent();
            if (animation != null)
            {
                if (animation[animationName] != null)
                {
                    _hasRootMotion[animationName] = hasRootMotion.HasValue && hasRootMotion.Value;
                    int scalar = paused.HasValue ? paused.Value ? 0 : 1 : 1;
                    float time = animationTime ?? 0;
                    animation[animationName].speed = 1f * scalar;
                    animation[animationName].weight = 1f * scalar;
                    animation[animationName].time = time;
                }
            }
        }

        internal void StopAnimation(string animationName, float? animationTime)
        {
            var animation = GetUnityAnimationComponent();
            if (animation)
            {
                if (animation[animationName] != null)
                {
                    if (_hasRootMotion.TryGetValue(animationName, out bool dictionaryValue) && dictionaryValue)
                    {
                        ApplyRootMotion(animation[animationName].clip, animation[animationName].time, animationTime ?? 0f);
                    }

                    if (animationTime.HasValue)
                    {
                        animation[animationName].time = animationTime.Value;
                    }
                    animation[animationName].speed = 0f;

                    AnimationStopped(animationName, animation[animationName].time);
                }
            }
        }

        internal void ResetAnimation(string animationName)
        {
            var animation = GetUnityAnimationComponent();
            if (animation)
            {
                if (animation[animationName] != null)
                {
                    if (animation[animationName].speed != 0)
                    {
                        if (_hasRootMotion.TryGetValue(animationName, out bool dictionaryValue) && dictionaryValue)
                        {
                            ApplyRootMotion(animation[animationName].clip, animation[animationName].time, 0f);
                        }
                    }
                    animation[animationName].time = 0;
                }
            }
        }

        internal void PauseAnimation(string animationName)
        {
            var animation = GetUnityAnimationComponent();
            if (animation != null)
            {
                if (animation[animationName] != null)
                {
                    animation[animationName].speed = 0f;
                    animation[animationName].weight = 0f;
                    AnimationStopped(animationName, animation[animationName].time);
                }
            }
        }

        internal void ResumeAnimation(string animationName)
        {
            var animation = GetUnityAnimationComponent();
            if (animation != null)
            {
                if (animation[animationName] != null)
                {
                    animation[animationName].speed = 1f;
                    animation[animationName].weight = 1f;
                }
            }
        }

        internal IList<MWAnimationState> GetAnimationStates()
        {
            var animation = GetUnityAnimationComponent();
            if (animation)
            {
                var animationStates = new List<MWAnimationState>();
                foreach (var item in animation)
                {
                    if (item is AnimationState)
                    {
                        var animationState = item as AnimationState;
                        _hasRootMotion.TryGetValue(animationState.clip.name, out bool hasRootMotion );

                        animationStates.Add(new MWAnimationState()
                        {
                            ActorId = this.AttachedActor.Id,
                            AnimationName = animationState.name,
                            AnimationTime = animationState.time,
                            Paused = animationState.speed == 0,
                            HasRootMotion = hasRootMotion
                        });
                    }
                }

                return animationStates;
            }

            return null;
        }

        internal void ApplyAnimationState(MWAnimationState animationState)
        {
            this.StartAnimation(animationState.AnimationName, animationState.AnimationTime, animationState.Paused, animationState.HasRootMotion);
        }

        internal void AnimationStopped(string animationName, float animationTime)
        {
            if (AttachedActor.App.OperatingModel == OperatingModel.PeerAuthoritative && AttachedActor.App.IsAuthoritativePeer)
            {
                AttachedActor.App.EventManager.QueueEvent(new AnimationStoppedEvent(AttachedActor.Id, animationName, animationTime));
            }
        }

        internal void ApplyRootMotion(AnimationClip clip, float beforeFrame, float afterFrame)
        {
            GameObject tempGameObject = new GameObject();

            clip.SampleAnimation(tempGameObject, beforeFrame);
            Vector3 positionAtBeforeFrame = tempGameObject.transform.localPosition;
            Quaternion orientationAtBeforeFrame = tempGameObject.transform.localRotation;
            float scaleAtBeforeFrame = tempGameObject.transform.localScale.y;
            scaleAtBeforeFrame = scaleAtBeforeFrame > 0.0f ? scaleAtBeforeFrame : 1.0f;

            clip.SampleAnimation(tempGameObject, afterFrame);
            Vector3 positionAtAfterFrame = tempGameObject.transform.localPosition;
            Quaternion orientationAtAfterFrame = tempGameObject.transform.localRotation;
            float scaleAtAfterFrame = tempGameObject.transform.localScale.y;
            scaleAtAfterFrame = scaleAtAfterFrame > 0.0f ? scaleAtAfterFrame : 1.0f;

            Destroy(tempGameObject);

            //Transform local space changes into root node's space 
            Vector3 tempPosition = transform.position;
            Quaternion tempRotation = transform.rotation;

            Quaternion orientationChange = orientationAtBeforeFrame * Quaternion.Inverse(orientationAtAfterFrame);
            float scaleChange = scaleAtBeforeFrame / scaleAtAfterFrame;

            transform.parent.position = transform.parent.TransformPoint(positionAtBeforeFrame - positionAtAfterFrame);
            transform.parent.rotation = orientationChange * transform.parent.rotation;
            transform.parent.localScale *= scaleChange;

            transform.position = tempPosition;
            transform.rotation = tempRotation;
            transform.localScale /= scaleChange;
        }
        internal void AnimationEndedEvent(AnimationEvent animationEvent)
        {
            if (_hasRootMotion.TryGetValue(animationEvent.animationState.name, out bool dictionaryValue) && dictionaryValue)
            {
                ApplyRootMotion(animationEvent.animationState.clip, animationEvent.animationState.clip.length, 0f);
            }

            if (!animationEvent.animationState.wrapMode.FromUnityWrapMode().IsLooping())
            {
                AnimationStopped(animationEvent.animationState.name, animationEvent.animationState.time);
                animationEvent.animationState.speed = 0f;
            }
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
