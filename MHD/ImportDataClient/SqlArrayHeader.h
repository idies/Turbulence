template <class IndexT>
	struct SqlArrayHeader
	{
	public:
		/// <summary>Bit indicating of it's a short (=1) or max (=0) array.</summary>
		unsigned HeaderType : 1;

		/// <summary>Bit indicating the index ordering of the array.</summary>
		/// <remarks>Only column major ordering is supported.</remarks>
		unsigned ColumnMajor : 1;

		/// <summary>Reserved bits for padding</summary>
		unsigned Reserved : 6;

		/// <summary>Element type of the array.</summary>
		unsigned DataType : 4;

		/// <summary>Rank (number of indices) of the array.</summary>
		unsigned Rank : 4;

		// Reserved bits for padding to the next 4 bytes
		unsigned Reserved2 : 16;
		
		/// <summary>Number of all elements in the array</summary>
		IndexT Length;
	};
