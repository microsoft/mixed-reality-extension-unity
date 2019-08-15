using System;
using System.Collections.Generic;

namespace MixedRealityExtension.Patching
{
    public class PatchProperty<PropT> : IEquatable<PatchProperty<PropT>>
    {
        private PropT _value = default(PropT);

        public bool Write { get; set; } = false;

        public PropT Value
        {
            get => _value;
            set
            {
                _value = value; Write = true;
            }
        }

        public bool Equals(PatchProperty<PropT> other)
        {
            if (ReferenceEquals(null, other))
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return Value.Equals(other.Value);
        }

        public override bool Equals(object other)
        {
            return GetType() == other.GetType() && Equals((PatchProperty<PropT>)other);
        }

        public static bool operator ==(PatchProperty<PropT> lhs, PatchProperty<PropT> rhs)
        {
            if (ReferenceEquals(null, lhs) || ReferenceEquals(null, rhs))
            {
                return false;
            }

            if (ReferenceEquals(lhs, rhs))
            {
                return true;
            }

            return EqualityComparer<PropT>.Default.Equals(lhs.Value, rhs.Value);
        }

        public static bool operator !=(PatchProperty<PropT> lhs, PatchProperty<PropT> rhs)
        {
            return !(lhs == rhs);
        }

        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }
    }
}
