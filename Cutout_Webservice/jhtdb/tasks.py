from __future__ import absolute_import

from celery import shared_task
from celery import Task
from jhtdb.jhtdblib import JHTDBLib
from jhtdb.jhtdblib import CutoutInfo
from jhtdb.hdfdata import HDFData
from jhtdb.vtkdata import VTKData


@shared_task
def add(x, y):
    return x + y


@shared_task
def mul(x, y):
    return x * y


@shared_task
def xsum(numbers):
    return sum(numbers)

class Getbigcutout(Task):

    def __init__(self):
        self.progress = 0

    def run(self, webargs, ipaddr):
        print("Task begin")
        self.update_state(state='PROGRESS', meta={'cubes': 0, 'percent': 0})
        ci = CutoutInfo()
        ci.ipaddr = ipaddr
        jhlib = JHTDBLib()
        #Parse web args into cutout info object
        ci=jhlib.parsewebargs(webargs)
        ci.persistance = 1
        #Verify token (remove in the future--handled by stored procedure now.
        print ("Checking token")
        isValid, limit = jhlib.verify(ci.authtoken,1)
        if isValid:
            if (ci.filetype == "vtk"):
                #vtkfile = VTKData().getvtk(ci) #Note: This could be a .vtr, .vti, or .zip depending on the request!
                #Set the filename to the dataset name, and the suffix to the suffix of the temp file
                #response['Content-Disposition'] = 'attachment;filename=' +  ci.dataset +'.' + vtkfile.name.split('.').pop()
                #Since VTK can have different file types, getvtk makes those decisions and returns the HTTP response with the correct file info.
                response = VTKData().getvtk(ci)
                print("Got vtk  response")
            else:
                #Serve up an HDF5 file
                path = '/var/www/cutoutcache/'
                h5file = HDFData().gethdf(ci, self)
                filename = ci.dataset + "_" + ci.authtoken + ".h5"
                #f = h5py.File(filename, 'w')
                #copy the tempfile to the permanent file
                #shutil.copy(h5file.name, path + filename)
                print ("Saved HDF file")
                return filename
        else:
            response = HttpResponse("Error: token is invalid")
        #request.session['download_progress'] = 100 #test for progress bar.  Should update based on cutout
        print("returning response")
        return response

