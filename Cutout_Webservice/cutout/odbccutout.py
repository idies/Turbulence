import vtk
from vtk.util import numpy_support
import pyodbc
import numpy as np
import django
import os
import h5py
import tempfile

class OdbcCutout:
    def __init__(self):
        #initialize odbc
        self.db = None

    def gethdf(self, webargs):
        DBSTRING = os.environ['db_connection_string']
        conn = pyodbc.connect(DBSTRING, autocommit=True)
        cursor = conn.cursor()
        #url = "http://localhost:8000/cutout/getcutout/"+ token + "/" + dataset + "/" + datafield + "/" + ts + "," +te + "/" + xs + "," + xe +"/" + ys + "," + ye +"/" + zs + "," + ze
        w = webargs.split("/")
        ts = int(w[3].split(',')[0])
        te = int(w[3].split(',')[1])
        xs = int(w[4].split(',')[0])
        xe = int(w[4].split(',')[1])
        ys = int(w[5].split(',')[0])
        ye = int(w[5].split(',')[1])
        zs = int(w[6].split(',')[0])
        ze = int(w[6].split(',')[1])
        if ((w[2] == 'vo') or (w[2] == 'qc') or (w[2] == 'cvo') or (w[2] == 'qcc')):
            component = 'u'
        else:
            component = w[2]

        try:
            tmpfile = tempfile.NamedTemporaryFile()
            fh = h5py.File(tmpfile.name, driver='core', block_size=16, backing_store=True)
            contents = fh.create_dataset('_contents', (1,), dtype='int32')
            contents[0] = 1
            dataset = fh.create_dataset('_dataset', (1,), dtype='int32')
            dataset[0] = 4
            size = fh.create_dataset('_size', (4,), dtype='int32')
            size[...] = [te-ts,xe-xs,ye-ys,ze-zs]
            start = fh.create_dataset('_start', (4,), dtype='int32')
            start[...] = [ts,xs, ys, zs]
            fieldlist = list(component)
            for field in fieldlist:
                #Look for step parameters
                if (len(w) > 9):
                    step = True
                    s = w[8].split(",")
                    tstep = s[0]
                    xstep = float(s[1])
                    ystep = float(s[2])
                    zstep = float(s[3])
                    filterwidth = w[9]
		    print("Stepped: %s" %s[1])
		else:
                    print("len is %s" %len(w))
		    xstep = 1
		    ystep = 1
		    zstep = 1
		    tstep = 1
		    filterwidth = 1

                for timestep in range(ts,te, tstep):
                    cursor.execute("{CALL turbdev.dbo.GetAnyCutout(?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?)}",w[1], field, timestep, xs, ys, zs, xstep, ystep, zstep,1,1,xe,ye,ze,filterwidth,1)
                    row = cursor.fetchone()
                    raw = row[0]
                    part=0
                    while(cursor.nextset()):           
                        row = cursor.fetchone()
                        raw = raw + row[0]
                        part = part +1
                        print ("added part %d" % part)
                        print ("Part size is %d" % len(row[0]))
                    print ("Raw size is %d" % len(raw))
                    data = np.frombuffer(raw, dtype=np.float32)
                    dsetname = field + '{0:05d}'.format(timestep*10)
                    print("zsize = %d" % ((ze-zs)/zstep))
                    print("ysize = %d" % ((ye-ys)/ystep))
                    print("xsize = %d" % ((xe-xs)/xstep))

                    dset = fh.create_dataset(dsetname, (ze/zstep,ye/ystep,xe/xstep,3), maxshape=(ze/zstep,ye/ystep,xe/xstep,3),compression='gzip')
		    print ("Data length is: %s" %len(data))
		    data = data.reshape(ze/zstep,ye/ystep,xe/xstep,3)
                    dset[...] = data
        except:
            fh.close()
            tmpfile.close()
            raise
        fh.close()
        tmpfile.seek(0)
        cursor.close()
        return tmpfile
    def getygrid(self):
        DBSTRING = os.environ['db_channel_string']
        conn = pyodbc.connect(DBSTRING, autocommit=True)
        cursor = conn.cursor()
	rows= cursor.execute("SELECT cell_index, value from grid_points_y ORDER BY cell_index").fetchall()
        length = len(rows)
        ygrid = np.zeros((length,1))
	for row in rows:
            ygrid[row.cell_index]=row.value
        conn.close()
	return ygrid

    def numcomponents(self, component):
        #change this to get from DB in the future, using odbc connection
        if (component == 'u'):
            return 3
        elif (component == 'p'):
            return 1
        elif (component == 'b'):
            return 3 #check this
        elif (component == 'a'):
            return 1 #check this
        else:
            return 3

    def componentname(self, component):
        #change this to get from DB in the future, using odbc connection
        if (component == 'u'):
            return "Velocity"
        if (component == 'p'):
            return "Pressure"
        if (component == 'b'):
            return "Magnetic Field"
        if (component == 'a'):
            return "Vector Potential" 

    def expandcutout(self, extent, xmax, ymax, zmax, overlap):
        if (extent[0]-overlap >=0):
            extent[0] = extent[0]-overlap
            extent[3] = extent[3] + overlap
        if (extent[1]-overlap >=0):
            extent[1] = extent[1]-overlap
            extent[4] = extent[4] + overlap
        if (extent[2]-overlap >=0):
            extent[2] = extent[2]-overlap
            extent[5] = extent[5] + overlap

        #Prevent hitting outside the bounds of the database here
        if (xmax < (extent[0]+extent[3]+overlap)):
            extent[3] = extent[3] + overlap
        if (ymax < (extent[1]+extent[4]+overlap)):
            extent[4] = extent[4] + overlap
        if (zmax < (extent[2]+extent[5]+overlap)):
            extent[5] = extent[5] + overlap
        return extent

    def getmaxrange(self, dataset):
        #In the future, these will be pulled from the database, and probably add time range as well
        if ((dataset == "isotropic1024coarse") or (dataset == "isotropic1024fine") or (dataset == "mixing") or (dataset == "mhd1024")):
            ranges = (1024, 1024, 1024)
        elif (dataset == "channel"):
            ranges = (2048, 512, 1536)
        return ranges

    def getvtkimage(self, webargs, timestep):
        #Setup query
        DBSTRING = os.environ['db_connection_string']
        conn = pyodbc.connect(DBSTRING, autocommit=True)
        cursor = conn.cursor()
        #url = "http://localhost:8000/cutout/getcutout/"+ token + "/" + dataset + "/" + datafield + "/" + ts + "," +te + "/" + xs + "," + xe +"/" + ys + "," + ye +"/" + zs + "," + ze
        w = webargs.split("/")
        ts = int(w[3].split(',')[0])
        te = int(w[3].split(',')[1])
        xs = int(w[4].split(',')[0])
        xe = int(w[4].split(',')[1])
        ys = int(w[5].split(',')[0])
        ye = int(w[5].split(',')[1])
        zs = int(w[6].split(',')[0])
        ze = int(w[6].split(',')[1])
        extent = (xs, ys, zs, xe, ye, ze)
        overlap = 2 #Used only on contours--vorticity and Q-criterion
        #Look for step parameters
        if (len(w) > 9):
            step = True;
	    s = w[8].split(",")
            tstep = s[0]
            xstep = float(s[1])
            ystep = float(s[2])
            zstep = float(s[3])
            filterwidth = w[9]
        else:
            step = False;
            xstep = 1
            ystep = 1
            zstep = 1
            filterwidth = 1
        cfieldlist = w[2].split(",")
        firstval = cfieldlist[0]    
        maxrange = self.getmaxrange(w[1])     
        if ((firstval == 'vo') or (firstval == 'qc') or (firstval == 'cvo') or (firstval == 'qcc')):
            component = 'u'
            computation = firstval #We are doing a computation, so we need to know which one.
            #check to see if we have a threshold (only for contours)
            if (len(cfieldlist) > 1):
                threshold = float(cfieldlist[1])
            else:
                threshold = .6
            #New:  We need an expanded cutout if contouring.  Push the cutout out by 2 in all directions (unless at boundary).
            if ((firstval == 'cvo') or (firstval == 'qcc')):
                newextent = self.expandcutout(extent, maxrange[0], maxrange[1], maxrange[2], overlap)
                contour = True                
        else:
            component = w[2] #There could be multiple components, so we will have to loop
            computation = ''
        #Split component into list and add them to the image

        #Check to see if we have a value for vorticity or q contour
        fieldlist = list(component)

        for field in fieldlist:
            print("Field = %s" % field)
            cursor.execute("{CALL turbdev.dbo.GetAnyCutout(?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?)}",w[1], field, timestep, extent[0], extent[1], extent[2], xstep, ystep, zstep, 1,1,extent[3], extent[4], extent[5],filterwidth,1)
            #If data spans across multiple servers, we get multiple sets, so concatenate them.
            row = cursor.fetchone()
            raw = row[0]
            part = 0            
            print ("First part size is %d" % len(row[0]))
            while(cursor.nextset()):           
                row = cursor.fetchone()
                raw = raw + row[0]
                part = part +1
                print ("added part %d" % part)
                print ("Part size is %d" % len(row[0]))
            print ("Raw size is %d" % len(raw))
            data = np.frombuffer(raw, dtype=np.float32)
            conn.close()
            vtkdata = numpy_support.numpy_to_vtk(data, deep=True, array_type=vtk.VTK_FLOAT)
            components = self.numcomponents(field)
            vtkdata.SetNumberOfComponents(components)
            vtkdata.SetName(self.componentname(field))
            image = vtk.vtkImageData()
            if (step):
                xes = int(extent[3])/int(xstep)-1
                yes = int(extent[4])/int(ystep)-1
                zes = int(extent[5])/int(zstep)-1
                image.SetExtent(extent[0], extent[0]+extent[3], extent[1], extent[1]+extent[4], extent[2], extenet[2]+extenet[5])
                print("Step extent=" +str(xes))
                print("xs=" + str(xstep) + " ys = "+ str(ystep) +" zs = " + str(zstep))
            else:
                image.SetExtent(extent[0], extent[0]+extent[3]-1, extent[1], extent[1]+extent[4]-1, extent[2], extent[2]+extent[5]-1)
            image.GetPointData().SetVectors(vtkdata)

            if (step): #Magnify to original size
                image.SetSpacing(xstep,ystep,zstep)

        #Check if we need a rectilinear grid, and set it up if so.
        if (w[1] == 'channel'):
            ygrid = self.getygrid()
            #print("Ygrid: ")
            #print (ygrid)
            rg = vtk.vtkRectilinearGrid()
            #Not sure about contouring channel yet, so we are going back to original variables at this point.
            rg.SetExtent(xs, xs+xe-1, ys, ys+ye-1, zs, zs+ze-1)
            rg.GetPointData().SetVectors(vtkdata)

            xg = np.arange(float(xs),float(xe))
            zg = np.arange(float(zs),float(ze))
            for x in xg:
                    xg[x] = 8*3.141592654/2048*x
            for z in zg:
                    zg[z] = 3*3.141592654/2048*z
            vtkxgrid=numpy_support.numpy_to_vtk(xg, deep=True,
                array_type=vtk.VTK_FLOAT)
            vtkzgrid=numpy_support.numpy_to_vtk(zg, deep=True,
                array_type=vtk.VTK_FLOAT)
            vtkygrid=numpy_support.numpy_to_vtk(ygrid,
                deep=True, array_type=vtk.VTK_FLOAT)
            rg.SetXCoordinates(vtkxgrid)
            rg.SetZCoordinates(vtkzgrid)
            rg.SetYCoordinates(vtkygrid)
            image = rg #we rewrite the image since we may be doing a
                       #computation below
        #See if we are doing a computation
        if (computation == 'vo'):
            vorticity = vtk.vtkCellDerivatives()
            vorticity.SetVectorModeToComputeVorticity()
            vorticity.SetTensorModeToPassTensors()
            vorticity.SetInputData(image)
            print("Computing Vorticity")
            vorticity.Update()
        elif (computation == 'cvo'):
            vorticity = vtk.vtkCellDerivatives()
            vorticity.SetVectorModeToComputeVorticity()
            vorticity.SetTensorModeToPassTensors()
            vorticity.SetInputData(image)
            print("Computing Voricity")
            vorticity.Update()
            mag = vtk.vtkImageMagnitude()
            cp = vtk.vtkCellDataToPointData()
            cp.SetInputData(vorticity.GetOutput())
            print("Computing magnitude")
            cp.Update()
            image.GetPointData().SetScalars(cp.GetOutput().GetPointData().GetVectors())
            mag.SetInputData(image)
            mag.Update()
            c = vtk.vtkContourFilter()
            c.SetValue(0,threshold)
            c.SetInputData(mag.GetOutput())
            print("Computing Contour")
            c.Update()
            #Now we need to clip out the overlap
            box = vtk.vtkBox()    
            #set box to requested size
            box.SetBounds(xs, xs+xe-1, ys, ys+ye-1, zs,zs+ze-1)
            clip = vtk.vtkClipPolyData()       
            clip.SetClipFunction(box)
            clip.GenerateClippedOutputOn()
            clip.SetInputData(c.GetOutput())
            clip.InsideOutOn()
            clip.Update()
            cropdata = clip.GetOutput()
            return cropdata

        elif (computation == 'qcc'):
            q = vtk.vtkGradientFilter()
            q.SetInputData(image)
            q.SetInputScalars(image.FIELD_ASSOCIATION_POINTS,"Velocity")
            q.ComputeQCriterionOn()
            q.Update()
            #newimage = vtk.vtkImageData()
            image.GetPointData().SetScalars(q.GetOutput().GetPointData().GetVectors("Q-criterion"))
            mag = vtk.vtkImageMagnitude()
            mag.SetInputData(image)
            mag.Update()
            c = vtk.vtkContourFilter()
            c.SetValue(0,threshold)
            c.SetInputData(mag.GetOutput())
            c.Update()
            #clip out the overlap here
            box = vtk.vtkBox()    
            #set box to requested size
            box.SetBounds(xs, xs+xe-1, ys, ys+ye-1, zs,zs+ze-1)
            clip = vtk.vtkClipPolyData()       
            clip.SetClipFunction(box)
            clip.GenerateClippedOutputOn()
            clip.SetInputData(c.GetOutput())
            clip.InsideOutOn()
            clip.Update()
            cropdata = clip.GetOutput()
            return cropdata
        else:
            return image

    
