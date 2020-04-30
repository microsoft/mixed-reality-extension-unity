using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace MixedRealityExtension.Messaging.Commands
{
	internal class CommandHandlerContext<T> : ICommandHandlerContext
	{
		static Dictionary<Type, MethodInfo> _methodInfo;
		static CommandHandlerContext()
		{
			_methodInfo = new Type[] { typeof(T) }
				.SelectMany(t => t.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance))
				.Where(m => m.GetCustomAttributes().OfType<CommandHandler>().Any())
				.ToDictionary(m => m.GetCustomAttributes().OfType<CommandHandler>().First().CommandType);
		}

		public static Dictionary<Type, MethodInfo> GetMethodInfoDictionary()
		{
			return _methodInfo;
		}
	}
}
