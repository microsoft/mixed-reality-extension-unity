// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using Newtonsoft.Json.Linq;
using System;

namespace MixedRealityExtension.RPC
{
	/// <summary>
	/// Base class for all RPC handlers.
	/// </summary>
	public abstract class RPCHandlerBase
	{
		internal abstract void Execute(JToken[] args);
	}


	/// <summary>
	/// Class that serves as an RPC handler for a procedure that takes no arguments.
	/// </summary>
	public class RPCHandler : RPCHandlerBase
	{
		private readonly Action _action;

		/// <summary>
		/// Initializes an instance of the class <see cref="RPCHandler"/>
		/// </summary>
		/// <param name="action">The action to perform when the RPC is being executed.</param>
		public RPCHandler(Action action)
		{
			_action = action;
		}

		/// <inheritdoc />
		internal override void Execute(JToken[] args)
		{
			if (args.Length != 0)
			{
				throw new ArgumentException("Not the correct number of args.  This RPC handler expects no args.");
			}

			_action?.Invoke();
		}
	}

	/// <summary>
	/// Class that serves as an RPC handler for a procedure that takes one arguments.
	/// </summary>
	/// <typeparam name="ArgT1">The type of the first argument.</typeparam>
	public class RPCHandler<ArgT1> : RPCHandlerBase
	{
		private readonly Action<ArgT1> _action;

		/// <summary>
		/// Initializes an instance of the class <see cref="RPCHandler{ArgT1}"/>
		/// </summary>
		/// <param name="action">The action to perform when the RPC is being executed.</param>
		public RPCHandler(Action<ArgT1> action)
		{
			_action = action;
		}

		internal override void Execute(JToken[] args)
		{
			if (args.Length != 1)
			{
				throw new ArgumentException("Not the correct number of args.  This RPC handler expects 1 arg.");
			}

			_action?.Invoke(args[0].ToObject<ArgT1>());
		}
	}

	/// <summary>
	/// Class that serves as an RPC handler for a procedure that takes two arguments.
	/// </summary>
	/// <typeparam name="ArgT1">The type of the first argument.</typeparam>
	/// <typeparam name="ArgT2">The type of the second argument.</typeparam>
	public class RPCHandler<ArgT1, ArgT2> : RPCHandlerBase
	{
		private readonly Action<ArgT1, ArgT2> _action;

		/// <summary>
		/// Initializes an instance of the class <see cref="RPCHandler{ArgT1, ArgT2}"/>
		/// </summary>
		/// <param name="action">The action to perform when the RPC is being executed.</param>
		public RPCHandler(Action<ArgT1, ArgT2> action)
		{
			_action = action;
		}

		internal override void Execute(JToken[] args)
		{
			if (args.Length != 2)
			{
				throw new ArgumentException("Not the correct number of args.  This RPC handler expects two args.");
			}

			_action?.Invoke(
				args[0].ToObject<ArgT1>(),
				args[1].ToObject<ArgT2>()
			);
		}
	}

	/// <summary>
	/// Class that serves as an RPC handler for a procedure that takes three arguments.
	/// </summary>
	/// <typeparam name="ArgT1">The type of the first argument.</typeparam>
	/// <typeparam name="ArgT2">The type of the second argument.</typeparam>
	/// <typeparam name="ArgT3">The type of the third argument.</typeparam>
	public class RPCHandler<ArgT1, ArgT2, ArgT3> : RPCHandlerBase
	{
		private readonly Action<ArgT1, ArgT2, ArgT3> _action;

		/// <summary>
		/// Initializes an instance of the class <see cref="RPCHandler{ArgT1, ArgT2, ArgT3}"/>
		/// </summary>
		/// <param name="action">The action to perform when the RPC is being executed.</param>
		public RPCHandler(Action<ArgT1, ArgT2, ArgT3> action)
		{
			_action = action;
		}

		internal override void Execute(JToken[] args)
		{
			if (args.Length != 3)
			{
				throw new ArgumentException("Not the correct number of args.  This RPC handler expects three args.");
			}

			_action?.Invoke(
				args[0].ToObject<ArgT1>(),
				args[1].ToObject<ArgT2>(),
				args[2].ToObject<ArgT3>()
			);
		}
	}

