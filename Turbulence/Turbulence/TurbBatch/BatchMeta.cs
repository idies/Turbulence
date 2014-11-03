using System;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections;
using Turbulence.TurbLib.DataTypes;
using Turbulence.TurbLib;

namespace Turbulence.TurbBatch
{    
	/// <summary>
	/// metadata for batched query
	/// encodes boundary between different queries and map a batch point to the origina query and insertion index
	/// also encode different computation parameters between queries
	/// </summary>
	public class BatchMeta
	{
		private int[] boundary;			// index of query boundaries
		private int[][] localOrder;		// point's local order within the original query
		private TurbulenceOptions.SpatialInterpolation[] spatialInterp;	// spatial interpolation for different queries
		private TurbulenceOptions.TemporalInterpolation[] temporalInterp;	// temporal interpolation for different queries
        private bool[] round;	// rounding for different queries
        private int[] kernelSize;	// kernel size of computation for different queries
		private BatchQuery[] queries;	// source query

		private int count;

		/// <summary>
		/// constructor
		/// </summary>
		public BatchMeta(int total)
		{
			boundary = new int[total];
			localOrder = new int[total][];
			spatialInterp = new TurbulenceOptions.SpatialInterpolation[total];
			temporalInterp = new TurbulenceOptions.TemporalInterpolation[total];
            round = new bool[total];
            kernelSize = new int[total];
			queries = new BatchQuery[total];

			count = 0;
		}

		/// <summary>
		/// return the original insertion order of a point
		/// </summary>
		public int lookupLocalOrder(int index)
		{
			for(int i=0; i<boundary.Length; ++i)
			{
				if(index <= boundary[i])
				{
					if(i==0)
						return localOrder[i][index];
					else
						return localOrder[i][index-boundary[i-1]-1];
				}
			}
			throw new Exception("invalid lookup local order index "+index);
		}

		/// <summary>
		/// return the original query of a given point
		/// </summary>
		public BatchQuery lookupQuery(int index)
		{
			for(int i=0; i<boundary.Length; ++i)
			{
				if(index <= boundary[i])
					return queries[i];
			}
			throw new Exception("invalid lookup query index "+index);
		}

		/// <summary>
		/// encode query boundary and computation in string field
		/// </summary>
		public string toBoundaryString()
		{
			string ret = "";
			for(int i=0; i<boundary.Length; ++i)
			{
				if(i>0)
					ret += ";";
				ret += boundary[i]+","+(int)spatialInterp[i]+","+(int)temporalInterp[i]+","+(bool)round[i]+","+(int)kernelSize[i];
			}
			return ret;
		}

		/// <summary>
		/// insert new query and create boundary
		/// return the set of points inserted
		/// </summary>
		public Point3[] insertQuery(BatchQuery query, ArrayList points, Point3[] mergedPoints)
		{
			int basei = 0;
			localOrder[count] = new int[points.Count];
			Point3[] ret = new Point3[points.Count];
			if(count == 0)
				boundary[count] = points.Count-1;
			else
			{
				boundary[count] = boundary[count-1] + points.Count;
				basei = boundary[count-1]+1;
			}

			// merge points and record orignal insertion index
			for(int i=0; i<points.Count; ++i)
			{
				int index = (int)points[i];
				mergedPoints[basei+i] = query.points[index];
				ret[i] = query.points[index];
				localOrder[count][i] = index;
			}

			spatialInterp[count] = query.spatialInterpolation;
			temporalInterp[count] = query.temporalInterpolation;
            round[count] = query.round;
            kernelSize[count] = query.kernelSize;
			queries[count] = query;
			++count;

			return ret;
		}

	}
}
