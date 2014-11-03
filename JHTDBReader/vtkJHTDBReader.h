#ifndef __vtkJHTDBReader_h
#define __vtkJHTDBReader_h

#include "vtkIOCoreModule.h" // For export macro
#include "vtkImageAlgorithm.h"

#include <vcclr.h>

using namespace System;


#define VTK_JHTDB_VELOCITY   0
#define VTK_JHTDB_PRESSURE   1

class vtkJHTDBReader : public vtkImageAlgorithm
{
public:
	static vtkJHTDBReader* New();
    vtkTypeMacro(vtkJHTDBReader,vtkImageAlgorithm);
    void PrintSelf(ostream& os, vtkIndent indent);
	
	void SetTimestep(const int timestep);
	void SetWholeExtent(int xMinx, int xMax, int yMin, int yMax,
						int zMin, int zMax);
	void SetField(int field);

protected:
	vtkJHTDBReader();
	~vtkJHTDBReader();

	int RequestInformation(
		vtkInformation*,
		vtkInformationVector**,
		vtkInformationVector*);

	int RequestData(
		vtkInformation*,
		vtkInformationVector**,
		vtkInformationVector*);

private:
	int atomSize;
	int dataPointSize;
	int SqlArrayHeaderSize;
	int field;
	gcroot<String^> serverName;
	gcroot<String^> dbName;
	gcroot<String^> codeDbName;
	gcroot<String^> dataset;
	int timeStep;
	int WholeExtent[6];
	int components;
	int CachedTimeStep;
	int CachedExtent[6];
	gcroot<array<unsigned char>^> rawdata;

	vtkJHTDBReader(const vtkJHTDBReader&);
	void operator=(const vtkJHTDBReader&);

	void InitializeManagedObjects();
	void ReadRawData(const int timestep, const int subext[6]);
};

#endif
