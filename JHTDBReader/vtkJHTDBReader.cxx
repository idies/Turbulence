#include "vtkJHTDBReader.h"
#include <string.h>
#include <vtkDataObject.h>
#include <vtkFloatArray.h>
#include <vtkImageData.h>
#include <vtkInformation.h>
#include <vtkInformationVector.h>
#include <vtkPointData.h>
#include <vtkSmartPointer.h>
#include <vtkStreamingDemandDrivenPipeline.h>

#using <System.dll>
#using <System.Data.dll>

using namespace System::Collections::Generic;
using namespace System::Text;
using namespace System::Data;
using namespace System::Data::SqlClient;
using namespace System::Collections;
using namespace System::Runtime::InteropServices;

vtkJHTDBReader* vtkJHTDBReader::New()
{
	return new vtkJHTDBReader();
}

vtkJHTDBReader::vtkJHTDBReader()
{
	this->dataPointSize = sizeof(float);
	this->atomSize = 8;
	this->SqlArrayHeaderSize = 24;
	this->components = 3;
	this->timeStep = 4000;
	this->field = -1;
	this->WholeExtent[0] = -1000;
	this->WholeExtent[1] = -1000;
	this->WholeExtent[2] = -1000;
	this->WholeExtent[3] = -1000;
	this->WholeExtent[4] = -1000;
	this->WholeExtent[5] = -1000;
	this->CachedTimeStep = -1000;
	this->CachedExtent[0] = -1000;
	this->CachedExtent[1] = -1000;
	this->CachedExtent[2] = -1000;
	this->CachedExtent[3] = -1000;
	this->CachedExtent[4] = -1000;
	this->CachedExtent[5] = -1000;
	InitializeManagedObjects();

	this->SetNumberOfInputPorts(0);
}

void vtkJHTDBReader::InitializeManagedObjects()
{
	this->serverName = gcnew String("gwwn1");
	this->dbName = gcnew String("turbdb101");
	this->codeDbName = gcnew String("mhddev");
	//this->dataset = gcnew String("vel");
}

vtkJHTDBReader::~vtkJHTDBReader()
{
}

void vtkJHTDBReader::SetTimestep(const int timestep)
{
	if (this->timeStep != timestep) {
		this->timeStep = timestep;
		this->Modified();
	}
}

void vtkJHTDBReader::SetWholeExtent(int xMin,
	int xMax,
    int yMin,
    int yMax,
    int zMin,
    int zMax)
{
	int modified = 0;

	if (this->WholeExtent[0] != xMin) {
		modified = 1;
		this->WholeExtent[0] = xMin;
    }
	if (this->WholeExtent[1] != xMax) {
		modified = 1;
		this->WholeExtent[1] = xMax;
    }
	if (this->WholeExtent[2] != yMin) {
		modified = 1;
		this->WholeExtent[2] = yMin;
    }
	if (this->WholeExtent[3] != yMax) {
		modified = 1;
		this->WholeExtent[3] = yMax;
    }
	if (this->WholeExtent[4] != zMin) {
		modified = 1;
		this->WholeExtent[4] = zMin;
    } 
	if (this->WholeExtent[5] != zMax) {
		modified = 1;
		this->WholeExtent[5] = zMax;
    } 
	if (modified) {
		this->Modified();
    }
}

void vtkJHTDBReader::SetField(int field)
{
	if (this->field != field) {
		if (field == VTK_JHTDB_VELOCITY) {
			this->dataset = gcnew String("vel");
			this->components = 3;
		}
		if (field == VTK_JHTDB_PRESSURE) {
			this->dataset = gcnew String("pr");
			this->components = 1;
		}
		this->Modified();
	}
}

void vtkJHTDBReader::PrintSelf(ostream& os, vtkIndent indent)
{
	this->Superclass::PrintSelf(os,indent);

	os << indent << "timestep: " 
		<< this->timeStep << "\n";
	os << indent << "Box: " 
		<< WholeExtent[0] << " " << WholeExtent[2] << " " << WholeExtent[4] << " "
		<< WholeExtent[1] + 1 << " " << WholeExtent[3] + 1 << " " << WholeExtent[5] + 1 << " "
		<< "\n";
}

int vtkJHTDBReader::RequestInformation (
	vtkInformation*,
	vtkInformationVector**,
	vtkInformationVector* outputVector)
{
	vtkInformation* outInfo =
		outputVector->GetInformationObject(0);

	double spacing[3] = {1, 1, 1};
	double origin[3] = {0, 0, 0};

	outInfo->Set(
		vtkStreamingDemandDrivenPipeline::
		WHOLE_EXTENT(),
		this->WholeExtent, 6);
	outInfo->Set(vtkDataObject::SPACING(),spacing, 3);
	outInfo->Set(vtkDataObject::ORIGIN(), origin, 3);
	vtkDataObject::SetPointDataActiveScalarInfo(outInfo, VTK_FLOAT, this->components);

	return 1;
}

