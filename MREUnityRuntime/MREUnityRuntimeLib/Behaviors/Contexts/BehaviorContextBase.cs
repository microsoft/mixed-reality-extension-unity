using MixedRealityExtension.App;
using MixedRealityExtension.Behaviors.Actions;
using MixedRealityExtension.Core.Interfaces;
using MixedRealityExtension.PluginInterfaces.Behaviors;
using System;
using System.Linq;

namespace MixedRealityExtension.Behaviors.Contexts
{
	public abstract class BehaviorContextBase
	{
		private WeakReference<MixedRealityExtensionApp> _appRef;
		private Guid _attachedActorId;
		private BehaviorType? _behaviorType;

		internal IBehavior Behavior { get; private set; }

		internal BehaviorType BehaviorType
		{
			get
			{
				_behaviorType = _behaviorType ?? GetBehaviorType();
				return _behaviorType.Value;
			}
		}

		internal BehaviorContextBase()
		{
			
		}

		internal void Initialize(
			IBehavior behavior,
			WeakReference<MixedRealityExtensionApp> appRef,
			IActor attachedActor)
		{
			Behavior = behavior;
			_appRef = appRef;
			_attachedActorId = attachedActor.Id;

			Behavior.Actor = attachedActor;
		}

		internal virtual void FixedUpdate()
		{

		}

		internal virtual void SynchronizeBehavior()
		{

		}

		internal virtual void CleanUp()
		{
			var behavior = Behavior;
			Behavior = null;

			behavior.CleanUp();
		}

		protected void RegisterActionHandler(MWActionBase action, string name)
		{
			var handler = new BehaviorActionHandler(((IBehaviorHandler)this).BehaviorType, name, _appRef, _attachedActorId);
			action.Handler = handler;
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
	}
}
