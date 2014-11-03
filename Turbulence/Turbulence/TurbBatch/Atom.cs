using System;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections;
using Turbulence.TurbLib;
using Turbulence.TurbLib.DataTypes;

namespace Turbulence.TurbBatch
{    
	public class Atom : System.IComparable
	{
		// list of queries in a time plane
		public ArrayList queries;		// list of queries pending against time plane
		public int zindex;
		public int time;
		public double utility;
		
		public Atom()
		{
			queries = new ArrayList();
			zindex = -1;
			time = -1;
			utility = -1;
		}

		/// <summary>
		/// compare two atoms based on utility
		/// </summary>
		public int CompareTo(object obj)
		{
			Atom a = (Atom)obj;
			return a.utility.CompareTo(utility);
		}

	}
}