	/// <summary>
	/// Class that serves as an RPC handler for a procedure that takes four arguments.
	/// </summary>
	/// <typeparam name="ArgT1">The type of the first argument.</typeparam>
	/// <typeparam name="ArgT2">The type of the second argument.</typeparam>
	/// <typeparam name="ArgT3">The type of the third argument.</typeparam>
	/// <typeparam name="ArgT4">The type of the fourth argument.</typeparam>
	public class RPCHandler<ArgT1, ArgT2, ArgT3, ArgT4> : RPCHandlerBase
	{
		private readonly Action<ArgT1, ArgT2, ArgT3, ArgT4> _action;

		/// <summary>
		/// Initializes an instance of the class <see cref="RPCHandler{ArgT1, ArgT2, ArgT3, ArgT4}"/>
		/// </summary>
		/// <param name="action">The action to perform when the RPC is being executed.</param>
		public RPCHandler(Action<ArgT1, ArgT2, ArgT3, ArgT4> action)
		{
			_action = action;
		}

		internal override void Execute(JToken[] args)
		{
			if (args.Length != 4)
			{
				throw new ArgumentException("Not the correct number of args.  This RPC handler expects four args.");
			}

			_action?.Invoke(
				args[0].ToObject<ArgT1>(),
				args[1].ToObject<ArgT2>(),
				args[2].ToObject<ArgT3>(),
				args[3].ToObject<ArgT4>()
			);
		}
	}

	/// <summary>
	/// Class that serves as an RPC handler for a procedure that takes five arguments.
	/// </summary>
	/// <typeparam name="ArgT1">The type of the first argument.</typeparam>
	/// <typeparam name="ArgT2">The type of the second argument.</typeparam>
	/// <typeparam name="ArgT3">The type of the third argument.</typeparam>
	/// <typeparam name="ArgT4">The type of the fourth argument.</typeparam>
	/// <typeparam name="ArgT5">The type of the fifth argument.</typeparam>
	public class RPCHandler<ArgT1, ArgT2, ArgT3, ArgT4, ArgT5> : RPCHandlerBase
	{
		private readonly Action<ArgT1, ArgT2, ArgT3, ArgT4, ArgT5> _action;

		/// <summary>
		/// Initializes an instance of the class <see cref="RPCHandler{ArgT1, ArgT2, ArgT3, ArgT4, ArgT5}"/>
		/// </summary>
		/// <param name="action">The action to perform when the RPC is being executed.</param>
		public RPCHandler(Action<ArgT1, ArgT2, ArgT3, ArgT4, ArgT5> action)
		{
			_action = action;
		}

		internal override void Execute(JToken[] args)
		{
			if (args.Length != 5)
			{
				throw new ArgumentException("Not the correct number of args.  This RPC handler expects five args.");
			}

			_action?.Invoke(
				args[0].ToObject<ArgT1>(),
				args[1].ToObject<ArgT2>(),
				args[2].ToObject<ArgT3>(),
				args[3].ToObject<ArgT4>(),
				args[4].ToObject<ArgT5>()
			);
		}
	}

	/// <summary>
	/// Class that serves as an RPC handler for a procedure that takes six arguments.
	/// </summary>
	/// <typeparam name="ArgT1">The type of the first argument.</typeparam>
	/// <typeparam name="ArgT2">The type of the second argument.</typeparam>
	/// <typeparam name="ArgT3">The type of the third argument.</typeparam>
	/// <typeparam name="ArgT4">The type of the fourth argument.</typeparam>
	/// <typeparam name="ArgT5">The type of the fifth argument.</typeparam>
	/// <typeparam name="ArgT6">The type of the sixth argument.</typeparam>
	public class RPCHandler<ArgT1, ArgT2, ArgT3, ArgT4, ArgT5, ArgT6> : RPCHandlerBase
	{
		private readonly Action<ArgT1, ArgT2, ArgT3, ArgT4, ArgT5, ArgT6> _action;

		/// <summary>
		/// Initializes an instance of the class <see cref="RPCHandler{ArgT1, ArgT2, ArgT3, ArgT4, ArgT5, ArgT6}"/>
		/// </summary>
		/// <param name="action">The action to perform when the RPC is being executed.</param>
		public RPCHandler(Action<ArgT1, ArgT2, ArgT3, ArgT4, ArgT5, ArgT6> action)
		{
			_action = action;
		}

		internal override void Execute(JToken[] args)
		{
			if (args.Length != 6)
			{
				throw new ArgumentException("Not the correct number of args.  This RPC handler expects six args.");
			}

			_action?.Invoke(
				args[0].ToObject<ArgT1>(),
				args[1].ToObject<ArgT2>(),
				args[2].ToObject<ArgT3>(),
				args[3].ToObject<ArgT4>(),
				args[4].ToObject<ArgT5>(),
				args[5].ToObject<ArgT6>()
			);
		}
	}

