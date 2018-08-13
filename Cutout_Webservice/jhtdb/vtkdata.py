import getdata
import jhtdblib
from django.http import HttpResponse
from jhtdb.models import Datafield
from jhtdb.models import Dataset
from jhtdb.models import Polycache
from getdata import GetData
from django.core.files.temp import NamedTemporaryFile
import vtk
from vtk.util import numpy_support
import copy
import zipfile
import time
import math
import numpy as np
from multiprocessing import Pool
import tempfile
from django.db import connection
import copy_reg
import types

class VTKData:

    def _pickle_method(m):
        if m.im_self is None:
            return getattr, (m.im_class, m.im_func.func_name)
        else:
            return getattr, (m.im_self, m.im_func.func_name)

    copy_reg.pickle(types.MethodType, _pickle_method)

    def expandcutout(self, ci, overlap):
        if (ci.xstart-overlap >=0):
            ci.xstart = ci.xstart-overlap
            ci.xlen = ci.xlen + overlap
        if (ci.ystart-overlap >=0):
            ci.ystart = ci.ystart-overlap
            ci.ylen = ci.ylen + overlap
        if (ci.zstart-overlap >=0):
            ci.zstart = ci.zstart-overlap
            ci.zlen = ci.zlen + overlap
        ds = Dataset.objects.get(dbname_text=ci.dataset)
        if (ds.xend > (ci.xstart+ci.xlen+overlap)):
            ci.xlen = ci.xlen + overlap
        if (ds.yend > (ci.ystart+ci.ylen+overlap)):
            ci.ylen = ci.ylen + overlap
        if (ds.zend > (ci.zstart+ci.zlen+overlap)):
            ci.zlen = ci.zlen + overlap
        #else:
            #print ("zend ztart zlen overlap: ", ds.zend, ci.zstart, ci.zlen, overlap)
        #print ("z is", ci.zlen)
        return ci

    def getvtk(self, ci):
        contour = False
        firstval = ci.datafields.split(',')[0]
        if ((firstval == 'cvo') or (firstval == 'qcc') or (firstval == 'pcvo')): #we may need to return a vtp file
            tmp = NamedTemporaryFile(suffix='.vtp')
            suffix = 'vtp'
            writer = vtk.vtkXMLPolyDataWriter()
            outfile = ci.dataset + '-contour'
            contour = True
        elif (ci.dataset == "channel" or ci.dataset == "transition_bl" or ci.dataset == "channel5200"):
            tmp = NamedTemporaryFile(suffix='.vtr')
            suffix = 'vtr'
            writer = vtk.vtkXMLRectilinearGridWriter()
            outfile = ci.dataset
        else:
            tmp = NamedTemporaryFile(suffix='.vti')
            suffix = 'vti'
            writer = vtk.vtkXMLImageDataWriter()
            outfile = ci.dataset
        writer.SetFileName(tmp.name)
        writer.SetCompressorTypeToZLib()
        writer.SetDataModeToBinary()
        #If a preview, change the writer to write an ascii oldschool vtk format
        if (firstval == 'pcvo'):
            suffix = 'vtk'
            writer = vtk.vtkPolyDataWriter()
            writer.SetFileTypeToASCII()
        #if multiple timesteps, zip the file.
        if (ci.tlen > 1):
            #Write each timestep to file and read it back in.  Seems to be the only way I know how to put all timesteps in one file for now
            #Create a timestep for each file and then send the user a zip file
            ziptmp = NamedTemporaryFile(suffix='.zip')
            z = zipfile.ZipFile(ziptmp.name, 'w') #, 'allowZip64' this should be added to get zipfiles > 2gb.  Should we allow this?
            for timestep in range (ci.tstart,ci.tstart+ci.tlen, ci.tstep ):
                if (contour == True): #If we have a contour, call the cache version.
                    image = self.getcachedcontour(ci, timestep)
                else:
                    image = self.getvtkdata(ci, timestep)
                writer.SetInputData(image)
                writer.SetFileName(tmp.name)
                writer.Write()
                #Now add this file to the zipfile
                z.write(tmp.name, 'cutout' + str(timestep) + '.' + suffix)
                image = None
            z.close()
            #Try to clean up here
            writer = None
            ct = 'application/zip'
            suffix = 'zip'
            response = HttpResponse(ziptmp, content_type=ct)
        else:
            #print("Single Timestep")
            if (contour == True): #If we have a contour, call the cache version.
                image = self.getcachedcontour(ci, ci.tstart)
            else:
                image = self.getvtkdata(ci, ci.tstart)
            writer.SetInputData(image)
            writer.SetFileName(tmp.name)
            writer.EncodeAppendedDataOff()
            writer.Write()
            ct = 'applicaton/' + suffix
            response = HttpResponse(tmp, content_type=ct)
        response['Content-Disposition'] = 'attachment;filename=' +  outfile +'.' + suffix
        return response

    def getcachedcontour(self, ci, timestep):
        #This is only called on qcc or cvo
        #cube size should be 256 or 512 for production, using 16 for testing.

        cubedimension = 256

        fullcubesize  = [math.ceil(float(ci.xlen)/float(cubedimension))*cubedimension, math.ceil(float(ci.ylen)/float(cubedimension))*cubedimension, math.ceil(float(ci.zlen)/float(cubedimension))*cubedimension]
        #print ("Full poly mesh cube size is: ", fullcubesize)
        #The corner of the cube must be a multiple of the cubedimension.
        corner = [ci.xstart-ci.xstart%cubedimension, ci.ystart-ci.xstart%cubedimension, ci.zstart-ci.xstart%cubedimension]
        cubesize = [cubedimension, cubedimension, cubedimension]

        #We will try to get cached data, or we will use getvtkdata if we miss.  vtkdata does the overlap, so we don't worry about it here.
        start = time.time()
        cornermap = []
        fullcube = vtk.vtkAppendPolyData()
        args = []
        cubecount = 0
        for xcorner in range (ci.xstart-ci.xstart%cubedimension,ci.xstart + ci.xlen, cubedimension):
            for ycorner in range (ci.ystart-ci.ystart%cubedimension,ci.ystart + ci.ylen, cubedimension):
                for zcorner in range (ci.zstart-ci.zstart%cubedimension,ci.zstart + ci.zlen, cubedimension):
                    #cornermap.append([xcorner, ycorner, zcorner])
                    cubecount = cubecount+1
                    args.append([xcorner, ycorner, zcorner, ci, cubedimension,timestep])

                    #output = self.buildcube(corners, ci, cubedimension, timestep)
                    #fullcube.AddInputConnection(output.GetOutputPort())

        #Create a process pool for each cube
        if (cubecount > 8):
            cubecount = 8 #limit to only 8 processes
        p = Pool(cubecount)
        #print("Pool created..mapping args", args)
        #connection.close()
        cubelist = p.map(self.buildcube, args)
        #p.join() #wait for processes to finish
        for filename in cubelist:
            r = vtk.vtkXMLPolyDataReader()
            r.SetFileName(filename)
            #r.Update()
            fullcube.AddInputConnection(r.GetOutputPort())
            #print("Added:", filename)

        connection.close()
        #cleanup processes
        #p = None
        p.close() #free up the ram

        #cubelist = None
        end = time.time()
        comptime = end-start
        print("Final Computation time: " + str(comptime) + "s")
        print("Done processing.")

        xspacing = Dataset.objects.get(dbname_text=ci.dataset).xspacing
        yspacing = Dataset.objects.get(dbname_text=ci.dataset).yspacing
        zspacing = Dataset.objects.get(dbname_text=ci.dataset).zspacing
        box = vtk.vtkBox()
        box.SetBounds(ci.xstart*xspacing, (ci.xstart+ci.xlen-1)*xspacing, ci.ystart*yspacing, (ci.ystart+ci.ylen-1)*yspacing, ci.zstart*zspacing, (ci.zstart + ci.zlen-1)*zspacing)
        fullcube.Update()
        clip = vtk.vtkClipPolyData()
        clip.SetClipFunction(box)
        clip.GenerateClippedOutputOn()
        fco = fullcube.GetOutput()
        fco.GetPointData().SetScalars(fco.GetPointData().GetArray("Velocity"))
        clip.SetInputData(fco)
        clip.InsideOutOn()
        clip.Update()
        #print clip.GetOutput()
        #print("Cleaning cube (removing duplicate points)")
        clean = vtk.vtkCleanPolyData()
        clean.SetInputConnection(clip.GetOutputPort())
        clean.Update()
        #Try to cleanup!
        clip = None
        fullcube = None
        box = None
        return clean.GetOutput()

    def buildcube(self, args):
        path = '/var/www/polycache/'
        ci = args[3]
        cubedimension = args[4]
        timestep = args[5]
        xcorner = args[0]
        ycorner = args[1]
        zcorner = args[2]
        #Django hack for connection issue with multiprocessing.  This forces a new connection for each process.
        connection.close()
        #print("Gettting cube: ", xcorner, ycorner, zcorner)
        mortonstart = jhtdblib.JHTDBLib().createmortonindex(xcorner, ycorner, zcorner)
        mortonend = jhtdblib.JHTDBLib().createmortonindex(xcorner + cubedimension, ycorner + cubedimension, zcorner + cubedimension)
        dataset = Dataset.objects.get(dbname_text=ci.dataset)
        #Determine if we have a hit or miss.
        cache = Polycache.objects.filter(zindexstart =mortonstart,zindexend = mortonend, dataset=dataset, threshold=ci.threshold, timestep=timestep, filterwidth=ci.filter)
        #import pdb; pdb.set_trace();
        #We don't want to cache strided contour data.
        #skip cache for concurrent testing
        fullname = ""
        if ((len(cache) > 0) and 0 and (ci.xstep ==1) and (ci.ystep == 1) and (ci.zstep==1)): #cache hit, serve up the file
            #print("Cache hit " + str(timestep))
            #reader = vtk.vtkXMLPolyDataReader()
            #reader.SetFileName(path + cache[0].filename)
            #print path + cache[0].filename
            #vtpcube = reader
            fullname = path + cache[0].filename

        else: #Cache miss, grab from db and cache the result
            #print ("Cache miss")
            cubeci = copy.deepcopy(ci)
            cubeci.xstart = xcorner
            cubeci.ystart = ycorner
            cubeci.zstart = zcorner
            cubeci.xlen= cubeci.ylen= cubeci.zlen = cubedimension
            start = time.time()
            vtpcube = self.getvtkdata(cubeci, timestep)
            end = time.time()
            #Now write to disk
            writer = vtk.vtkXMLPolyDataWriter()
            vtpfilename = ci.dataset + '-' + str(ci.threshold).replace(".", "_") +'-' +str(ci.filter) + '-'+ str(timestep)+'-'+ str(mortonstart) + '-' + str(mortonend)
            fullname = path + vtpfilename
            writer.SetFileName(fullname)
            writer.SetInputData(vtpcube.GetOutput())
            writer.Write()
            #cleanup
            #ccache = Polycache(zindexstart=mortonstart, zindexend=mortonend, filename=vtpfilename, compute_time=(end-start), threshold=ci.threshold,dataset=dataset, computation=ci.datafields.split(",")[0], timestep=timestep, filterwidth=ci.filter)
            #ccache.save()
            #import pdb;pdb.set_trace()
        #print("Returning cube: ", xcorner, ycorner, zcorner)
        #return vtpcube
        #q.put(vtpcube.GetOutputPort())
        #fullcube.AddInputConnection(vtpcube.GetOutputPort())
        #print("wrote tempfile")
        return (fullname)

    def getvtkdata(self, ci, timestep):
        PI= 3.141592654
        contour=False
        firstval = ci.datafields.split(',')[0]
        #print ("First: ", firstval)
        if ((firstval == 'vo') or (firstval == 'qc') or (firstval == 'cvo') or (firstval == 'pcvo') or (firstval == 'qcc')):
            datafields = 'u'
            computation = firstval #We are doing a computation, so we need to know which one.
            if ((firstval == 'cvo') or (firstval == 'qcc') or (firstval ==
                'pcvo') or (firstval == 'vo') or (firstval == 'qc')):
                overlap = 3 #This was 2, but due to rounding because of the spacing, 3 is required.
                #Save a copy of the original request
                oci = jhtdblib.CutoutInfo()
                oci.xstart = ci.xstart
                oci.ystart = ci.ystart
                oci.zstart = ci.zstart
                oci.xlen = ci.xlen
                oci.ylen = ci.ylen
                oci.zlen = ci.zlen
                #The following should only happen on the first timestep.
                if (ci.tstart == timestep):
                    ci = self.expandcutout(ci, overlap) #Expand the cutout by the overlap
                contour = True
        else:
            datafields = ci.datafields.split(',') #There could be multiple components, so we will have to loop
            computation = ''
        #Split component into list and add them to the image

        #Check to see if we have a value for vorticity or q contour
        fieldlist = list(datafields)
        image = vtk.vtkImageData()
        rg = vtk.vtkRectilinearGrid()
        for field in fieldlist:
            if (ci.xlen > 61 and ci.ylen > +61 and ci.zlen > 61 and ci.xstep ==1 and ci.ystep ==1 and ci.zstep ==1 and not contour):
                #Do this if cutout is too large
                #Note: we don't want to get cubed data if we are doing cubes for contouring.
                data=GetData().getcubedrawdata(ci, timestep, field)
            else:
                data=GetData().getrawdata(ci, timestep, field)
            vtkdata = numpy_support.numpy_to_vtk(data.flat, deep=True, array_type=vtk.VTK_FLOAT)
            components = Datafield.objects.get(shortname=field).components
            vtkdata.SetNumberOfComponents(components)
            vtkdata.SetName(Datafield.objects.get(shortname=field).longname)
            #We need to see if we need to subtract one on end of extent edges.
            image.SetExtent(ci.xstart, ci.xstart+((ci.xlen+ci.xstep-1)/ci.xstep)-1, ci.ystart, ci.ystart+((ci.ylen+ci.ystep-1)/ci.ystep)-1, ci.zstart, ci.zstart+((ci.zlen+ci.zstep-1)/ci.zstep)-1)
            #image.SetExtent(ci.xstart, ci.xstart+int(ci.xlen)-1, ci.ystart, ci.ystart+int(ci.ylen)-1, ci.zstart, ci.zstart+int(ci.zlen)-1)
            image.GetPointData().AddArray(vtkdata)
            if (Datafield.objects.get(shortname=field).longname == "Velocity"):
                #Set the Velocity Array as vectors in the image.
                image.GetPointData().SetVectors(image.GetPointData().GetArray("Velocity"))
