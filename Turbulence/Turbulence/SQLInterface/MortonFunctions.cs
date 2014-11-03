using System;
using System.Data;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using Microsoft.SqlServer.Server;
using Turbulence;

public partial class UserDefinedFunctions
{
    /// <summary>
    /// Return the 3-D Morton index associated with three values
    /// </summary>
    /// <param name="z"></param>
    /// <param name="y"></param>
    /// <param name="x"></param>
    /// <returns>Morton Index</returns>
    [SqlFunction(IsDeterministic=true, IsPrecise=true, DataAccess=DataAccessKind.None)]
    public static SqlInt64 CreateMortonIndex(int z, int y, int x)
    {
        Morton3D morton = new Morton3D(z, y, x);
        // Put your code here
        return new SqlInt64(morton);
    }

    [SqlFunction(IsDeterministic=true, IsPrecise= true, DataAccess = DataAccessKind.None)]
    public static SqlInt32 GetMortonX(long key)
    {
        Morton3D morton = new Morton3D(key);
        return new SqlInt32(morton.X);
    }

    [SqlFunction(IsDeterministic=true, IsPrecise=true, DataAccess=DataAccessKind.None)]
    public static SqlInt32 GetMortonY(long key)
    {
        Morton3D morton = new Morton3D(key);
        return new SqlInt32(morton.Y);
    }

    [SqlFunction(IsDeterministic=true, IsPrecise=true, DataAccess=DataAccessKind.None)]
    public static SqlInt32 GetMortonZ(long key)
    {
        Morton3D morton = new Morton3D(key);
        return new SqlInt32(morton.Z);
    }

};

