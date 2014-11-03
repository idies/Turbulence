#pragma once

#include <cstddef>

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
class Morton3D
{
public:

	Morton3D(void);
	
	/// <summary>
    /// Initialize with a precomputed index
    /// </summary>
    /// <param name="key"></param>
    Morton3D(long long key)
    {
        this->key = key;
    }
	
	/// <summary>
    /// Create the 3D Morton index from the Z,Y,X values
    /// </summary>
    /// <param name="z"></param>
    /// <param name="y"></param>
    /// <param name="x"></param>
    Morton3D (int z, int y, int x)
    {
        key = 0;
        SetKey(z, y, x);
    }

	~Morton3D(void);

private:
	long long key; // key storage

public:
	long long Key(){ return key; };
	void Key( long long value ){ key = value; };

	int X(){ return GetComponent(0); };
	void X( int value ){ SetComponent(value, 0); };

	int Y(){ return GetComponent(1); };
	void Y( int value ){ SetComponent(value, 1); };

	int Z(){ return GetComponent(2); };
	void Z( int value ){ SetComponent(value, 2); };

	
//	/*static implicit operator long long(Morton3D morton)
//    {
//        return morton.key;
//    }*/

private:
	void SetKey(int z, int y, int x)
    {
        key = 0;
        int shift = 0;
        int mask = 1;
        for (int i = 0; i < 16; i++)
        {
            key |= (long long)(x & mask) << shift;
            shift++;
            key |= (long long)(y & mask) << shift;
            shift++;
            key |= (long long)(z & mask) << shift;
            mask = mask << 1;
        }
    }

    /// <summary>
    /// Set a specific component in the key without changing the others
    /// </summary>
    /// <param name="value">Value</param>
    /// <param name="component">Component [Z=2, Y=1, X=0]</param>
    void SetComponent(int value, int component)
    {
        int shift = component;
        int mask = 1;
        for (int i = 0; i < 16; i++)
        {
            if ( (value & mask) != 0)
            {
                // set bit
                key |= (long long)(value & mask) << shift;
            }
            else
            {
                // unset bit
                key &= (long long) ~((value & mask) << shift);
            }
            shift += 2;
            mask = mask << 1;
        }
    }
 
    /// <summary>
    /// Retrieve a specific component from the key.
    /// </summary>
    /// <param name="component">Component [Z=2, Y=1, X=0]</param>
	int GetComponent(int component)
    {
        int shift = component;
        int mask = 1;
        int value = 0;

        if (IsNull()) {  return -1; }
        for (int i = 0; i < 16; i++)
        {
            value |= (int)(key >> shift) & mask;
            shift += 2;
            mask = mask << 1;
        }
        return value;
    }
	
public: 
    /// <summary>
    /// Retrieve the dimensions out of a morton order key
    /// </summary>
    /// <returns>The array int[z,y,x].</returns>
	void GetValues(int value[3])
    {
        int shift = 0;
        int mask = 1;
        value[0] = 0;
		value[1] = 0;
		value[2] = 0;

        if (IsNull()) 
		{ 
			value = NULL;
			return; 
		}
        for (int i = 0; i < 16; i++)
        {
            value[2] |= (int)(key >> shift) & mask;
            shift++;
            value[1] |= (int)(key >> shift) & mask;
            shift++;
            value[0] |= (int)(key >> shift) & mask;
            mask = mask << 1;
        }
    }

//public:
//	string ToBinaryString()
//    {
//        System.Text.StringBuilder s = new System.Text.StringBuilder(65);
//        for (int i = 63; i > 0; i--)
//        {
//            if ((key & ((long long)1 << i)) == 0)
//            {
//                s.Append('0');
//            } else {
//                s.Append('1');
//            }
//        }
//        s.Append('b');
//        return s.ToString();
//    }

	bool IsNull()
    {
		return (key == -1);
    }

//	static Morton3D Null
//    {
//        get
//        {
//            Morton3D z = new Morton3D();
//            z.key = -1;
//            return z;
//        }
//    }
//
//	static Morton3D Parse(SqlString s)
//    {
//        if (s.IsNull)
//            return Null;
//        Morton3D z = new Morton3D(long long.Parse(s.ToString()));
//        return z;
//    }
};