int vtkJHTDBReader::RequestData(
	vtkInformation*,
	vtkInformationVector**,
	vtkInformationVector* outputVector)
{
	vtkImageData* image =
		vtkImageData::GetData(outputVector);

    vtkInformation *outInfo = outputVector->GetInformationObject(0);
    int subext[6];
    outInfo->Get(vtkStreamingDemandDrivenPipeline::UPDATE_EXTENT(),subext);

	// Have the data been read-in already?
	if (!rawdata
		|| CachedTimeStep != this->timeStep
		|| subext[0] < CachedExtent[0] || subext[0] > CachedExtent[1]
		|| subext[1] < CachedExtent[0] || subext[1] > CachedExtent[1]
		|| subext[2] < CachedExtent[2] || subext[2] > CachedExtent[3]
		|| subext[3] < CachedExtent[2] || subext[3] > CachedExtent[3]
		|| subext[4] < CachedExtent[4] || subext[4] > CachedExtent[5]
		|| subext[5] < CachedExtent[4] || subext[5] > CachedExtent[5]) {
		ReadRawData(timeStep, subext);
	}
	
	int x_width = (subext[1] - subext[0] + 1);
	int y_width = (subext[3] - subext[2] + 1);
	int z_width = (subext[5] - subext[4] + 1);

	int CachedXWidth = CachedExtent[1] - CachedExtent[0] + 1;
	int CachedYWidth = CachedExtent[3] - CachedExtent[2] + 1;
	int CachedZWidth = CachedExtent[5] - CachedExtent[4] + 1;

	image->SetDimensions(x_width, y_width, z_width);
	image->AllocateScalars(VTK_FLOAT, this->components);

	vtkDataArray* scalars =
		image->GetPointData()->GetScalars();

	scalars->SetName("Data");
	float* values = new float[this->components];
	for (vtkIdType z = 0; z < z_width; ++z)
	{
		for (vtkIdType y = 0; y < y_width; ++y)
		{
			for (vtkIdType x = 0; x < x_width; ++x)
			{
				int source_offset = z * CachedYWidth * CachedXWidth + y * CachedXWidth + x;
				int dest_offset = z * y_width * x_width + y * x_width + x;
				for (int c = 0; c < this->components; c++)
				{
					values[c] = BitConverter::ToSingle(rawdata, (source_offset * components + c) * dataPointSize);
				}
				scalars->SetTuple(dest_offset, values);
				//float value = BitConverter::ToSingle(rawdata, source_offset * components * dataPointSize);
				//scalars->SetTuple1(dest_offset, value);
			}
		}
	}
	delete [] values;

	return 1;
}

void vtkJHTDBReader::ReadRawData(const int timeStep, const int subext[6]) {
	String^ cString = String::Format("server={0};database={1};Integrated Security=true;", this->serverName, this->codeDbName);
    SqlConnection ^ sqlcon = gcnew SqlConnection(cString);
    sqlcon->Open();
	if (sqlcon->State == ConnectionState::Open)
		cerr << "JHTDBReader: connected to database" << endl;
	else
		cerr << "JHTDBReader: error connecting to database " << endl;

	String^ queryBox = String::Format("box[{0},{1},{2},{3},{4},{5}]", 
		subext[0], subext[2], subext[4], subext[1] + 1, subext[3] + 1, subext[5] + 1);
	
	SqlCommand ^ command = gcnew SqlCommand();
	command->Connection = sqlcon;
	command->CommandType = CommandType::StoredProcedure;
	command->CommandText = "GetDataCutout";
	command->Parameters->Add(gcnew SqlParameter("@serverName",SqlDbType::VarChar));
	command->Parameters["@serverName"]->Value = this->serverName;
	command->Parameters->Add(gcnew SqlParameter("@dbname",SqlDbType::VarChar));
	command->Parameters["@dbname"]->Value = this->dbName;
	command->Parameters->Add(gcnew SqlParameter("@codedb",SqlDbType::VarChar));
	command->Parameters["@codedb"]->Value = codeDbName;
	command->Parameters->Add(gcnew SqlParameter("@dataset",SqlDbType::VarChar));
	command->Parameters["@dataset"]->Value = this->dataset;
	command->Parameters->Add(gcnew SqlParameter("@blobDim",SqlDbType::Int));
	command->Parameters["@blobDim"]->Value = this->atomSize;
	command->Parameters->Add(gcnew SqlParameter("@timestep",SqlDbType::Int));
	command->Parameters["@timestep"]->Value = timeStep;
	command->Parameters->Add(gcnew SqlParameter("@queryBox",SqlDbType::VarChar));
	command->Parameters["@queryBox"]->Value = queryBox;

	SqlDataReader ^ reader = command->ExecuteReader();

	int x_width = (subext[1] - subext[0] + 1);
	int y_width = (subext[3] - subext[2] + 1);
	int z_width = (subext[5] - subext[4] + 1);
	int data_size = x_width * y_width * z_width * components * dataPointSize;
	this->rawdata = gcnew array<unsigned char>(data_size);

	__int64 bytesRead = 0;
	int bufferIndex = 0;
	while (reader->Read())
	{
		bufferIndex = (int)bytesRead;
		bytesRead += reader->GetBytes(0, 0, this->rawdata, bufferIndex, data_size - bufferIndex);
	}
	reader->Close();

	this->CachedTimeStep = timeStep;
	for (int i = 0; i < sizeof(WholeExtent) / sizeof(int); i++) {
		this->CachedExtent[i] = subext[i];
	}
}