	/// <summary>
	/// Class that serves as an RPC handler for a procedure that takes seven arguments.
	/// </summary>
	/// <typeparam name="ArgT1">The type of the first argument.</typeparam>
	/// <typeparam name="ArgT2">The type of the second argument.</typeparam>
	/// <typeparam name="ArgT3">The type of the third argument.</typeparam>
	/// <typeparam name="ArgT4">The type of the fourth argument.</typeparam>
	/// <typeparam name="ArgT5">The type of the fifth argument.</typeparam>
	/// <typeparam name="ArgT6">The type of the sixth argument.</typeparam>
	/// <typeparam name="ArgT7">The type of the seventh argument.</typeparam>
	public class RPCHandler<ArgT1, ArgT2, ArgT3, ArgT4, ArgT5, ArgT6, ArgT7> : RPCHandlerBase
	{
		private readonly Action<ArgT1, ArgT2, ArgT3, ArgT4, ArgT5, ArgT6, ArgT7> _action;

		/// <summary>
		/// Initializes an instance of the class <see cref="RPCHandler{ArgT1, ArgT2, ArgT3, ArgT4, ArgT5, ArgT6, ArgT7}"/>
		/// </summary>
		/// <param name="action">The action to perform when the RPC is being executed.</param>
		public RPCHandler(Action<ArgT1, ArgT2, ArgT3, ArgT4, ArgT5, ArgT6, ArgT7> action)
		{
			_action = action;
		}

		internal override void Execute(JToken[] args)
		{
			if (args.Length != 7)
			{
				throw new ArgumentException("Not the correct number of args.  This RPC handler expects seven args.");
			}

			_action?.Invoke(
				args[0].ToObject<ArgT1>(),
				args[1].ToObject<ArgT2>(),
				args[2].ToObject<ArgT3>(),
				args[3].ToObject<ArgT4>(),
				args[4].ToObject<ArgT5>(),
				args[5].ToObject<ArgT6>(),
				args[6].ToObject<ArgT7>()
			);
		}
	}

	/// <summary>
	/// Class that serves as an RPC handler for a procedure that takes eight arguments.
	/// </summary>
	/// <typeparam name="ArgT1">The type of the first argument.</typeparam>
	/// <typeparam name="ArgT2">The type of the second argument.</typeparam>
	/// <typeparam name="ArgT3">The type of the third argument.</typeparam>
	/// <typeparam name="ArgT4">The type of the fourth argument.</typeparam>
	/// <typeparam name="ArgT5">The type of the fifth argument.</typeparam>
	/// <typeparam name="ArgT6">The type of the sixth argument.</typeparam>
	/// <typeparam name="ArgT7">The type of the seventh argument.</typeparam>
	/// <typeparam name="ArgT8">The type of the eighth argument.</typeparam>
	public class RPCHandler<ArgT1, ArgT2, ArgT3, ArgT4, ArgT5, ArgT6, ArgT7, ArgT8> : RPCHandlerBase
	{
		private readonly Action<ArgT1, ArgT2, ArgT3, ArgT4, ArgT5, ArgT6, ArgT7, ArgT8> _action;

		/// <summary>
		/// Initializes an instance of the class <see cref="RPCHandler{ArgT1, ArgT2, ArgT3, ArgT4, ArgT5, ArgT6, ArgT7, ArgT8}"/>
		/// </summary>
		/// <param name="action">The action to perform when the RPC is being executed.</param>
		public RPCHandler(Action<ArgT1, ArgT2, ArgT3, ArgT4, ArgT5, ArgT6, ArgT7, ArgT8> action)
		{
			_action = action;
		}

		internal override void Execute(JToken[] args)
		{
			if (args.Length != 8)
			{
				throw new ArgumentException("Not the correct number of args.  This RPC handler expects eight args.");
			}

			_action?.Invoke(
				args[0].ToObject<ArgT1>(),
				args[1].ToObject<ArgT2>(),
				args[2].ToObject<ArgT3>(),
				args[3].ToObject<ArgT4>(),
				args[4].ToObject<ArgT5>(),
				args[5].ToObject<ArgT6>(),
				args[6].ToObject<ArgT7>(),
				args[7].ToObject<ArgT8>()
			);
		}
	}
}
