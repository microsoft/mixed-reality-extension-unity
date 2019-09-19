using System;
using System.Collections.Generic;
using MixedRealityExtension.App;

namespace MixedRealityExtension.Core
{
	internal class UserManager
	{
		private MixedRealityExtensionApp _app;
		private Dictionary<Guid, User> _userMapping = new Dictionary<Guid, User>();

		internal Dictionary<Guid, User>.ValueCollection Users => _userMapping.Values;

		internal UserManager(MixedRealityExtensionApp app)
		{
			_app = app;
		}

		internal void AddUser(User user)
		{
			_userMapping[user.Id] = user;
			user.JoinApp(_app);
		}

		internal void RemoveUser(User user)
		{
			user.LeaveApp(_app);
			_userMapping.Remove(user.Id);
		}

		internal User FindUser(Guid userId)
		{
			_userMapping.TryGetValue(userId, out User value);
			return value;
		}

		internal bool HasUser(Guid userId)
		{
			return _userMapping.ContainsKey(userId);
		}
	}
}
