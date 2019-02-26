using MixedRealityExtension.Core.Types;
using MixedRealityExtension.Util.Unity;
using UnityEngine;

namespace MixedRealityExtension.Core
{
    public abstract class ColliderGeometry
    {
        public abstract ColliderType ColliderType { get; }

        internal abstract void Patch(UnityEngine.Collider collider);
    }

    public class SphereColliderGeometry : ColliderGeometry
    {
        public override ColliderType ColliderType => ColliderType.Sphere;

        public float? Radius { get; set; }

        public MWVector3 Center { get; set; }

        internal override void Patch(UnityEngine.Collider collider)
        {
            if (collider is SphereCollider sphereCollider)
            {
                Patch(sphereCollider);
            }
        }

        private void Patch(SphereCollider collider)
        {
            if (Center != null)
            {
                collider.center = Center.ToVector3();
            }

            if (Radius != null)
            {
                collider.radius = Radius.Value;
            }
        }
    }

    public class BoxColliderGeometry : ColliderGeometry
    {
        public override ColliderType ColliderType => ColliderType.Box;

        public MWVector3 Size { get; set; }

        public MWVector3 Center { get; set; }

        internal override void Patch(UnityEngine.Collider collider)
        {
            if (collider is BoxCollider boxCollider)
            {
                Patch(boxCollider);
            }
        }

        private void Patch(BoxCollider collider)
        {
            if (Center != null)
            {
                collider.center = Center.ToVector3();
            }

            if (Size != null)
            {
                collider.size = Size.ToVector3();
            }
        }
    }
}
