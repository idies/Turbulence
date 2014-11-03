using System;
using System.Data;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using Microsoft.SqlServer.Server;

/// <summary>
/// The primary index for addresses in the database involves the use
/// of a Morton (aka Z-Order) curve.
/// 
/// Regions are addressed using the corner value with min(x),min(y),min(z).
/// Bits are interleaved in the form: {Zn,Yn,Xn}...{Z0,Y0,X0).
/// 
/// 3 values up to 21 bits in length each are encoded.  The 64th bit (MSB)
/// is reserved for special values.  Currently, the only special value used
/// is -1 for NULL.
/// 
/// TODO: Add conversion from comma-deliminated Zd,Yd,Xd string input.
/// TODO: Make number of bits generic (currently 16)
/// [Microsoft.SqlServer.Server.SqlUserDefinedType(Format.Native, IsByteOrdered=true, IsFixedLength=true)]
/// </summary>
[Serializable]
public struct Morton3D : INullable
{
    private long key; // key storage

    public long Key
    {
        get { return key; }
        set { key = value; }
    }

    public static implicit operator long(Morton3D morton)
    {
        return morton.key;
    }

    public override string ToString()
    {
        return key.ToString();
    }

    public string ToPrettyString()
    {
        int[] v = this.GetValues();
        return String.Format("({0},{1},{2})", v[0], v[1], v[2]);
    }

    public int X
    {
        get { return GetComponent(0); }
        set { SetComponent(value, 0); }
    }
    public int Y
    {
        get { return GetComponent(1); }
        set { SetComponent(value, 1); }
    }
    public int Z
    {
        get { return GetComponent(2); }
        set { SetComponent(value, 2); }
    }

    /// <summary>
    /// Initialize with a precomputed index
    /// </summary>
    /// <param name="key"></param>
    public Morton3D(long key)
    {
        this.key = key;
    }

    /// <summary>
    /// Create the 3D Morton index from the Z,Y,X values
    /// </summary>
    /// <param name="z"></param>
    /// <param name="y"></param>
    /// <param name="x"></param>
    public Morton3D (int z, int y, int x)
    {
        key = 0;
        SetKey(z, y, x);
    }

    private void SetKey(int z, int y, int x)
    {
        //key = 0;
        //int shift = 0;
        //int mask = 1;
        //for (int i = 0; i < 16; i++)
        //{
        //    key |= (long)(x & mask) << shift;
        //    shift++;
        //    key |= (long)(y & mask) << shift;
        //    shift++;
        //    key |= (long)(z & mask) << shift;
        //    mask = mask << 1;
        //}
        
        key = (PartBy2(z) << 2) | (PartBy2(y) << 1) | PartBy2(x);
    }

    // "Insert" two 0 bits after each of the 10 low bits of x
    private long PartBy2(long i)
    {
        long x = i & 0x1fffff; // we only look at the first 21 bits
        x = (x | x << 32) & 0x1f00000000ffff;  // shift left 32 bits, OR with self, and 00011111000000000000000000000000000000001111111111111111
        x = (x | x << 16) & 0x1f0000ff0000ff;  // shift left 32 bits, OR with self, and 00011111000000000000000011111111000000000000000011111111
        x = (x | x << 8) & 0x100f00f00f00f00f; // shift left 32 bits, OR with self, and 0001000000001111000000001111000000001111000000001111000000000000
        x = (x | x << 4) & 0x10c30c30c30c30c3; // shift left 32 bits, OR with self, and 0001000011000011000011000011000011000011000011000011000100000000
        x = (x | x << 2) & 0x1249249249249249;
        return x;

        //i &= 0x000003ff;                  // i = ---- ---- ---- ---- ---- --98 7654 3210
        //i = (i ^ (i << 16)) & 0xff0000ff; // i = ---- --98 ---- ---- ---- ---- 7654 3210
        //i = (i ^ (i << 8)) & 0x0300f00f;  // i = ---- --98 ---- ---- 7654 ---- ---- 3210
        //i = (i ^ (i << 4)) & 0x030c30c3;  // i = ---- --98 ---- 76-- --54 ---- 32-- --10
        //i = (i ^ (i << 2)) & 0x09249249;  // i = ---- 9--8 --7- -6-- 5--4 --3- -2-- 1--0
        //return i;
    }

