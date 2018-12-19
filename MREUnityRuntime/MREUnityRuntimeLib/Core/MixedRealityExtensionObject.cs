// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using MixedRealityExtension.App;
using MixedRealityExtension.Core.Interfaces;
using MixedRealityExtension.Core.Types;
using MixedRealityExtension.Patching;
using MixedRealityExtension.Patching.Types;
using MixedRealityExtension.Util.Unity;
using System;
using UnityEngine;

namespace MixedRealityExtension.Core
{
    internal abstract class MixedRealityExtensionObject : MonoBehaviour, IMixedRealityExtensionObject
    {
        /// <inheritdoc />
        public Guid Id { get; private set; }

        /// <inheritdoc />
        public Guid AppInstanceId => App.InstanceId;

        /// <inheritdoc />
        public virtual string Name => gameObject.name;

        /// <inheritdoc />
        public MWTransform Transform { get; set; }

        /// <inheritdoc />
        public GameObject GameObject => this.gameObject;

        /// <inheritdoc />
        public abstract Vector3 LookAtPosition { get; }

        internal MixedRealityExtensionApp App { get; private set; }

        public IUser LocalUser => App.LocalUser;

        public void Initialize(Guid id, MixedRealityExtensionApp app)
        {
            Id = id;
            App = app;
            Transform = transform.ToMWTransform();
        }

        protected abstract void InternalUpdate();

        protected virtual void InternalFixedUpdate()
        {

        }

        protected virtual void OnStart()
        {

        }

        protected virtual void OnAwake()
        {
            
        }

        protected virtual void OnDestroyed()
        {

        }

        protected TransformPatch SynchronizeTransform(Transform engineTransform)
        {
            var transformPatch = GenerateTransformPatch(engineTransform);
            Transform = engineTransform.ToMWTransform();
            return transformPatch;
        }

        private TransformPatch GenerateTransformPatch(Transform engineTransform)
        {
            TransformPatch patch = PatchingUtilMethods.GeneratePatch(Transform, engineTransform);
            Transform.ApplyPatch(patch);
            return patch;
        }

        #region MonoBehaviour Methods

        private void Start()
        {
            OnStart();
        }

        private void Awake()
        {
            OnAwake();
        }

        private void Update()
        {
            InternalUpdate();
        }

        private void FixedUpdate()
        {
            InternalFixedUpdate();
        }

        private void OnDestroy()
        {
            OnDestroyed();
        }

        #endregion
    }
}
