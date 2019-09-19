using MixedRealityExtension.App;
using MixedRealityExtension.Core.Interfaces;
using MixedRealityExtension.PluginInterfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class UserInfoProvider : IUserInfoProvider
{
	public IUserInfo GetUserInfo(IMixedRealityExtensionApp app, Guid userId)
	{
		return MREComponent.GetUserInfo(userId);
	}
}
