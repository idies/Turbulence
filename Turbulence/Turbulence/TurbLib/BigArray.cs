using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Turbulence.TurbLib
{
    // Goal: create an array that allows for a number of elements > Int.MaxValue
    public class BigArray<T>
    {
        // These need to be const so that the getter/setter get inlined by the JIT into 
        // calling methods just like with a real array to have any chance of meeting our 
        // performance goals.
        //
        // BLOCK_SIZE must be a power of 2, and we want it to be big enough that we allocate
        // blocks in the large object heap so that they don't move.
        internal const int BLOCK_SIZE = 134217728;
        internal const int BLOCK_SIZE_LOG2 = 27;

        // Don't use a multi-dimensional array here because then we can't right size the last
        // block and we have to do range checking on our own and since there will then be 
        // exception throwing in our code there is a good chance that the JIT won't inline.
        T[][] _elements;
        ulong _length;

        // maximum BigArray size = BLOCK_SIZE * Int.MaxValue
        public BigArray(ulong size)
        {
            _length = size;
            if (size <= BLOCK_SIZE)
            {
                _elements = new T[1][];
                _elements[0] = new T[size];
            }
            else
            {
                int numBlocks = (int)(size / BLOCK_SIZE);
                ulong NumElementsInLastBlock = BLOCK_SIZE;
                if (((ulong)numBlocks * BLOCK_SIZE) < size)
                {
                    NumElementsInLastBlock = size - (ulong)numBlocks * BLOCK_SIZE;
                    numBlocks += 1;
                }

                _elements = new T[numBlocks][];
                for (int i = 0; i < (numBlocks - 1); i++)
                {
                    _elements[i] = new T[BLOCK_SIZE];
                }
                // by making sure to make the last block right sized then we get the range checks 
                // for free with the normal array range checks and don't have to add our own
                _elements[numBlocks - 1] = new T[NumElementsInLastBlock];
            }
        }

        public ulong Length
        {
            get
            {
                return _length;
            }
        }

        public T this[ulong elementNumber]
        {
            // these must be _very_ simple in order to ensure that they get inlined into
            // their caller 
            get
            {
                int blockNum = (int)(elementNumber >> BLOCK_SIZE_LOG2);
                int elementNumberInBlock = (int)(elementNumber & (BLOCK_SIZE - 1));
                return _elements[blockNum][elementNumberInBlock];
            }
            set
            {
                int blockNum = (int)(elementNumber >> BLOCK_SIZE_LOG2);
                int elementNumberInBlock = (int)(elementNumber & (BLOCK_SIZE - 1));
                _elements[blockNum][elementNumberInBlock] = value;
            }
        }

        public void CopyInto(Array sourceArray, int sourceIndex, ulong destinationIndex, int length)
        {
            int blockNum = (int)(destinationIndex >> BLOCK_SIZE_LOG2);
            ulong elementNumberInBlock = destinationIndex & (BLOCK_SIZE - 1);
            if (elementNumberInBlock + (ulong)length < BLOCK_SIZE)
            {
                Array.Copy(sourceArray, sourceIndex, _elements[blockNum], (int)elementNumberInBlock, length);
            }
            else
            {
                int length1 = (int)(BLOCK_SIZE - elementNumberInBlock);
                Array.Copy(sourceArray, sourceIndex, _elements[blockNum], (int)elementNumberInBlock, length1);
                int length2 = length - length1;
                Array.Copy(sourceArray, sourceIndex + length1, _elements[blockNum + 1], 0, length2);
            }
        }

        public void BlockCopyInto(Array sourceArray, int sourceIndex, ulong destinationIndex, int ByteLength, ulong sizeof_element)
        {
            int blockNum = (int)((destinationIndex / sizeof_element) >> BLOCK_SIZE_LOG2);
            ulong elementNumberInBlock = ((destinationIndex / sizeof_element) & (BLOCK_SIZE - 1)) * sizeof_element;
            if (elementNumberInBlock + (ulong)ByteLength <= BLOCK_SIZE * sizeof_element)
            {
                Buffer.BlockCopy(sourceArray, sourceIndex, _elements[blockNum], (int)elementNumberInBlock, ByteLength);
            }
            else
            {
                int length1 = (int)(BLOCK_SIZE * sizeof_element - elementNumberInBlock);
                Buffer.BlockCopy(sourceArray, sourceIndex, _elements[blockNum], (int)elementNumberInBlock, length1);
                int length2 = ByteLength - length1;
                Buffer.BlockCopy(sourceArray, sourceIndex + length1, _elements[blockNum + 1], 0, length2);
            }
        }
    }
}
