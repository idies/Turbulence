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
        key = 0;
        int shift = 0;
        int mask = 1;
        for (int i = 0; i < 16; i++)
        {
            key |= (long)(x & mask) << shift;
            shift++;
            key |= (long)(y & mask) << shift;
            shift++;
            key |= (long)(z & mask) << shift;
            mask = mask << 1;
        }
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
        int shift = 0;
        int mask = 1;
        int[] value = { 0, 0, 0 };

        if (IsNull) { return null; }
        for (int i = 0; i < 16; i++)
        {
            value[2] |= (int)(key >> shift) & mask;
            shift++;
            value[1] |= (int)(key >> shift) & mask;
            shift++;
            value[0] |= (int)(key >> shift) & mask;
            mask = mask << 1;
        }
        return value;
    }

    /// <summary>
    /// Retrieve a specific component from the key.
    /// </summary>
    /// <param name="component">Component [Z=Z, Y=1, X=0]</param>
    private int GetComponent(int component)
    {
        int shift = component;
        int mask = 1;
        int value = 0;

        if (IsNull) {  return -1; }
        for (int i = 0; i < 16; i++)
        {
            value |= (int)(key >> shift) & mask;
            shift += 2;
            mask = mask << 1;
        }
        return value;
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


