using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Turbulence.TurbLib
{
    //TODO: This struct is only needed, because we are using the 3.5 framework 
    //      in order to be able to deploy the library to SQL Server 2008.
    //      The 4.0 framework already has a Tuple class.
    public struct Tuple2<T1, T2> : IEquatable<Tuple2<T1, T2>>
    {
        readonly T1 item1;
        readonly T2 item2;

        public T1 Item1 { get { return item1; } }
        public T2 Item2 { get { return item2; } }

        public Tuple2(T1 item1, T2 item2)
        {
            this.item1 = item1;
            this.item2 = item2;
        }

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }
            return Equals((Tuple2<T1, T2>)obj);
        }

        public bool Equals(Tuple2<T1, T2> other)
        {
            return other.item1.Equals(item1) && other.item2.Equals(item2);
        }

        public override int GetHashCode()
        {
            return item1.GetHashCode() ^ item2.GetHashCode();
        }
    }
}
