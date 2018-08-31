import numpy as np
import pyodbc, os
from jhtdb.models import Datafield, Dataset
import time
from django.conf import settings
import sys

class Cube:

    #  Express cubesize in [ x,y,z ]
    def __init__(self, cubecorner, cubesize, cubestep, filterwidth, components):
        """Create empty array of cubesize"""
        floatsize = 4
        # cubesize is in z,y,x
        self.zlen, self.ylen, self.xlen = self.cubesize = [ cubesize[0],cubesize[1],cubesize[2] ]
        self.xwidth, self.ywidth, self.zwidth = self.cubewidth = [cubesize[2], cubesize[1], cubesize[0]]
        self.xstart, self.ystart, self.zstart = self.corner = [ cubecorner[0], cubecorner[1], cubecorner[2]]
        self.xstep, self.ystep, self.zstep = self.step = [ cubestep[0], cubestep[1], cubestep[2]]
        self.filterwidth = filterwidth
        self.components = components
        # RB this next line is not typed and produces floats.  Cube needs to be created in the derived classes
        #    self.data = np.empty ( self.cubesize )
        self.data = np.empty ([self.zwidth,self.ywidth,self.xwidth,components])
        #self.data.reshape()

    def getCubeData(self, ci, datafield, timestep):
        #get this from field data in db
        components = Datafield.objects.get(shortname=datafield).components
        DBSTRING = settings.ODBC['db_turblib_string']
        retries = 4
        attempt = 0
        while (attempt < retries):
            try:
                if attempt < 2:
                    DBSTRING = settings.ODBC['db_turblib_string']
                else:
                    DBSTRING = settings.ODBC['db_turblib_string']
 
                conn = pyodbc.connect(DBSTRING, autocommit=True)
                attempt = retries
                print('Connecting succeeded:',DBSTRING)
            except:
                print('Connecting failed:',DBSTRING)
                attempt = attempt+1

        cursor = conn.cursor()
        start = time.time()
        #Need to get the time factor which is the multiple of the timestep
        timefactor = Dataset.objects.get(dbname_text=ci.dataset).timefactor
        #It appears this sometimes fails when parallel processing, so we try up to 3 times...
        #retries =3
        attempt = 0
        while (attempt < retries):
            try:
                cursor.execute("{CALL GetAnyCutout(?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?)}",
                    ci.dataset, datafield, ci.authtoken, ci.ipaddr, timestep*timefactor, self.xstart, self.ystart, self.zstart, self.xstep, self.ystep, self.zstep,1,1,self.xwidth,self.ywidth,self.zwidth,self.filterwidth,1)
                attempt = retries #success, so don't let it retry.
                print("SQL Succeeded")
            except:
                print("Call %d failed", attempt)
                attempt = attempt+1

        end = time.time()
        extime = end - start
        print ("DB Execution time: " + str(extime) + " seconds")
        #print (ci.dataset, datafield, timestep, self.xstart, self.ystart, self.zstart, self.xstep, self.ystep, self.zstep,1,1,self.xwidth,self.ywidth,self.zwidth,self.filterwidth,1)
        row = cursor.fetchone()
        raw = row[0]
        part=0
        while(cursor.nextset()):
            row = cursor.fetchone()
            raw = raw + row[0]
            part = part +1
            #print ("added part %d" % part)
            #print ("Part size is %d" % len(row[0]))
        print ("Raw size is %d" % len(raw))
        #print ("components is %d" % components)
        shape = [0]*3
        shape[0] = (self.zwidth+ci.zstep-1)/ci.zstep
        shape[1] = (self.ywidth+ci.ystep-1)/ci.ystep
        shape[2] = (self.xwidth+ci.xstep-1)/ci.xstep
        print("raw size: ", sys.getsizeof(raw))
        print(self.data.shape, " actual: ", np.frombuffer(raw, dtype=np.float32).shape)
        try:
            self.data = np.frombuffer(raw, dtype=np.float32).reshape([shape[0],shape[1],shape[2],components])
            print("Size matched")
            print (self.data.shape)
            print(len(raw))
            print(len(self.data))
            return True
        except:
            print("Size apparently didn't match!")
            print (self.data.shape)
            print(len(raw))
            print(len(self.data))
            return False
        #print("shape = ")
        #print (self.data.shape)
        conn.close()

    def addData ( self, other, ci ):
        """Add data to a larger cube from a smaller cube"""
        xoffset = other.xstart - ci.xstart
        yoffset = other.ystart - ci.ystart
        zoffset = other.zstart - ci.zstart
        #print ("Offsets: ", xoffset, yoffset, zoffset)
        #print("size", other.xlen, other.ylen, other.zlen)
        #zoffset:zoffset+other.zlen,yoffset:yoffset+other.ylen,xoffset:xoffset+other.xlen
        #if (other.data.shape != self.data[zoffset:zoffset+other.zlen,yoffset:yoffset+other.ylen,xoffset:xoffset+other.xlen,0:self.components].shape):
            #print("Data not matching. debug mode")
            #import pdb
            #pdb.set_trace()

        np.copyto ( self.data[zoffset:zoffset+other.zlen,yoffset:yoffset+other.ylen,xoffset:xoffset+other.xlen,0:self.components], other.data[:,:,:,:] )
    def trim ( self, ci ):
        """Trim off the excess data"""
        self.data = self.data [ 0:ci.zlen, 0:ci.ylen, 0:ci.xlen ]