    // Inverse of PartBy2 - "delete" all bits not at positions divisible by 3
    private long CompactBy2(long i)
    {
        long x = i & 0x1249249249249249;
        x = (x | x >> 2) & 0x10c30c30c30c30c3;
        x = (x | x >> 4) & 0x100f00f00f00f00f;
        x = (x | x >> 8) & 0x1f0000ff0000ff;
        x = (x | x >> 16) & 0x1f00000000ffff;
        x = (x | x >> 32) & 0x1fffff;
        return x;

        //i &= 0x09249249;                  // i = ---- 9--8 --7- -6-- 5--4 --3- -2-- 1--0
        //i = (i ^ (i >> 2)) & 0x030c30c3;  // i = ---- --98 ---- 76-- --54 ---- 32-- --10
        //i = (i ^ (i >> 4)) & 0x0300f00f;  // i = ---- --98 ---- ---- 7654 ---- ---- 3210
        //i = (i ^ (i >> 8)) & 0xff0000ff;  // i = ---- --98 ---- ---- ---- ---- 7654 3210
        //i = (i ^ (i >> 16)) & 0x000003ff;  // i = ---- ---- ---- ---- ---- --98 7654 3210
        //return i;
    }

    /// <summary>
    /// Set a specific component in the key without changing the others
    /// </summary>
    /// <param name="value">Value</param>
    /// <param name="component">Component [Z=2, Y=1, X=0]</param>
    private void SetComponent(int value, int component)
    {
        /*
        int[] x = GetValues();
        x[2 - component] = value;
        key = (new Morton3D(x[0], x[1], x[2])).key;
        */
        int shift = component;
        int mask = 1;
        for (int i = 0; i < 16; i++)
        {
            if ( (value & mask) != 0)
            {
                // set bit
                key |= (long)(value & mask) << shift;
            }
            else
            {
                // unset bit
                key &= (long) ~((value & mask) << shift);
            }
            shift += 2;
            mask = mask << 1;
        }
    }
    
    /// <summary>
    /// Retrieve the dimensions out of a morton order key
    /// </summary>
    /// <returns>The array int[z,y,x].</returns>
    public int[] GetValues()
    {
        //int shift = 0;
        //int mask = 1;

        int[] value = { 0, 0, 0 };

        //if (IsNull) { return null; }
        //for (int i = 0; i < 16; i++)
        //{
        //    value[2] |= (int)(key >> shift) & mask;
        //    shift++;
        //    value[1] |= (int)(key >> shift) & mask;
        //    shift++;
        //    value[0] |= (int)(key >> shift) & mask;
        //    mask = mask << 1;
        //}
        
        value[2] = (int)CompactBy2(key >> 0);
        value[1] = (int)CompactBy2(key >> 1);
        value[0] = (int)CompactBy2(key >> 2);
        return value;
    }

    /// <summary>
    /// Retrieve a specific component from the key.
    /// </summary>
    /// <param name="component">Component [Z=Z, Y=1, X=0]</param>
    private int GetComponent(int component)
    {
        //int shift = component;
        //int mask = 1;
        //int value = 0;

        //if (IsNull) { return -1; }
        //for (int i = 0; i < 16; i++)
        //{
        //    value |= (int)(key >> shift) & mask;
        //    shift += 2;
        //    mask = mask << 1;
        //}
        //return value;
        
        return (int)CompactBy2(key >> component);
    }

    public void IncrementComponent(int component)
    {
        long mask;
        for (int i = component; i < 32; i += 3)
        {
            //(code & ( 1 << i )) >> i
            mask = (long)1 << i;
            if (((key & mask) >> i) == 0)
            {
                key |= mask;
                return;
            }
            else
            {
                key &= ~mask;
            }
        }
        throw new Exception("Arithmetic overflow!");
    }

    public void DecrementComponent(int component)
    {
        long mask;
        for (int i = component; i < 32; i += 3)
        {
            //(code & ( 1 << i )) >> i
            mask = (long)1 << i;
            if (((key & mask) >> i) == 0)
            {
                key |= mask;
            }
            else
            {
                key &= ~mask;
                return;
            }
        }
        throw new Exception("Arithmetic underflow! Component can't be negative!");
    }

    public string ToBinaryString()
    {
        System.Text.StringBuilder s = new System.Text.StringBuilder(65);
        for (int i = 63; i > 0; i--)
        {
            if ((key & ((long)1 << i)) == 0)
            {
                s.Append('0');
            } else {
                s.Append('1');
            }
        }
        s.Append('b');
        return s.ToString();
    }

    public bool IsNull
    {
        get { return key.Equals(-1); }
    }

    public static Morton3D Null
    {
        get
        {
            Morton3D z = new Morton3D();
            z.key = -1;
            return z;
        }
    }

    public static Morton3D Parse(SqlString s)
    {
        if (s.IsNull)
            return Null;
        Morton3D z = new Morton3D(long.Parse(s.ToString()));
        return z;
    }

}


