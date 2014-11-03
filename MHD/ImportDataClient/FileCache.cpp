#include "FileCache.h"

FileCache::FileCache()
{
	this->zbase[0] = -1;
	this->zbase[1] = -1;
	this->zbase[2] = -1;
	this->timeStep = -1;
	this->data_dir = "";
	this->prefix = "V";
	this->resolution = 1024;
	this->X_resolution = 1024;
	this->Y_resolution = 1024;
	this->Z_resolution = 512;
	this->z_width = 8;
	this->z_slices = resolution / z_width;
	this->components = 3;
	this->cacheSize = 0;
	this->data = NULL;
	this->file_size = 0;
	this->file_data = NULL;
	this->ID = -1;
	this->io_time = 0;
}

FileCache::FileCache(string data_dir, string prefix, int resolution, int z_width, int components, int ID)
{
	this->zbase[0] = -1;
	this->zbase[1] = -1;
	this->zbase[2] = -1;
	this->timeStep = -1;
	this->data_dir = data_dir;
	this->prefix = prefix;
	this->resolution = resolution;
	this->X_resolution = -1;
	this->Y_resolution = -1;
	this->Z_resolution = -1;
	this->z_slices = resolution / z_width;
	this->z_width = z_width;
	this->components = components;
	this->ID = ID;
	this->io_time = 0;
	
	this->cacheSize = 0;
	this->data = NULL;

	this->file_size = components * z_width * resolution * resolution * sizeof(float);
	try
	{
		this->file_data = new unsigned char[file_size];
	}
	catch (bad_alloc&)
	{
		printf("Could not allocate memory for a single file's data!\n");
		exit(1);
	}
}

FileCache::FileCache(const FileCache& copy)
{
	CopyFileCache(copy);
}

FileCache& FileCache::operator = (const FileCache& other)
{
	if (this != &other)
	{
		DeleteData();
		if (file_data)
			delete [] file_data;
	
		CopyFileCache(other);

	}
	return *this;
}

void FileCache::CopyFileCache(const FileCache& copy)
{
	this->zbase[0] = copy.zbase[0];
	this->zbase[1] = copy.zbase[1];
	this->zbase[2] = copy.zbase[2];
	this->timeStep = copy.timeStep;
	this->data_dir = copy.data_dir;
	this->prefix = copy.prefix;
	this->resolution = copy.resolution;
	this->X_resolution = copy.X_resolution;
	this->Y_resolution = copy.Y_resolution;
	this->Z_resolution = copy.Z_resolution;
	this->z_width = copy.z_width;
	this->z_slices = copy.z_slices;
	this->components = copy.components;
	this->file_size = copy.file_size;
	this->cacheSize = copy.cacheSize;
	this->ID = copy.ID;
	this->io_time = copy.io_time;

	if (copy.data)
	{		
		try
		{
			this->data = new unsigned char*[components];
		}
		catch (bad_alloc&)
		{
			printf("Could not allocate memory for the data cube to be cached!\n");
			exit(1);
		}
			
		for (int i = 0; i < components; i++)
		{
			try
			{
				this->data[i] = new unsigned char[cacheSize];
			}
			catch (bad_alloc&)
			{
				printf("Could not allocate memory for the data cube to be cached!\n");
				exit(1);
			}
			memcpy(this->data[i], copy.data[i], copy.cacheSize);
		}

	}
	if (copy.file_data)
	{	
		try
		{
			this->file_data = new unsigned char[file_size];
		}
		catch (bad_alloc&)
		{
			printf("Could not allocate memory for a single file's data!\n");
			exit(1);
		}
		memcpy(this->file_data, copy.file_data, copy.file_size);
	}
}

FileCache::~FileCache(void)
{
	DeleteData();
	
	if (file_data)
		delete [] file_data;
}

