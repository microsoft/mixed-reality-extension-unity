// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using MixedRealityExtension.App;
using MixedRealityExtension.Core.Interfaces;
using MixedRealityExtension.Messaging.Payloads;
using MixedRealityExtension.Patching.Types;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace MixedRealityExtension.Core
{
    internal class User : MixedRealityExtensionObject, IUser
    {
        private IList<MixedRealityExtensionApp> _joinedApps = new List<MixedRealityExtensionApp>();
        private IDictionary<Guid, SubscriptionType> _subscriptions = new Dictionary<Guid, SubscriptionType>();

        public override string Name => UserInfo.Name;

        public override Vector3 LookAtPosition => UserInfo.LookAtPosition;

        public IUserInfo UserInfo { get; private set; }

        internal void Initialize(IUserInfo userInfo, MixedRealityExtensionApp app)
        {
            UserInfo = userInfo;
            base.Initialize(UserInfo.Id, app);
        }

        internal void JoinApp(MixedRealityExtensionApp app)
        {
            _joinedApps.Add(app);
            _subscriptions[app.InstanceId] = SubscriptionType.None;
        }

        internal void LeaveApp(MixedRealityExtensionApp app)
        {
            _joinedApps.Remove(app);
            _subscriptions.Remove(app.InstanceId);
        }

        internal void SynchronizeApps()
        {
            var transformPatch = SynchronizeTransform(transform);

            foreach (var mreApp in _joinedApps)
            {
                var userPatch = new UserPatch(Id);

                SubscriptionType subscriptions;
                if (_subscriptions.TryGetValue(mreApp.InstanceId, out subscriptions) 
                    && subscriptions.HasFlag(SubscriptionType.Transform))
                {
                    userPatch.Transform = transformPatch;
                }

                mreApp.SynchronizeUser(userPatch);
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

        internal void AddSubscriptions(Guid appInstanceId, IEnumerable<SubscriptionType> adds)
        {
            SubscriptionType subs;
            if (adds != null && _subscriptions.TryGetValue(appInstanceId, out subs))
            {
                foreach (var subscription in adds)
                {
                    subs |= subscription;
                }
            }
        }

        internal void RemoveSubscriptions(Guid appInstanceId, IEnumerable<SubscriptionType> removes)
        {
            SubscriptionType subs;
            if (removes != null && _subscriptions.TryGetValue(appInstanceId, out subs))
            {
                foreach (var subscription in removes)
                {
                    subs &= ~subscription;
                }
            }
        }

        protected override void InternalUpdate()
        {
            SynchronizeApps();
        }
    }
}
