using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HDF5DotNet;

namespace ImportData
{
    public class HDF5Reader
    {
        H5FileId file;
        H5DataSetId[] dataset;
        H5DataSpaceId[] dataspace;
        H5DataSpaceId memspace;
        H5DataTypeId datatype;
        H5T.H5TClass dataclass;
        H5T.Order order;

        //Task[] reading_tasks;

        long[] dims_out;

        string data_dir = "";
        int timestep = -1;
        long firstbox = -1;
        long lastbox = -1;
        int atomSize = 8;
        int components = 0;
        bool open_file = false;

        long[] cache_dimensions;
        float[] data0;
        float[] data1;
        float[] data2;

        int[] base_coordinates = { -1, -1, -1 };
        // coordiantes of the lower left corner (base) of values currently cached
        public int[] Base { get { return base_coordinates; } }

        public HDF5Reader(string data_dir, int components)
        {
            cache_dimensions = new long[3];
            this.data_dir = data_dir;
            this.components = components;
            this.dataset = new H5DataSetId[components];
            this.dataspace = new H5DataSpaceId[components];
            //reading_tasks = new Task[components];
        }

        public void Close()
        {
            H5T.close(datatype);
            for (int i = 0; i < components; i++)
            {
                H5D.close(dataset[i]);
                H5S.close(dataspace[i]);
            }
            //H5D.close(dataset0);
            //H5S.close(dataspace0);
            //if (components == 3)
            //{
            //    H5D.close(dataset1);
            //    H5D.close(dataset2);
            //    H5S.close(dataspace1);
            //    H5S.close(dataspace2);
            //}
            H5S.close(memspace);
            H5F.close(file);
        }

        public void readFiles(int timestep, long firstbox, long lastbox, int atomSize)
        {
            if ((this.timestep == timestep) && (this.firstbox == firstbox) && (this.lastbox == lastbox))
            {
                return;
            }

            this.firstbox = firstbox;
            this.lastbox = lastbox;

            // Z-coord of the base region loaded into memory
            base_coordinates = new Morton3D(firstbox).GetValues();
            int[] end_coordinates = new Morton3D(lastbox).GetValues();

            long[] start = { base_coordinates[0], base_coordinates[1], base_coordinates[2] };
            cache_dimensions[0] = end_coordinates[0] + atomSize - base_coordinates[0];
            cache_dimensions[1] = end_coordinates[1] + atomSize - base_coordinates[1];
            cache_dimensions[2] = end_coordinates[2] + atomSize - base_coordinates[2];
            long dataSize = cache_dimensions[2] * cache_dimensions[1] * cache_dimensions[0];

            if (this.data0 == null)
            {
                data0 = new float[dataSize];
                if (components == 3)
                {
                    data1 = new float[dataSize];
                    data2 = new float[dataSize];
                }
            }


            // If this is a new timestep we need to open the associated file.
            if (this.timestep != timestep)
            {
                this.timestep = timestep;
                if (open_file)
                {
                    H5T.close(datatype);
                    for (int i = 0; i < components; i++)
                    {
                        H5D.close(dataset[i]);
                        H5S.close(dataspace[i]);
                    }
                    //H5D.close(dataset0);
                    //H5S.close(dataspace0);
                    //if (components == 3)
                    //{
                    //    H5D.close(dataset1);
                    //    H5D.close(dataset2);
                    //    H5S.close(dataspace1);
                    //    H5S.close(dataspace2);
                    //}
                    H5S.close(memspace);
                    H5F.close(file);
                }

                string filename = GetFileName(timestep);
                file = H5F.open(filename, H5F.OpenMode.ACC_RDONLY);
                if (components == 3)
                {
                    dataset[0] = H5D.open(file, "u");
                    dataspace[0] = H5D.getSpace(dataset[0]);
                    dataset[1] = H5D.open(file, "v");
                    dataspace[1] = H5D.getSpace(dataset[1]);
                    dataset[2] = H5D.open(file, "w");
                    dataspace[2] = H5D.getSpace(dataset[2]);
                    //dataset0 = H5D.open(file, "u");
                    //dataspace0 = H5D.getSpace(dataset0);
                    //dataset1 = H5D.open(file, "v");
                    //dataspace1 = H5D.getSpace(dataset1);
                    //dataset2 = H5D.open(file, "w");
                    //dataspace2 = H5D.getSpace(dataset2);
                }
                else
                {
                    dataset[0] = H5D.open(file, "p");
                    dataspace[0] = H5D.getSpace(dataset[0]);
                    //dataset0 = H5D.open(file, "p");
                    //dataspace0 = H5D.getSpace(dataset0);
                }

                datatype = H5D.getType(dataset[0]);     /* datatype handle */
                dataclass = H5T.getClass(datatype);
                if (dataclass == H5T.H5TClass.FLOAT)
                {
                    Console.WriteLine("Data set has FLOAT type.");
                }

                order = H5T.get_order(datatype);
                if (order == H5T.Order.LE)
                {
                    Console.WriteLine("Little endian order.");
                }

                int size = H5T.getSize(datatype);
                Console.WriteLine("Data size is {0}.", size);

                int rank = H5S.getSimpleExtentNDims(dataspace[0]);
                dims_out = H5S.getSimpleExtentDims(dataspace[0]);
                Console.WriteLine("Rank {0}, dimensions {1} x {2} x {3}.", rank, dims_out[0], dims_out[1], dims_out[2]);

                memspace = H5S.create_simple(3, cache_dimensions);
                open_file = true;
            }

            // Read the data.
            //ReadDataSetsAsync(start);
            ReadDataSetsSequentially(start);
        }

