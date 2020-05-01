using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace MixedRealityExtension.Messaging.Commands
{
	/// <summary>
	/// This class pre-caches method info for command handlers, since it does not change at runtime.
	/// This saved us 20ms on the startup frame for the MREApp on a high end CPU.
	/// </summary>
	/// <typeparam name="T"></typeparam>
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
