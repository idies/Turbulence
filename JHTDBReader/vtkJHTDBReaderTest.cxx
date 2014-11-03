#include "vtkJHTDBReader.h"

#include <vtkActor2D.h>
#include <vtkImageData.h>
#include <vtkImageMapper.h>
#include <vtkInteractorStyleImage.h>
#include <vtkSmartPointer.h>
#include <vtkPolyDataMapper.h>
#include <vtkActor.h>
#include <vtkSimplePointsReader.h>
#include <vtkRenderWindow.h>
#include <vtkRenderWindowInteractor.h>
#include <vtkRenderer.h>
#include <vtkProperty.h>
#include <vtkXMLImageDataWriter.h>
#include <vtkXMLImageDataReader.h>
#include <vtkImageDataGeometryFilter.h>
#include <vtkImageClip.h>

#using <System.dll>
#using <System.Data.dll>

using namespace System;
using namespace System::Collections::Generic;
using namespace System::Text;
using namespace System::Data;
using namespace System::Data::SqlClient;
using namespace System::Collections;

void PrintImage(vtkImageData* image);

int main(int argc, char* argv[])
{
	int timestep = 0;
	int extent[6];
	extent[0] = 0;
	extent[1] = 15;
	extent[2] = 0;
	extent[3] = 15;
	extent[4] = 0;
	extent[5] = 15;

	// Read the data
	vtkSmartPointer<vtkJHTDBReader> reader =
		vtkSmartPointer<vtkJHTDBReader>::New();
	reader->SetTimestep(timestep);
	reader->SetWholeExtent(extent[0], extent[1], extent[2], extent[3], extent[4], extent[5]);
	reader->SetField(0);
	reader->Update();
	
	extent[0] = 0;
	extent[1] = 23;
	extent[2] = 0;
	extent[3] = 23;
	extent[4] = 0;
	extent[5] = 15;
	reader->SetWholeExtent(extent[0], extent[1], extent[2], extent[3], extent[4], extent[5]);

	vtkImageData* output = reader->GetOutput();
	
	//PrintImage(output);
 
	vtkSmartPointer<vtkXMLImageDataWriter> writer =
	vtkSmartPointer<vtkXMLImageDataWriter>::New();
	writer->SetFileName("C:\\Documents and Settings\\Kalin Kanov\\My Documents\\Research\\JHTDBReader\\test.vti");
	writer->SetInputData(output);
	//writer->SetInputConnection(reader->GetOutputPort());
	writer->Write();

	//// Read the file (to test that it was written correctly)
	//vtkSmartPointer<vtkXMLImageDataReader> image_reader =
	//vtkSmartPointer<vtkXMLImageDataReader>::New();
	//image_reader->SetFileName("C:\\Users\\kalin\\Documents\\JHTDBReader\\build\\Debug\\test.vti");
	//image_reader->Update();
 
	// Convert the image to a polydata
	vtkSmartPointer<vtkImageDataGeometryFilter> imageDataGeometryFilter =
	vtkSmartPointer<vtkImageDataGeometryFilter>::New();
	imageDataGeometryFilter->SetInputConnection(reader->GetOutputPort());
	imageDataGeometryFilter->Update();
 
	vtkSmartPointer<vtkPolyDataMapper> mapper =
	vtkSmartPointer<vtkPolyDataMapper>::New();
	mapper->SetInputConnection(imageDataGeometryFilter->GetOutputPort());
 
	vtkSmartPointer<vtkActor> actor =
	vtkSmartPointer<vtkActor>::New();
	actor->SetMapper(mapper);
	actor->GetProperty()->SetPointSize(3);
 
	// Setup rendering
	vtkSmartPointer<vtkRenderer> renderer =
	vtkSmartPointer<vtkRenderer>::New();
	renderer->AddActor(actor);
	renderer->SetBackground(1,1,1);
	renderer->ResetCamera();
 
	vtkSmartPointer<vtkRenderWindow> renderWindow =
	vtkSmartPointer<vtkRenderWindow>::New();
	renderWindow->AddRenderer(renderer);
 
	vtkSmartPointer<vtkRenderWindowInteractor> renderWindowInteractor =
	vtkSmartPointer<vtkRenderWindowInteractor>::New();
 
	renderWindowInteractor->SetRenderWindow(renderWindow);
	renderWindowInteractor->Initialize();
	renderWindowInteractor->Start();

  return EXIT_SUCCESS;
}

void PrintImage(vtkImageData* image)
{
	int* dims = image->GetDimensions();
 
	for (int z = 0; z < dims[2]; z++)
	{ 
		for (int y = 0; y < dims[1]; y++)
		{
			for (int x = 0; x < dims[0]; x++)
			{
				double v = image->GetScalarComponentAsDouble(x, y, z, 0);
				std::cout << v << " ";
			}
			std::cout << std::endl;
		}
	} 
}
