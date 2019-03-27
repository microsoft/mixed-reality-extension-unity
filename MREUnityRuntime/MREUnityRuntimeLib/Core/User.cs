// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using MixedRealityExtension.App;
using MixedRealityExtension.Core.Interfaces;
using MixedRealityExtension.Messaging.Commands;
using MixedRealityExtension.Messaging.Payloads;
using MixedRealityExtension.Patching;
using MixedRealityExtension.Patching.Types;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace MixedRealityExtension.Core
{
    internal class User : MixedRealityExtensionObject, IUser
    {
        private IList<MixedRealityExtensionApp> _joinedApps = new List<MixedRealityExtensionApp>();

        public override string Name => UserInfo.Name;

        public IUserInfo UserInfo { get; private set; }

        public UInt32 Groups { get; internal set; } = 1;

        internal void Initialize(IUserInfo userInfo, MixedRealityExtensionApp app)
        {
            UserInfo = userInfo;
            base.Initialize(UserInfo.Id, app);
        }

        internal void JoinApp(MixedRealityExtensionApp app)
        {
            _joinedApps.Add(app);
        }

        internal void LeaveApp(MixedRealityExtensionApp app)
        {
            _joinedApps.Remove(app);
        }

        internal void SynchronizeApps()
        {
            var transformPatch = SynchronizeTransform(transform);

            foreach (var mreApp in _joinedApps)
            {
                var userPatch = new UserPatch(Id);

                // TODO: Write user changes to the patch.

                if (userPatch.IsPatched())
                {
                    mreApp.SynchronizeUser(userPatch);
                }
            }
        }

        internal void SynchronizeEngine(UserPatch patch)
        {
            // no need to queue like with actors, just slap it right on there
            if (patch.Groups.HasValue)
            {
                Groups = patch.Groups.Value;
            }
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as IUser);
        }

        public bool Equals(IUser other)
        {
            return other != null && Id == other.Id;
        }

        protected override void InternalUpdate()
        {
            SynchronizeApps();
        }
    }
}