        private void ReadDataSetsSequentially(long[] start)
        {
            H5S.selectHyperslab(dataspace[0], H5S.SelectOperator.SET, start, cache_dimensions);
            H5D.read<float>(dataset[0], datatype, memspace, dataspace[0], new H5PropertyListId(H5P.Template.DEFAULT), new H5Array<float>(data0));
            if (components == 3)
            {
                H5S.selectHyperslab(dataspace[1], H5S.SelectOperator.SET, start, cache_dimensions);
                H5D.read<float>(dataset[1], datatype, memspace, dataspace[1], new H5PropertyListId(H5P.Template.DEFAULT), new H5Array<float>(data1));
                H5S.selectHyperslab(dataspace[2], H5S.SelectOperator.SET, start, cache_dimensions);
                H5D.read<float>(dataset[2], datatype, memspace, dataspace[2], new H5PropertyListId(H5P.Template.DEFAULT), new H5Array<float>(data2));
            }
            //H5S.selectHyperslab(dataspace0, H5S.SelectOperator.SET, start, cache_dimensions);
            //H5D.read<float>(dataset0, datatype, memspace, dataspace0, new H5PropertyListId(H5P.Template.DEFAULT), new H5Array<float>(data0));
            //if (components == 3)
            //{
            //    H5S.selectHyperslab(dataspace1, H5S.SelectOperator.SET, start, cache_dimensions);
            //    H5D.read<float>(dataset1, datatype, memspace, dataspace1, new H5PropertyListId(H5P.Template.DEFAULT), new H5Array<float>(data1));
            //    H5S.selectHyperslab(dataspace2, H5S.SelectOperator.SET, start, cache_dimensions);
            //    H5D.read<float>(dataset2, datatype, memspace, dataspace2, new H5PropertyListId(H5P.Template.DEFAULT), new H5Array<float>(data2));
            //}
        }

        //private void ReadDataSetsAsync(long[] start)
        //{
        //    H5S.selectHyperslab(dataspace0, H5S.SelectOperator.SET, start, cache_dimensions);
        //    Task read_dataset0 = Task.Factory.StartNew(() =>
        //    {
        //        H5D.read<float>(dataset0, datatype, memspace, dataspace0, new H5PropertyListId(H5P.Template.DEFAULT), new H5Array<float>(data0));
        //    });
        //    reading_tasks[0] = read_dataset0;
        //    if (components == 3)
        //    {
        //        Task read_dataset1 = Task.Factory.StartNew(() =>
        //        {
        //            H5S.selectHyperslab(dataspace1, H5S.SelectOperator.SET, start, cache_dimensions);
        //            H5D.read<float>(dataset1, datatype, memspace, dataspace1, new H5PropertyListId(H5P.Template.DEFAULT), new H5Array<float>(data1));
        //        });
        //        reading_tasks[1] = read_dataset1;
        //        Task read_dataset2 = Task.Factory.StartNew(() =>
        //        {
        //            H5S.selectHyperslab(dataspace2, H5S.SelectOperator.SET, start, cache_dimensions);
        //            H5D.read<float>(dataset2, datatype, memspace, dataspace2, new H5PropertyListId(H5P.Template.DEFAULT), new H5Array<float>(data2));
        //        });
        //        reading_tasks[2] = read_dataset2;
        //    }

        //    Task.WaitAll(reading_tasks);
        //}

        //public void GetAtom(long[] start, long[] count, byte[] u, byte[] v, byte[] w)
        //{
        //    H5DataSpaceId atom_memspace = H5S.create_simple(3, new long[] { atomSize, atomSize, atomSize });
        //    H5S.selectHyperslab(dataspace[0], H5S.SelectOperator.SET, start, count);
        //    float[] buffer_u = new float[atomSize * atomSize * atomSize];
        //    H5D.read<float>(dataset[0], datatype, atom_memspace, dataspace[0], new H5PropertyListId(H5P.Template.DEFAULT), new H5Array<float>(buffer_u));
        //    H5S.selectHyperslab(dataspace[1], H5S.SelectOperator.SET, start, cache_dimensions);
        //    float[] buffer_v = new float[atomSize * atomSize * atomSize];
        //    H5D.read<float>(dataset[1], datatype, atom_memspace, dataspace[1], new H5PropertyListId(H5P.Template.DEFAULT), new H5Array<float>(buffer_v));
        //    H5S.selectHyperslab(dataspace[2], H5S.SelectOperator.SET, start, cache_dimensions);
        //    float[] buffer_w = new float[atomSize * atomSize * atomSize];
        //    H5D.read<float>(dataset[2], datatype, atom_memspace, dataspace[2], new H5PropertyListId(H5P.Template.DEFAULT), new H5Array<float>(buffer_w));
        //    Buffer.BlockCopy(buffer_u, 0, u, 0, atomSize * atomSize * atomSize * sizeof(float));
        //    Buffer.BlockCopy(buffer_v, 0, v, 0, atomSize * atomSize * atomSize * sizeof(float));
        //    Buffer.BlockCopy(buffer_w, 0, w, 0, atomSize * atomSize * atomSize * sizeof(float));
        //}

