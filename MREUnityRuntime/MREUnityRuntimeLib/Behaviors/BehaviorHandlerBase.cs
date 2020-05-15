// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using MixedRealityExtension.App;
using MixedRealityExtension.Behaviors.Actions;
using MixedRealityExtension.Core.Interfaces;
using MixedRealityExtension.PluginInterfaces.Behaviors;
using System;
using System.Linq;

namespace MixedRealityExtension.Behaviors
{
	internal abstract class BehaviorHandlerBase : IBehaviorHandler
	{
		private readonly WeakReference<MixedRealityExtensionApp> _appRef;
		private readonly Guid _attachedActorId;

		private BehaviorType? _behaviorType;

		protected IBehavior Behavior { get; private set; }

		BehaviorType IBehaviorHandler.BehaviorType
		{
			get
			{
				_behaviorType = _behaviorType ?? GetBehaviorType();
				return _behaviorType.Value;
			}
		}

		IBehavior IBehaviorHandler.Behavior => Behavior;

		internal BehaviorHandlerBase(
			IBehavior behavior,
			WeakReference<MixedRealityExtensionApp> appRef, 
			IActor attachedActor)
		{
			Behavior = behavior;
			_appRef = appRef;
			_attachedActorId = attachedActor.Id;

			Behavior.Actor = attachedActor;
		}

		protected virtual void FixedUpdate()
		{

		}

		protected virtual void SynchronizeBehavior()
		{

		}

		protected virtual void CleanUp()
		{

		}

		protected void RegisterActionHandler(MWActionBase action, string name)
		{
			var handler = new BehaviorActionHandler(((IBehaviorHandler)this).BehaviorType, name, _appRef, _attachedActorId);
			action.Handler = handler;
		}

		public bool Equals(IBehaviorHandler other)
		{
			return ((IBehaviorHandler)this).BehaviorType == other.BehaviorType;
		}

		public override bool Equals(object obj)
		{
			return Equals(obj as IBehaviorHandler);
		}

		public override int GetHashCode()
		{
			return ((IBehaviorHandler)this).BehaviorType.GetHashCode();
		}
		

		private BehaviorType GetBehaviorType()
		{
			var behaviorEnumType = typeof(BehaviorType);
			foreach (var name in behaviorEnumType.GetEnumNames())
			{
				var behaviorContextTypeAttr = behaviorEnumType.GetField(name)
					.GetCustomAttributes(false)
					.OfType<BehaviorContextType>()
					.SingleOrDefault();
		
				if (this.GetType() == behaviorContextTypeAttr?.ContextType)
				{
					return (BehaviorType)Enum.Parse(typeof(BehaviorType), name);
				}
			}
		
			return BehaviorType.None;
		}

		#region IBehaviorHandler Interface

		void IBehaviorHandler.FixedUpdate()
		{
			FixedUpdate();
		}

		void IBehaviorHandler.SynchronizeBehavior()
		{
			SynchronizeBehavior();
		}

		void IBehaviorHandler.CleanUp()
		{
			var behavior = Behavior;
			Behavior = null;

			behavior.CleanUp();
		}

		bool IEquatable<IBehaviorHandler>.Equals(IBehaviorHandler other)
		{
			return ((IBehaviorHandler)this).BehaviorType == other.BehaviorType;
		}

		#endregion
	}
}
