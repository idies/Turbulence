using System;
using System.Data;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using Microsoft.SqlServer.Server;


/// <summary>
/// Group the attributes of particles into a single struct for use in temporary SQL workload tables.
/// </summary>
/*
[Serializable]
[Microsoft.SqlServer.Server.SqlUserDefinedType(Format.Native)]
public struct Particle : INullable
{
    private float x;
    private float y ;
    private float z;
   
    public override string ToString()
    {
        x = 0;
        y = 0;
        z = 0;
        // Replace the following code with your code
        return String.Format("( {0}, {1}, {2} )", x, y, z);
    }

    public bool IsNull
    {
        get
        {
            // Put your code here
            return m_Null;
        }
    }

    public static Particle Null
    {
        get
        {
            Particle h = new Particle();
            h.m_Null = true;
            return h;
        }
    }

    public static Particle Parse(SqlString s)
    {
        if (s.IsNull)
            return Null;
        Particle u = new Particle();
        // Put your code here
        return u;
    }

    // This is a place-holder method
    public string Method1()
    {
        //Insert method code here
        return "Hello";
    }

    // This is a place-holder static method
    public static SqlString Method2()
    {
        //Insert method code here
        return new SqlString("Hello");
    }

    // This is a place-holder field member
    public int var1;
    // Private member
    private bool m_Null;
}


*/