        public void GetAtom(int[] start, int[] count, byte[] destinationArray, int destinationOffset)
        {
            int data_offset = destinationOffset, srcOffset, zOffset, yOffset;
            for (int z = start[0]; z < start[0] + count[0]; z++)
            {
                zOffset = z * (int)cache_dimensions[2] * (int)cache_dimensions[1];
                for (int y = start[1]; y < start[1] + count[1]; y++)
                {
                    yOffset = y * (int)cache_dimensions[2];
                    for (int x = start[2]; x < start[2] + count[2]; x++)
                    {
                        srcOffset = (zOffset + yOffset + x) * sizeof(float);
                        Buffer.BlockCopy(data0, srcOffset, destinationArray, data_offset, sizeof(float));
                        data_offset += sizeof(float);
                        if (components == 3)
                        {
                            Buffer.BlockCopy(data1, srcOffset, destinationArray, data_offset, sizeof(float));
                            data_offset += sizeof(float);
                            Buffer.BlockCopy(data2, srcOffset, destinationArray, data_offset, sizeof(float));
                            data_offset += sizeof(float);
                        }
                    }
                }
            }
        }

        public void VerifyAtom(long[] start, byte[] atom, int dataOffset)
        {
            long[] atom_dimensions = new long[] { atomSize, atomSize, atomSize };
            H5DataSpaceId dspace0 = H5D.getSpace(dataset[0]);
            H5DataSpaceId dspace1 = H5D.getSpace(dataset[1]);
            H5DataSpaceId dspace2 = H5D.getSpace(dataset[2]);
            H5DataSpaceId atom_memspace = H5S.create_simple(3, atom_dimensions);


            H5S.selectHyperslab(dspace0, H5S.SelectOperator.SET, start, atom_dimensions);
            float[] buffer_u = new float[atomSize * atomSize * atomSize];
            H5D.read<float>(dataset[0], datatype, atom_memspace, dspace0, new H5PropertyListId(H5P.Template.DEFAULT), new H5Array<float>(buffer_u));
            H5S.selectHyperslab(dspace1, H5S.SelectOperator.SET, start, atom_dimensions);
            float[] buffer_v = new float[atomSize * atomSize * atomSize];
            H5D.read<float>(dataset[1], datatype, atom_memspace, dspace1, new H5PropertyListId(H5P.Template.DEFAULT), new H5Array<float>(buffer_v));
            H5S.selectHyperslab(dspace2, H5S.SelectOperator.SET, start, atom_dimensions);
            float[] buffer_w = new float[atomSize * atomSize * atomSize];
            H5D.read<float>(dataset[2], datatype, atom_memspace, dspace2, new H5PropertyListId(H5P.Template.DEFAULT), new H5Array<float>(buffer_w));


            float epsilon = 0.000001f;
            int offset = 0, buffer_offset = 0;
            float u, v, w;
            for (int i = 0; i < atomSize; i++)
                for (int j = 0; j < atomSize; j++)
                    for (int k = 0; k < atomSize; k++)
                    {
                        u = BitConverter.ToSingle(atom, dataOffset + offset * sizeof(float));
                        v = BitConverter.ToSingle(atom, dataOffset + (offset + 1) * sizeof(float));
                        w = BitConverter.ToSingle(atom, dataOffset + (offset + 2) * sizeof(float));
                        if (!nearlyEqual(u, buffer_u[buffer_offset], epsilon) ||
                            !nearlyEqual(v, buffer_v[buffer_offset], epsilon) ||
                            !nearlyEqual(w, buffer_w[buffer_offset], epsilon))
                        {
                            Console.WriteLine("Error! Values are not equal: u = {0}, v = {1}, w = {2} in atom; u = {3}, v = {4}, w = {5} in file at location {6}, {7}, {8}",
                                u, v, w, buffer_u[buffer_offset], buffer_v[buffer_offset], buffer_w[buffer_offset], start[0] + i, start[1] + j, start[2] + k);
                            Console.ReadLine();
                        }
                        //Console.WriteLine("[{3}, {4}, {5}]: u = {0}, v = {1}, w = {2}", u, v, w, start[0] + i, start[1] + j, start[2] + k);
                        offset += components;
                        buffer_offset++;
                    }

            H5S.close(dspace0);
            H5S.close(dspace1);
            H5S.close(dspace2);
            H5S.close(atom_memspace);
        }

        public static bool nearlyEqual(float a, float b, float epsilon)
        {
            return Math.Abs(a - b) < epsilon;
        }

        private string GetFileName(int time)
        {
            return String.Format(@"{0}fields_tdb_step_{1:000000000}.h5", data_dir, time);
        }
    }
}
