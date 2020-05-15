using MixedRealityExtension.Behaviors.ActionData;

namespace MixedRealityExtension.Behaviors.Contexts
{
	public class PenBehaviorContext : PhysicalToolBehaviorContext<PenData>
	{
		internal PenBehaviorContext()
			: base()
		{

		}

		internal override void FixedUpdate()
		{
			base.FixedUpdate();
			if (IsUsing)
			{
				ToolData.DrawData.Add(new DrawData() { Transform = Behavior.Actor.AppTransform });
			}
		}
	}
}
