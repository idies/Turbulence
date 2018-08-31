#!/usr/bin/python
########################################################################
#
#  Copyright 2014 Johns Hopkins University
#
# Licensed under the Apache License, Version 2.0 (the "License");
# you may not use this file except in compliance with the License.
# You may obtain a copy of the License at
#
#     http://www.apache.org/licenses/LICENSE-2.0
#
# Unless required by applicable law or agreed to in writing, software
# distributed under the License is distributed on an "AS IS" BASIS,
# WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
# See the License for the specific language governing permissions and
# limitations under the License.
#
# Contact: turbulence@pha.jhu.edu
# Website: http://turbulence.pha.jhu.edu/
#
########################################################################

import os
import sys
if sys.version_info[0] == 2:
    import urllib, urllib2
elif sys.version_info[0] == 3:
    import urllib.request, urllib.parse, urllib.error
import vtk
import numpy as np


def get_cutout(
        x0, y0, z0, t0, xl, yl, zl, tl,
        filename, data_set, data_type,
        auth_token, base_website, overlap, xmax, ymax, zmax):
    #Make overlap even and expand cutout so vorticity contour is corrected
    print ("overlap = %s" %str(overlap))
    halfoverlap= int(overlap)/2
    print ("half = %s" %str(halfoverlap))
    overlap=halfoverlap*2
    xlexpand = xl + overlap
    ylexpand = yl + overlap
    zlexpand = zl + overlap
    if (x0-halfoverlap >=0):
        xunderlap = x0-halfoverlap
        xlexpand = xlexpand - halfoverlap    
    else:
        xunderlap = 0
    if (y0-halfoverlap >=0):
        yunderlap = y0-halfoverlap
        ylexpand = ylexpand - halfoverlap
    else:
        yunderlap = 0
    if (z0-halfoverlap >=0):
        zunderlap = z0-halfoverlap
        zlexpand = zlexpand - halfoverlap
    else:
        zunderlap = 0
    #Prevent hitting outside the bounds of the database here
    if (xmax < (xunderlap + xlexpand)):
        xlexpand = xmax - xunderlap
        print("Truncating x to %d" %xlexpand)
    else:
        print("Ok: %d"% (xunderlap + xlexpand))
    if (ymax < (yunderlap + ylexpand)):
        ylexpand = ymax - yunderlap
        print("Truncating y to %d" %ylexpand )
    if (zmax < (zunderlap + zlexpand)):
        zlexpand = zmax - zunderlap
        print("Truncating z to %d" %zlexpand)
        
    url = ('http://' + base_website + '/cutout/getcutout/'
         + auth_token + '/'
         + data_set + '/' + data_type + ',0.1/'
         + '{0},{1}/'.format(t0, tl)
         + '{0},{1}/'.format(xunderlap, xlexpand)
         + '{0},{1}/'.format(yunderlap, ylexpand)
         + '{0},{1}/'.format(zunderlap, zlexpand)
         + 'vtk/')
    print(url)
    attempts = 0
    
    while attempts < 2: #Try up to 2 times
        try:
            print('Retrieving vtp file %s'% filename)
            if sys.version_info[0] == 2:
                f=urllib2.urlopen(url)
                with open("temp" + filename + '.vtp', "wb") as vtp:
                    vtp.write(f.read())
            elif sys.version_info[0] == 3:
                urllib.request.urlretrieve(url, filename + '.vtp')
            break
        except urllib2.HTTPError, e:
            #print("Error: %s" % (e.reason))
            print("Download failed, trying again")
            attempts +=1
    # check if file downloaded ok
    if attempts == 2:
        print("Too many retries.  Network error or URL issue.")
        raise 
    else:        
        print("Cropping")
        #Crop out the overlap
        reader = vtk.vtkXMLPolyDataReader()
        reader.SetFileName("temp" +filename + '.vtp')
        reader.Update()
        polydata = reader.GetOutput()
        box = vtk.vtkBox()    
        #set box to requested size
        box.SetBounds(x0, x0+xl-1, y0, y0+yl-1, z0,z0+zl-1)
        clip = vtk.vtkClipPolyData()       
        clip.SetClipFunction(box)
        clip.GenerateClippedOutputOn()
        clip.SetInputData(polydata)
        clip.InsideOutOn()
        clip.Update()
        cropdata = clip.GetOutput()
        writer = vtk.vtkXMLPolyDataWriter()
        writer.SetFileName(filename + '-clip.vtp')        
        writer.SetInputData(cropdata)
        writer.Write()
        #Remove temp file
        os.remove("temp" + filename + ".vtp")
    #print('Data downloaded and ' + filename + '.h5 written successfuly.')
    return attempts

def CreateMortonIndex(z,y,x):
    morton = 0
    mask = 0x001
    for i in range (0,20):
        morton += (x & mask) << (2*i)
        morton += (y & mask) << (2*i+1)
        morton += (z & mask) << (2*i+2)
        mask <<= 1
    return morton
     

def main():
    debug = 1
    
    # Get cutout with params
    x0 = 0
    y0 = 0
    z0 = 0
    xl = 1025
    yl = 1025
    zl = 1025
    t0 = 0
    tl = 1
    ts = 4 #timestep
    xmax = 1024
    ymax = 1024
    zmax = 1024
    chunk = 4
    overlap = 4
    fileprefix = 'contour-'
    data_set = 'isotropic1024coarse'
    data_type = 'qcc'
    auth_token = 'edu.jhu.ssh-c11eeb58'
    base_website = 'dsp033.pha.jhu.edu'
    errors = 0
    for c in range(0,tl, ts):
        for z in range(z0, zl+z0, zl/chunk):
            for y in range(y0, yl+y0, yl/chunk):
                for x in range(x0, xl+x0, xl/chunk):
                    filename = fileprefix + str(CreateMortonIndex(z,y,x))
                    print("Getting cutout: " + str(x) + " " + str(y) + " " + str(z) +
                        "x" + str(xl/chunk) + " " + str(yl/chunk) + " " + str(zl/chunk))
                    errors = errors + get_cutout(
                        x, y, z, c, xl/chunk+1, yl/chunk+1, zl/chunk+1, 1,
                        filename, data_set, data_type,
                        auth_token, base_website, overlap, xmax, ymax, zmax)
                    
    return None

if __name__ == '__main__':
    main()