bool FileCache::GetData(int timestep, int base[], int& XResolution, int& YResolution, int& ZResolution, int cube_resolution, int edge_width)
{
	//check whether the requested data cube exists in the cache
	if (this->timeStep != -1 && this->timeStep == timestep)
	  if (this->zbase[0] != -1 && base[0] >= this->zbase[0] && base[0] < this->zbase[0] + this->Z_resolution)
	    if (this->zbase[1] != -1 && base[1] >= this->zbase[1] && base[1] < this->zbase[1] + this->Y_resolution)
	      if(this->zbase[2] != -1 && base[2] >= this->zbase[2] && base[2] < this->zbase[2] + this->X_resolution)
		return 1;

	//the requested data cube does not exist in cache at this point
	//the files needed to create the cache will be read-in

	if (data == NULL)
	{
		//if the data forms a cube it can be read-in in 2 runs since the files are split along z
		if (XResolution == YResolution && YResolution == ZResolution && ZResolution >= 2 * cube_resolution)
			ZResolution = ZResolution / 2;

		long long allocSize = (XResolution + 2 * edge_width) * (YResolution + 2 * edge_width) * (ZResolution + 2 * edge_width) * sizeof(float);
		while (allocSize > SIZE_MAX)
		{
                        if (XResolution < 2 * cube_resolution || YResolution < 2 * cube_resolution || ZResolution < 2 * cube_resolution){
                                printf("Resolution cannot be less than the cube resolution!\n");
                                return 0;
                        }

			if (ZResolution >= YResolution && ZResolution >= XResolution)
			  ZResolution /= 2;
			else if (YResolution >= ZResolution && YResolution >= XResolution)
			  YResolution /= 2;
			else if (XResolution > YResolution && XResolution > ZResolution)
			  XResolution /= 2;
			allocSize = (XResolution + 2 * edge_width) * (YResolution + 2 * edge_width) * (ZResolution + 2 * edge_width) * sizeof(float);
		}

		try
		{
			data = new unsigned char*[components];
		}
		catch (bad_alloc&)
		{
			data = NULL;
			return 0;
		}

		bool flag = true;
		while (flag)
		{
			cacheSize = (XResolution + 2 * edge_width) * (YResolution + 2 * edge_width) * (ZResolution + 2 * edge_width) * sizeof(float);
			for (int i = 0; i < components; i++)
			{
				try
				{
					data[i] = new unsigned char[cacheSize];
					flag = false;
				}
				catch (bad_alloc&)
				{
				        printf("Could not allocate memory for the FileCache reducing the cache size!\n");
					for (int j = 0; j < i; j++)
					{
						delete [] data[j];
						data[j] = NULL;
					}

					//If memory could not be allocated, we attempt to reduce the XResolution, YResolution, and ZResolution
					//NOTE: In case they were equal to begin with the ZResolution was reduced by half
					
					if (XResolution < 2 * cube_resolution || YResolution < 2 * cube_resolution || ZResolution < 2 * cube_resolution){
					  delete [] data;
					  data = NULL;
					  return 0;
					}

					if (ZResolution >= YResolution && ZResolution >= XResolution)
					  ZResolution /= 2;
					else if (YResolution >= ZResolution && YResolution >= XResolution)
					  YResolution /= 2;
					else if (XResolution > YResolution && XResolution > ZResolution)
					  XResolution /= 2;

					flag = true;
					break;
				}
			}
		}
		
		this->X_resolution = XResolution;
		this->Y_resolution = YResolution;
		this->Z_resolution = ZResolution;
	}
	else
	{
		//The data array has already been allocated
		//Set the given resolutions for X, Y, Z to the ones that are currently used by the FileCache
		XResolution = this->X_resolution;
		YResolution = this->Y_resolution;
		ZResolution = this->Z_resolution;
	}

	//coordinate of the lower left corner of the cube cached in memory
	this->timeStep = timestep;
	this->zbase[0] = base[0];
	this->zbase[1] = base[1];
	this->zbase[2] = base[2];
	
	ReadFiles(timestep, edge_width);

	return 1;
}

void FileCache::DeleteData()
{
	if (data)
	{
		for(int i=components-1; i>=0; i--)
			if (data[i])
				delete [] data[i];
		delete [] data;
	}
}

