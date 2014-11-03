using System;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections;

namespace Turbulence.TurbBatch
{    
	public class BatchCounter
	{
		public int count;
		public DateTime oldest;

		public BatchCounter()
		{
			count = 0;
			oldest = DateTime.Now;
		}

	}
}
