import vtk
from vtk.util import numpy_support
import pyodbc
import numpy as np

conn = pyodbc.connect('DSN=turbinfo;UID=turbquery;PWD=aa2465ways2k', autocommit=True)
cursor = conn.cursor()

ts = 0
te = 1
xs = 600
xe = 15
ys = 600
ye = 15
zs = 600
ze = 15

cursor.execute("{CALL turbdev.dbo.GetAnyCutout(?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?)}",'isotropic1024coarse','u',ts,xs,ys,zs,1,1,1,1,te,xe,ye,ze,1,1)
row = cursor.fetchone()
raw = row[0]
data = np.frombuffer(raw, dtype='float32')
vtkdata = numpy_support.numpy_to_vtk(data, deep=True, array_type=vtk.VTK_FLOAT)
vtkdata.SetNumberOfComponents(3)
vtkdata.SetName("Velocity")
image = vtk.vtkImageData()
image.SetExtent(xs, xs+xe-1, ys, ys+ye-1, zs, zs+ze-1)
image.GetPointData().SetVectors(vtkdata)


q = vtk.vtkGradientFilter()
q.SetInputData(image)
q.SetInputScalars(image.FIELD_ASSOCIATION_POINTS,"Velocity")
q.ComputeQCriterionOn()
q.Update()
newimage = vtk.vtkImageData()
newimage.GetPointData().SetScalars(q.GetOutput().GetPointData().GetVectors("Q-criterion"))
mag = vtk.vtkImageMagnitude()
mag.SetInputData(newimage)
mag.Update()

writer = vtk.vtkXMLImageDataWriter()
writer.SetInputData(image)
writer.SetFileName('test.vti')
writer.Write()


import tempfile
import h5py

tmpfile = tempfile.NamedTemporaryFile()
fh = h5py.File(tmpfile.name, driver='core', backing_store=True)
dset = fh.create_dataset("iso", (xe-xs,ye-ys,ze-zs,3),dtype='f')
data = data.reshape(xe-xs,ye-ys,ze-zs,3)
dset = data