void FileCache::ReadFiles(int timestep, int edge_width)
{
	int first_slice = zbase[0] / z_width;
	//fileOffset comes into play when we need to start reading somewhere other than the beginning of the file
	//i.e. when the cache_resolution is not evenly divided across the files (z_width does not evenly divide cache_resolution)
	//or when z_width is bigger than the cache_resolution
	int fileOffset = zbase[0] - first_slice * z_width;

	//if fileOffset is greater than or equal to the edge width
	//the bottom edge is located in the same file as the first slice
	//otherwise the bottom edge is in the file containing the preivous slice
	if (fileOffset < edge_width)
	{
		first_slice -= 1;
		fileOffset = z_width - (edge_width - fileOffset);
	}

	//the total number of bytes that need to be read-in for the completed cube that will be cached
	int bytesToRead = (X_resolution + 2 * edge_width) * (Y_resolution + 2 * edge_width) * (Z_resolution + 2 * edge_width) * sizeof(float);
	
	//we need to determine how many consecutive files need to be read 
    //in order to create the complete cube that will be cached
    int slice_count = 1;
    //this flag will be raised if there are more than 1 files to be read
    bool flag = false;
    int availableData = z_width - fileOffset;
    while (availableData < Z_resolution + 2 * edge_width)
    {
		//there are more than 1 files to be read 
		//we can only read resolution * resolution * z_width bytes at a time
		//we also need to take into consideration fileOffset for the first file
		bytesToRead = (X_resolution + 2 * edge_width) * (Y_resolution + 2 * edge_width) * (z_width - fileOffset) * sizeof(float);
        flag = true;
        slice_count++;
        availableData += z_width;
    }

    int dataOffset = 0;

	if (!file_data)
	{
		file_size = components * z_width * resolution * resolution * sizeof(float);
		try
		{
			file_data = new unsigned char[file_size];
		}
		catch (bad_alloc&)
		{
			printf("Could not allocate memory for a single file's data!\n");
			exit(1);
		}
	}

	for (int sliceCount = 0; sliceCount < slice_count; sliceCount++)
	{
		//int slice = (first_slice + sliceCount) % z_slices;
		int slice = first_slice + sliceCount;
		if (slice > z_slices)
		{
			printf("Error: Slice requested is greater than the last available slice! (check cach_resolution and zbase)...\n");
			exit(1);
		}

		ReadData(timestep, slice);
	    
		//in some cases we do not need to read the entire last file
		//we only need to read as much as it is left to complete the cube to be cached
		//during each iteration, we have read X_resolution * Y_resolution * z_width bytes
		//possibly with the exception of the first iteration if fileOffset was greater than 0
		//therefore what is left to read is as below
		if (flag && (sliceCount == slice_count - 1))
		{
			bytesToRead = (X_resolution + 2 * edge_width) * (Y_resolution + 2 * edge_width) * 
				((Z_resolution + 2 * edge_width) - z_width * sliceCount + fileOffset) * sizeof(float);
		}

		//if fileOffset is greater than 0 we need to make sure we are reading from the appropriate place in the first file
		if ((sliceCount == 0) && (fileOffset > 0))
		{
			if (edge_width > 0)
				CopyData(bytesToRead, dataOffset, fileOffset, edge_width);
			else
				CopyData(bytesToRead, dataOffset, fileOffset);
		}
		else
		{
			if (edge_width > 0)
				CopyData(bytesToRead, dataOffset, 0, edge_width);
			else
				CopyData(bytesToRead, dataOffset, 0);
		}

		dataOffset += bytesToRead;

		//after the first file has been read-in we need to make sure that we read-in the rest in their entirety
		bytesToRead = (X_resolution + 2 * edge_width) * (Y_resolution + 2 * edge_width) * z_width * sizeof(float);
	}
}

/// <summary>
/// Read data from a single file for all components
/// </summary>
void FileCache::ReadData(int timestep, int slice)
{
    if (slice < 0)
        slice = z_slices - 1;
    if (slice == z_slices)
        slice = 0;

    double start, end;
    FILE *fs;
    char* filename = GetFileName(timestep, slice);
	int filesize = components * z_width * resolution * resolution * sizeof(float);
	fs = fopen(filename, "r");
    if (fs)
    {
      //printf("Reading %s...\n", filename);
	
#ifndef WIN32
	start = MPI_Wtime();
#endif
	int bytesRead = fread(file_data, 1, filesize, fs);
#ifndef WIN32
	end = MPI_Wtime();
	io_time += end - start;
	//printf("%d: I/O time = %f\n", ID, io_time);
#endif
	
	if (bytesRead < filesize)
        {
	  if (ferror(fs) != 0)
	    perror("File Error!\n");
	  //else if (feof(fs))
	    //printf("Read %i bytes (entire file)...\n", filesize);
        }
	//else 
	  //printf("Read %i bytes...\n", bytesRead);
        
        fclose(fs);
    }
    else
    {
		printf("Error: File not found: %s!\n", filename);
		exit(1);
    }
}

/// <summary>
/// Copy data from the cached contents of a single file into the data cube to be cached for all components
///	This function can be used in the case that there is no replicated edge
/// </summary>
void FileCache::CopyData(int bytesToCopy, int dataOffset, int fileOffset)
{
	//the number of bytes sotred in the file for one of the components
	int component_bytes = z_width * resolution * resolution * sizeof(float);

    for (int i = 0; i < components; i++)
    {
		int destinationIndex = dataOffset;
        int sourceIndex_x = zbase[2];
        int sourceIndex_y = zbase[1];
        int sourceIndex_z = fileOffset;
        int count = 0;
        int bytesCopied = 0;

        while (bytesCopied < bytesToCopy)
        {
            // Assign bytes for the cached data cube along x for the appropriate component.
			memcpy(data[i] + destinationIndex, 
				file_data + i * component_bytes + (sourceIndex_x + resolution * sourceIndex_y + resolution * resolution * sourceIndex_z) * sizeof(float), 
				X_resolution * sizeof(float));
            destinationIndex += X_resolution * sizeof(float);
            // Adjust the array index, so that we start from the beginning for the next row (along y).
            sourceIndex_y += 1;
            count++;
            // Once we have assigned enough bytes for an entire sheet, move to the next (along z).
            // We also need to adjust the y-index to start from the beginning.
            if (count == Y_resolution)
            {
                sourceIndex_z++;
                sourceIndex_y  = zbase[1];
                count = 0;
            }
            bytesCopied += X_resolution * sizeof(float);
        }
    }
}

