#pragma once

#include <stdio.h>
#include <stdlib.h>
#include <string>
#include <memory.h>
#ifndef WIN32
	#include <stdint.h>
#endif
#include <mpi.h>

using namespace std;

//#ifndef SIZE_MAX
//#define SIZE_MAX ((size_t)(-1))
//#endif
#define SIZE_MAX 1073741824

class FileCache
{
public:
	FileCache();
	FileCache(string data_dir, string prefix, int resolution, int z_width, int components, int ID);
	FileCache(const FileCache& copy);
	~FileCache(void);

	FileCache & operator = (const FileCache& other);

	bool GetData(int timestep, int base[3], int& XResolution, int& YResolution, int& ZResolution, int cube_resolution, int edge_width);
	void DeleteData();
	int XResolution();
	int YResolution();
	int ZResolution();
	int GetZBaseX();
	int GetZBaseY();
	int GetZBaseZ();

	unsigned char **data;
	size_t cacheSize;

	double io_time;

private:

	void CopyFileCache(const FileCache& copy);

	//void ReadFiles(int timestep);
	void ReadFiles(int timestep, int edge_width);
	void ReadData(int timestep, int slice);
	//void ReadData(int timestep, int slice, int bytesToRead, int dataOffset, int fileOffset);
	void CopyData(int bytesToCopy, int dataOffset, int fileOffset);
	void CopyData(int bytesToCopy, int dataOffset, int fileOffset, int edge_width);
	char* GetFileName(int time, int slice);

	string data_dir;
	string prefix;
	int resolution;
	int z_slices;
	int z_width; 
	int components;
	int X_resolution;
	int Y_resolution;
	int Z_resolution;
	int timeStep;
	int zbase[3];			//Coordinates ([z,y,x]) of the lower left corner of the cached cube 
	
	size_t file_size;
	unsigned char *file_data;

	int ID;
};