#Get spacing from database and multiply it by the step.  Don't do this on the contour--it is performed later on.
            #if (contour):
                #We need to scale the threshold to the spacing of the dataset.  This is because we build the cubes
                #on a 1 spacing cube in order to get proper overlap on the contours.
                #ci.threshold = ci.threshold*Dataset.objects.get(dbname_text=ci.dataset).xspacing
            #else:
            xspacing = Dataset.objects.get(dbname_text=ci.dataset).xspacing
            yspacing = Dataset.objects.get(dbname_text=ci.dataset).yspacing
            zspacing = Dataset.objects.get(dbname_text=ci.dataset).zspacing


            #Check if we need a rectilinear grid, and set it up if so.
            if ci.dataset == 'channel' or ci.dataset == 'transition_bl' or ci.dataset == 'channel5200':
                ygrid = jhtdblib.JHTDBLib().getygrid(ci)
                #print("Ygrid: ")
                #print (ygrid)
                #Not sure about contouring channel yet, so we are going back to original variables at this point.
                rg.SetExtent(ci.xstart, ci.xstart+((ci.xlen+ci.xstep-1)/ci.xstep)-1, ci.ystart, ci.ystart+((ci.ylen+ci.ystep-1)/ci.ystep)-1, ci.zstart, ci.zstart+((ci.zlen+ci.zstep-1)/ci.zstep)-1)
                #components = Datafield.objects.get(shortname=field).components
                #vtkdata.SetNumberOfComponents(components)
                #vtkdata.SetName(Datafield.objects.get(shortname=field).longname)
                rg.GetPointData().AddArray(vtkdata)
                #import pdb;pdb.set_trace()
                #This isn't possible--we will have to do something about this in the future.
                #rg.SetSpacing(ci.xstep,ci.ystep,ci.zstep)
                if ci.dataset == 'channel':
                    xg = np.arange(0,2048.0)
                    zg = np.arange(0,1536.0)                                                            
                    for x in xg:
                            xg[int(x)] = 8*PI/2048*x
                    for z in zg:
                            zg[int(z)] = 3*PI/1536*z
                elif ci.dataset == 'channel5200':
                    xg = np.arange(0,10240.0)
                    zg = np.arange(0,7680.0)                                                            
                    for x in xg:
                            xg[int(x)] = 8*PI/10240*x
                    for z in zg:
                            zg[int(z)] = 3*PI/7680*z
                elif ci.dataset == 'transition_bl':
                    xg = np.arange(0,3320.0)
                    zg = np.arange(0,2048.0)
                    for x in xg:
                            xg[int(x)] = 0.292210466240511*x+30.218496172581567
                    for z in zg:
                            zg[int(z)] = 0.117244748412311*z
                #print (xg[ci.xstart: ci.xstart+((ci.xlen+ci.xstep-1)/ci.xstep)])
                #print (ygrid[ci.ystart: ci.ystart+((ci.ylen+ci.ystep-1)/ci.ystep)])
                #print (zg[ci.zstart: ci.zstart+((ci.zlen+ci.zstep-1)/ci.zstep)])
                vtkxgrid=numpy_support.numpy_to_vtk(xg[ci.xstart: ci.xstart+((ci.xlen+ci.xstep-1)/ci.xstep)], deep=True,
                    array_type=vtk.VTK_FLOAT)
                vtkzgrid=numpy_support.numpy_to_vtk(zg[ci.zstart: ci.zstart+((ci.zlen+ci.zstep-1)/ci.zstep)], deep=True,
                    array_type=vtk.VTK_FLOAT)
                vtkygrid=numpy_support.numpy_to_vtk(ygrid[ci.ystart: ci.ystart+((ci.ylen+ci.ystep-1)/ci.ystep)],
                    deep=True, array_type=vtk.VTK_FLOAT)
                rg.SetXCoordinates(vtkxgrid)
                rg.SetZCoordinates(vtkzgrid)
                rg.SetYCoordinates(vtkygrid)
                image = rg #we rewrite the image since we may be doing a
                           #computation below
            else:
                image.SetSpacing(xspacing*ci.xstep,yspacing*ci.ystep,zspacing*ci.zstep)
        #See if we are doing a computation
        if (computation == 'vo'):
            start = time.time()
            vorticity = vtk.vtkCellDerivatives()
            vorticity.SetVectorModeToComputeVorticity()
            vorticity.SetTensorModeToPassTensors()
            vorticity.SetInputData(image)
            #print("Computing Vorticity")
            vorticity.Update()
            end = time.time()
            comptime = end-start
            print("Vorticity Computation time: " + str(comptime) + "s")
            print ("Returning vorticity")
            print (vorticity.GetOutput())
            print vorticity
            return vorticity.GetOutput()
        elif (computation == 'cvo' or computation == 'pcvo'):
            start = time.time()
            vorticity = vtk.vtkCellDerivatives()
            vorticity.SetVectorModeToComputeVorticity()
            vorticity.SetTensorModeToPassTensors()
            vorticity.SetInputData(image)
            #print("Computing Voricity")
            vorticity.Update()
            vend = time.time()
            comptime = vend-start
            print("Vorticity Computation time: " + str(comptime) + "s")
            mag = vtk.vtkImageMagnitude()
            cp = vtk.vtkCellDataToPointData()
            cp.SetInputData(vorticity.GetOutput())
            #print("Computing magnitude")
            cp.Update()
            mend = time.time()
            image.GetPointData().SetScalars(cp.GetOutput().GetPointData().GetVectors())
            mag.SetInputData(image)
            mag.Update()
            comptime = mend-vend
            print("Magnitude Computation time: " + str(comptime) + "s")
            c = vtk.vtkContourFilter()
            c.SetValue(0,ci.threshold)
            c.SetInputData(mag.GetOutput())
            print("Computing Contour with threshold", ci.threshold)
            c.Update()
            cend = time.time()
            comptime = cend-mend
            print("Contour Computation time: " + str(comptime) + "s")
            #Now we need to clip out the overlap
            box = vtk.vtkBox()
            #set box to requested size
            #The OCI deepcopy didn't seem to work.  Manually taking the overlap again.
            box.SetBounds(oci.xstart*xspacing, (oci.xstart+oci.xlen)*xspacing,
                    oci.ystart*yspacing, (oci.ystart+oci.ylen)*yspacing,
                    oci.zstart*zspacing,(oci.zstart+oci.zlen)*zspacing)
            clip = vtk.vtkClipPolyData()
            clip.SetClipFunction(box)
            clip.GenerateClippedOutputOn()
            clip.SetInputData(c.GetOutput())
            clip.InsideOutOn()
            clip.Update()
            #import pdb;pdb.set_trace()
            cropdata = clip.GetOutput()
            #Cleanup
            image.ReleaseData()
            #mag.ReleaseData()
            #box.ReleaseData()
            #clip.ReleaseData()
            #image.Delete()
            #box.Delete()
            #vorticity.Delete()
            end = time.time()
            comptime = end-start
            print("Total Computation time: " + str(comptime) + "s")
            #return cropdata
            #We need the output port for appending, so return the clip instead
            return clip
        elif (computation == 'qc'):
            q = vtk.vtkGradientFilter()
            q.SetInputData(image)
            q.SetInputScalars(image.FIELD_ASSOCIATION_POINTS,"Velocity")
            q.ComputeQCriterionOn()
            q.Update()
            print ("completed q criterion computation")
            if (ci.dataset == "channel" or ci.dataset == "transition_bl" or ci.dataset == "channel5200"):
                clip = vtk.vtkExtractRectilinearGrid()
            else:
                clip = vtk.vtkExtractVOI()
            clip.SetVOI(oci.xstart,
                    (oci.xstart+oci.xlen-1), oci.ystart,
                    (oci.ystart+oci.ylen-1),
                    oci.zstart,(oci.zstart+oci.zlen-1))
            clip.SetInputData(q.GetOutput())
            clip.Update()
            cropdata = clip.GetOutput()
            cropdata.GetPointData().RemoveArray("Velocity")
            #Cleanup
            image.ReleaseData()
            return cropdata
        elif (computation == 'qcc'):
            start = time.time()
            q = vtk.vtkGradientFilter()
            q.SetInputData(image)
            q.SetInputScalars(image.FIELD_ASSOCIATION_POINTS,"Velocity")
            q.ComputeQCriterionOn()
            q.Update()
            image.GetPointData().SetScalars(q.GetOutput().GetPointData().GetVectors("Q-criterion"))
            #mag = vtk.vtkImageMagnitude()
            #mag.SetInputData(image)
            #mag.Update()
            mend = time.time()
            comptime = mend-start
            #print("Magnitude Computation time: " + str(comptime) + "s")
            c = vtk.vtkContourFilter()
            c.SetValue(0,ci.threshold)
            c.SetInputData(image)
            print("Computing Contour with threshold", ci.threshold)
            c.Update()
            cend = time.time()
            comptime = cend-mend
            print("Q Contour Computation time: " + str(comptime) + "s")
            #clip out the overlap here
            box = vtk.vtkBox()
            #set box to requested size
            box.SetBounds(oci.xstart, oci.xstart+oci.xlen-1, oci.ystart, oci.ystart+oci.ylen-1, oci.zstart,oci.zstart+oci.zlen-1)
            clip = vtk.vtkClipPolyData()
            clip.SetClipFunction(box)
            clip.GenerateClippedOutputOn()
            clip.SetInputData(c.GetOutput())
            clip.InsideOutOn()
            clip.Update()
            cropdata = clip.GetOutput()
            end = time.time()
            comptime = end-start
            print("Computation time: " + str(comptime) + "s")
            #return cropdata
            return cropdata
        else:
            return image