/// <summary>
/// Copy data from the cached contents of a single file into the data cube to be cached for all components
///	This function can be used in the case that there is a replicated edge
/// </summary>
void FileCache::CopyData(int bytesToCopy, int dataOffset, int fileOffset, int edge)
{
	//the number of bytes stored in the file for one of the components
	int component_bytes = z_width * resolution * resolution * sizeof(float);

    for (int i = 0; i < components; i++)
    {
		int destinationIndex = dataOffset;
        int sourceIndex_x = (zbase[2] - edge + resolution) % resolution;
        int sourceIndex_y = (zbase[1] - edge + resolution) % resolution;
        int sourceIndex_z = fileOffset;
        int count = 0;
        int bytesCopied = 0;

        while (bytesCopied < bytesToCopy)
        {
            // Assign bytes for the left replicated edge along x for the appropriate component.
			memcpy(data[i] + destinationIndex, 
				file_data + i * component_bytes + (sourceIndex_x + resolution * sourceIndex_y + resolution * resolution * sourceIndex_z) * sizeof(float), 
				edge * sizeof(float));
            // Adjust the array index by the length of the edge.
            sourceIndex_x += edge;
            sourceIndex_x = sourceIndex_x % resolution;
            destinationIndex += edge * sizeof(float);
            // Assign bytes for the cached data cube along x for the appropriate component.
			memcpy(data[i] + destinationIndex, 
				file_data + i * component_bytes + (sourceIndex_x + resolution * sourceIndex_y + resolution * resolution * sourceIndex_z) * sizeof(float), 
				X_resolution * sizeof(float));
            // Adjust the array index by the resolution of the cached data cube.
            sourceIndex_x += X_resolution;
            sourceIndex_x = sourceIndex_x % resolution;
            destinationIndex += X_resolution * sizeof(float);
            // Assign bytes for the right replicated edge along x for the appropriate component.
			memcpy(data[i] + destinationIndex, 
				file_data + i * component_bytes + (sourceIndex_x + resolution * sourceIndex_y + resolution * resolution * sourceIndex_z) * sizeof(float), 
				edge * sizeof(float));
            // Adjust the array index, so that we skip over the rest and start from the beginning for the next row (along y).
            sourceIndex_x += resolution - X_resolution - edge;
            sourceIndex_x = sourceIndex_x % resolution;
            destinationIndex += edge * sizeof(float);
            sourceIndex_y += 1;
            sourceIndex_y = sourceIndex_y % resolution;
            count++;
            // Once we have assigned enough bytes for an entire sheet, move to the next (along z).
            // We also need to adjust the y-index to skip over the data outside of the cube and start from the beginning.
            if (count == Y_resolution + 2 * edge)
            {
                sourceIndex_z++;
                sourceIndex_y += resolution - Y_resolution - 2 * edge;
                sourceIndex_y = sourceIndex_y % resolution;
                count = 0;
            }
            bytesCopied += (Y_resolution + 2 * edge) * sizeof(float);
        }
    }
}

char* FileCache::GetFileName(int time, int slice)
{
    if (z_slices > 1)
    {
		char *ret = new char[100];
		sprintf(ret, "%s%s%05i.%03i", data_dir.c_str(), prefix.c_str(), time, slice);
        return ret;
    }
    else
    {
		char *ret = new char[100];
		sprintf(ret, "%s%s%05i.000", data_dir.c_str(), prefix.c_str(), time);
        return ret;
    }
}

int FileCache::XResolution()
{
	return X_resolution;
}

int FileCache::YResolution()
{
	return Y_resolution;
}

int FileCache::ZResolution()
{
	return Z_resolution;
}

int FileCache::GetZBaseX()
{
	return zbase[2];
}

int FileCache::GetZBaseY()
{
	return zbase[1];
}

int FileCache::GetZBaseZ()
{
	return zbase[0];
}
