import pyodbc, os, math
from cube import Cube
from jhtdblib import CutoutInfo
from jhtdb.models import Datafield
#from multiprocessing import Pool
from django.core.files.temp import NamedTemporaryFile
import cPickle as pickle

class GetData:

    #For now we are only doing 1 timestep and one datafield at a time for simplicity.
    #We can loop and build up a file with multiple timesteps and fields outside of this function
    def getrawdata(self, ci, timestep, datafield):
        cubesize  = [ci.zlen, ci.ylen, ci.xlen]
        filterwidth = ci.filter
        corner = [ci.xstart, ci.ystart, ci.zstart]
        step = [ci.xstep, ci.ystep, ci.zstep]
        cube = Cube(corner, cubesize,step, filterwidth, 3 )
        cube.getCubeData(ci, datafield, timestep)
        return cube.data

    def getcubedrawdata(self, ci, timestep, datafield, selftask = None):
        #We need to chunk the data first
        #cubesize 16
        cubedimension = 256
        components = Datafield.objects.get(shortname=datafield).components
        fullcubesize  = [math.ceil(float(ci.zlen)/float(cubedimension))*cubedimension, math.ceil(float(ci.ylen)/float(cubedimension))*cubedimension, math.ceil(float(ci.xlen)/float(cubedimension))*cubedimension]
        fullcubesize = [int(fullcubesize[0]), int(fullcubesize[1]), int(fullcubesize[2])]
        print ("Full cube size is: ", fullcubesize)
        filterwidth = ci.filter
        corner = [ci.xstart, ci.ystart, ci.zstart]
        step = [ci.xstep, ci.ystep, ci.zstep]
        fullcube = Cube(corner, fullcubesize,step, filterwidth, components )
        cubesize = [cubedimension, cubedimension, cubedimension]
        cubecount = 0
        args = []
        for xcorner in range (ci.xstart,ci.xstart + ci.xlen, cubedimension):
            for ycorner in range (ci.ystart,ci.ystart + ci.ylen, cubedimension):
                for zcorner in range (ci.zstart,ci.zstart + ci.zlen, cubedimension):
                    print("Gettting cube: ", xcorner, ycorner, zcorner)
                    cubecount = cubecount + 1
                    corners = [xcorner, ycorner, zcorner]
                    args.append([corners, ci, cubesize, timestep, step, filterwidth, components, datafield])

        cubesfinished = 0
        for arg in args:
            cube = self.getfilecube(args[cubesfinished])
            fullcube.addData(cube, ci)
            cubesfinished=cubesfinished+1
            if (selftask != None): #Update status for progress bar when tasked
                selftask.update_state(state='PROGRESS', meta={'cubes': cubesfinished, 'total': cubecount})
        #Multiprocessing
        #if (cubecount > 4):
        #    cubecount = 4 #limit to only 8 processes
        #print("Multiprocessing... Cubes: ", (cubecount))
        #p = Pool(cubecount)

        #cubelist = p.map(self.getfilecube, args)
        #self.getfilecube(args[0])
        #print("Mapped")
        #import pdb; pdb.set_trace();
        #p.join()
        #progress = 0
        #for cube in cubelist:
            #cubefile = open( filename , 'rb')
            #cube = pickle.load(cubefile)
            #print ("Cube: " % cube)
            #fullcube.addData(cube, ci)
            #print("Added:", cube)
            #progress = progress +1

        #connection.close()
            #fullcube.addData(cube, ci)
        #p.join()
        print("Complete")
        #connection.close()
        #p.close() #frees the ram
        fullcube.trim(ci)
        print("Trimming complete, returning cube")
        return fullcube.data

    def getfilecube(self, args):

        corners =args[0]
        ci =args[1]
        cubesize =args[2]
        timestep =args[3]
        step = args[4]
        filterwidth = args[5]
        components = args[6]
        datafield = args[7]
        print ("Setting cube: ", corners[0], corners[1], corners[2])
        cube = Cube(corners, cubesize, step, filterwidth, components)
        #mortonstart = jhtdblib.JHTDBLib().createmortonindex(corners[0], corners[1], corners[2])
        #mortonend = jhtdblib.JHTDBLib().createmortonindex(corners[0] + cubesize[0], corners[1] + cubesize[1], corners[2] + cubesize[2])
        #start = time.time()
        print ("getting cube: ", corners[0], corners[1], corners[2])
        #It appears this sometimes fails when parallel processing, so we try up to 3 times...
        retries =3
        attempt = 0
        #while (attempt < retries ):
        #    if (cube.getCubeData(ci, datafield, timestep) != True):
        #        attempt = attempt + 1
        #        print("Retrying cube.")
        #    else:
        #        attempt = 3
        #        print("Completed cube")
        if (cube.getCubeData(ci, datafield, timestep) != True):
            print("Success cube")
        print("Saving cube", corners[0], corners[1], corners[2])

        #tmp = NamedTemporaryFile(suffix='.RAW', mode='w+b', dir='/tmp', delete=False)
        #Save object
        #pickle.dump(cube, tmp, pickle.HIGHEST_PROTOCOL)
        #cPickle.dump(cube, tmp, cPickle.HIGHEST_PROTOCOL)

        return cube




