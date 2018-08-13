#various definitions needed for the cutout service
import os
import pyodbc
import numpy as np
from jhtdb.models import Dataset
import math
from django.conf import settings

class CutoutInfo():
    def __init__(self):
        self.xstart = 0
        self.ystart = 0
        self.zstart = 0
        self.tstart = 0
        self.xlen = 1
        self.ylen = 1
        self.zlen = 1
        self.tlen = 1
        self.dataset = ""
        self.filetype = "hdf5" #can be vtk or hdf, default to hdf for now
        self.datafields = "" #list of datafields
        self.authtoken = "testing"
        self.xstep = 1
        self.ystep = 1
        self.zstep = 1
        self.tstep = 1
        self.filter = 1
        self.threshold = .5 #not a good default, but we need something here.  Should be overwritten by parsewebargs.
        self.preview = 0
        self.ipaddr = "0.0.0.0" #Place for user ip address for logging.
        self.persistance = 0 #used for big files


class JHTDBLib():

    def parsewebargs(self, webargs):
        cutout_info = CutoutInfo()
        w = webargs.split("/")
        cutout_info.dataset = w[1]
        cutout_info.authtoken= w[0]
        cutout_info.tstart = int(w[3].split(',')[0])
        cutout_info.tlen = int(w[3].split(',')[1])
        cutout_info.xstart = int(w[4].split(',')[0])
        cutout_info.xlen = int(w[4].split(',')[1])
        cutout_info.ystart = int(w[5].split(',')[0])
        cutout_info.ylen = int(w[5].split(',')[1])
        cutout_info.zstart = int(w[6].split(',')[0])
        cutout_info.zlen = int(w[6].split(',')[1])
        #For computed fields, set component to velocity.
        cfieldlist = w[2].split(",")
        if ((cfieldlist[0] == 'vo') or (cfieldlist[0] == 'qc') or (cfieldlist[0] == 'cvo') or (cfieldlist[0] == 'qcc')or (cfieldlist[0] == 'pcvo')):
            cutout_info.datafields = w[2]
            if (cfieldlist[1] != ''):
                cutout_info.threshold = float(cfieldlist[1])
            else:
                print("Threshold not found, defaulting")
                #Just in case the user didn't supply anything, we default to the values in the database.
                if ((cfieldlist[0] == 'cvo') or (cfieldlist[0] == 'pcvo')):
                    cutout_info.threshold = Dataset.objects.get(dbname_text=cutout_info.dataset).defaultthreshold
                    cutout_info.filetype='vtk' #Might as well force this--we aren't doing contours with an HDF5 file.
                elif (cfieldlist[0] =='qcc'):
                    cutout_info.filetype='vtk' #Might as well force this--we aren't doing contours with an HDF5 file.
                    cutout_info.threshold = math.sqrt(Dataset.objects.get(dbname_text=cutout_info.dataset).defaultthreshold)
                print(str(cutout_info.threshold) + "  =  Threshold")
            if (cfieldlist[0] == 'pcvo'):
                self.preview = 1
        else:
            cutout_info.datafields = w[2]
            print("Datafields: ", w[2])
        #Set file type
        if (len(w) >= (7)):
            cutout_info.filetype = w[7]
        #Look for step parameters
        if (len(w) > 9):
            s = w[8].split(",")
            cutout_info.tstep = int(s[0])
            cutout_info.xstep = int(s[1])
            cutout_info.ystep = int(s[2])
            cutout_info.zstep = int(s[3])
            cutout_info.filter = int(w[9])

        return cutout_info

    def verify(self, authtoken, attempt):
        try:
            if attempt == 1:
                DBSTRING = settings.ODBC['db_authdb_string']
            else:
                DBSTRING = settings.ODBC['db_authdb_string']
            conn = pyodbc.connect(DBSTRING, autocommit=True)
            cursor = conn.cursor()
            query = "SELECT uid, limit FROM users WHERE authkey = '" + str(authtoken) + "'"
            print ("Query: " + query)
            rows = cursor.execute(query).fetchall()
            limit = rows[0][1]
            if (len(rows) > 0):
                conn.close()
                return (True,limit)
            else:
                conn.close()
                return (False,limit)
        except:
            if attempt == 1:
                return self.verify(authtoken,2)
            else:
                raise


    def getygrid(self, cutInfo):
        DBSTRING = settings.ODBC['db_channel_string']
        conn = pyodbc.connect(DBSTRING, autocommit=True)
        cursor = conn.cursor()
        tableName = 'none'
        if cutInfo.dataset == 'channel5200':
            tableName = 'channel5200_grid_points_y'
        elif cutInfo.dataset == 'channel':
            tableName = 'grid_points_y'
        elif cutInfo.dataset == 'transition_bl':
            tableName = 'BL_grid_points_y'
        rows= cursor.execute("SELECT cell_index, value from %s ORDER BY cell_index" % (tableName,) ).fetchall()
        length = len(rows)
        ygrid = np.zeros((length,1))
        for row in rows:
            ygrid[row.cell_index]=row.value
        conn.close()
        return ygrid

    def createmortonindex(self, z,y,x):
        morton = 0
        mask = 0x001
        for i in range (0,20):
            morton += (x & mask) << (2*i)
            morton += (y & mask) << (2*i+1)
            morton += (z & mask) << (2*i+2)
            mask <<= 1
        return morton

