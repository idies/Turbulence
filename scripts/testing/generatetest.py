#!/usr//bin/python

#
# Generate SQL test-code for in-database testing of the Turbulence Database
# Eric Perlman, eric@cs.jhu.edu
#

import getopt, sys
import random
import math
import datetime
import os

# Class to hold configurable settings
class Settings:
  database = "turbdb"
  dbTable = "isotropic1024data"
  xmin = ymin = zmin = 0
  xmax = ymax = zmax = 1023
  points = 10
  pointgen = "random"
  execmode = "join"
  verbose = False
  pointTable = "#mypoints"
  joinTable = "#joinTable"
  seed = None;
  flush = False;
  time = 0
  pchip = False
  pchipinc = 1
  dataset = "isotropic1024fine" # or isotropic1024course
  workerType = 1
  #          GetVelocity = 1,
  #          GetVelocityWithPressure = 2,
  #          GetVelocityGradient = 3,
  #          GetPressureGradient = 4,
  #          GetVelocityHessian = 7,
  #          GetPressureHessian = 8,
  #          GetVelocityLaplacian = 5,
  #          GetLaplacianOfVelocityGradient = 6,
  #          GetPosition = 21,
  #          GetBoxFilterPressure = 90,
  #          GetBoxFilterVelocity = 91,
  #          GetBoxFilterVelocityGradient = 92,
  #          GetBoxFilterSGSStress = 93,
  spatialInterp = 6
  #          None = 0,
  #          None_Fd4 = 40,
  #          None_Fd6 = 60,
  #          None_Fd8 = 80,
  #          Fd4Lag4 = 44,
  #          Lag4 = 4,
  #          Lag6 = 6,
  #          Lag8 = 8
  temporalInterp = 0
  #          None = 0,
  #          PCHIP = 1
  timeValue = 0.0002

def usage(settings):
  print "usage: %s [option]" % sys.argv[0]
  print "Options and arguments:"
  print "-h      : print this help message and exit (also --help)"
  print "-d      : database (also --database=DB, default: %s)" % settings.database
  print "-x n-n  : range of values for points in the x dimension (also --x=n-n, default: 0-1023)"
  print "-y n-n  : range of values for points in the y dimension (also --y=n-n, default: 0-1023)"
  print "-z n-n  : range of values for points in the z dimension (also --z=n-n, default: 0-1023)"
  print "          <<the ranges specified above will be converted to 0-2pi (1024=2pi)>>"
  print "-p n    : number of points to use (also --points=n, default: %s)" % settings.points
  print "-m mode : point generation mode (also --mode=mode, default: %s)" % settings.pointgen
  print "          random: random points within the bounding box"
  print "          regular: regular distribution, wi"
  print "-e mode : exection mode (also --exec=mode, default: %s)" % settings.execmode
  print "          modes include [clr,join]"
  print "-v      : verbose (also --verbose)"
  print "-s seed : seed value (also --seed=seed, default: %s)" % settings.seed
  print "-C      : toggle cache flush (also --cacheflush, default: %s)" % settings.flush
  print "-t time : timestep [integer, same as key in turbdb] (also --time=, default %s)" % settings.time
  print "##### The options below are not fully implemented. #####"
  print "--pchip : PCHIP interpolation (time,time+n,time+n,time+n) (default %s)" % settings.pchip
  print "--nopchip : disable PCHIP interpolation"

def main():
  settings = Settings()
  try:
    opts, args = getopt.gnu_getopt(sys.argv[1:], "vhd:p:x:y:z:m:e:s:Ct",
    ["help","database=","points=","x=","y=","z=","mode=","exec=", "verbose", "seed=",
    "cacheflush", "time=", "pchip", "nopchip"])
  except getopt.GetoptError, err:
    # print help information and exit:
    print str(err) # will print something like "option -a not recognized"
    usage(settings)
    sys.exit(2)
  for o, a in opts:
    if o in("-v", "--verbose"):
      settings.verbose = True;
    elif o in ("-h", "--help"):
      usage(settings)
      sys.exit()
    elif o in ("-d", "--database"):
      settings.database = a
    elif o in ("-p", "--points"):
      settings.points= int(a)
    elif o in ("-m", "--mode"):
      settings.pointgen = a
    elif o in ("-e", "--exec"):
      settings.execmode = a
    elif o in ("-s", "--seed"):
      settings.seed = a
    elif o in ("-t", "--time"):
      settings.time = a
    elif o in ("-C", "--cacheflush"):
      settings.flush = not settings.flush
    elif o in ("--pchip"):
      settings.pchip = True
      settings.temporalInterp = 1
    elif o in ("--nopchip"):
      settings.pchip = False
    elif o in ("-x", "-y", "-z", "--x", "--y", "--z"):
      (min,max) = a.split('-')
      min = int(min)
      max = int(max)
      if o in ("-x", "--x"):
        settings.xmin = min
        settings.xmax = max
      elif o in ("-y", "--y"):
        settings.ymin = min
        settings.ymax = max
      elif o in ("-z", "--z"):
        settings.zmin = min
        settings.zmax = max
    else:
      assert False, "unhandled option"
  # ...
  random.seed(settings.seed)
  printHeader(settings)
  points = generatePoints(settings)
  printPoints(settings, points)
  printExecCode(settings)
  printFooter(settings)

#
# Generate the set of query points
#
def generatePoints(settings):
  points = []
  if settings.pointgen == "random":
    for i in xrange(settings.points):
      points.append([random.uniform(settings.xmin,settings.xmax),
                    random.uniform(settings.ymin,settings.ymax),
                    random.uniform(settings.zmin,settings.zmax)])
  elif settings.pointgen == "regular":
    dim = int(math.ceil(math.pow(float(settings.points), 1.0/3.0)))
    if settings.verbose:
      print >> sys.stderr, "Using %s points instead of %s.\n" % (dim*dim*dim, settings.points)
    for i in xrange(dim):
      for j in xrange(dim):
        for k in xrange(dim):
          points.append([settings.xmin + (settings.xmax-settings.xmin)*(float(i)/dim),
                         settings.ymin + (settings.ymax-settings.ymin)*(float(j)/dim),
                         settings.zmin + (settings.zmax-settings.zmin)*(float(k)/dim)])
  else:
    assert False, "Invalid point generation option: %s" % settings.pointgen

  return points

def printHeader(settings):
  print "-- TurbulenceDB testing script"
  print "-- %s" % (datetime.datetime.now().ctime())
  print "-- %s " % ' '.join(sys.argv)
  if settings.flush:
    print "CHECKPOINT"
    print "DBCC DROPCLEANBUFFERS"
    print "GO"
    print "DBCC FREEPROCCACHE"
    print "GO"
  print "USE %s" % settings.database
  print "GO"

def printExecCode(settings):
  if settings.execmode == "join":
    print "SELECT DISTINCT (zindex & 0xfffc0000) AS zindex INTO %s FROM %s" % (settings.joinTable, settings.pointTable)
    print "GO"
    if settings.pchip:
      print "SELECT %s.zindex, %s.data FROM %s, %s WHERE %s.timestep = IN (%s, %s, %s, %s) AND %s.zindex = %s.zindex" % (
        settings.dbTable, settings.dbTable, settings.dbTable, settings.joinTable, settings.dbTable,
        settings.time, settings.time + settings.pchipinc,
        settings.time + settings.pchipinc * 3, settings.time +  settings.pchipinc * 4,
        settings.dbTable, settings.joinTable)
    else:
      print "SELECT %s.zindex, %s.data FROM %s, %s WHERE %s.timestep = %s AND %s.zindex = %s.zindex" % (
        settings.dbTable, settings.dbTable, settings.dbTable, settings.joinTable, settings.dbTable,
        settings.time, settings.dbTable, settings.joinTable)
    print "GO"  
    print "DROP TABLE %s" % (settings.joinTable)
    print "GO"
 
  elif settings.execmode == "clr":
    if settings.pchip:
      assert False, "PCHIP+CLR is not yet implemented"
    else:
     print "EXEC ExecuteTurbulenceWorker %s, %s, %s, %s, %s, 0, %s" % (
       settings.dataset, settings.workerType, settings.time * settings.timeValue, settings.spatialInterp,
       settings.temporalInterp, settings.pointTable)
     print "GO"
  else:
    assert False, "Invalid exec option: %s" % settings.execmode

def printFooter(settings):
  print "DROP TABLE %s" % (settings.pointTable)
  print "GO"

def interleave(x, y, z):
  result = long(0)
  position = 0
  bit = 1

  while bit <= x or bit <= y or bit <= z:
    if bit & z:
        result |= 1 << (3*position+2)
    if bit & y:
        result |= 1 << (3*position+1)
    if bit & x:
        result |= 1 << (3*position)
    position += 1
    bit = 1 << position
  return result

# Scale from 0-1023 to 0-2*pi
def scaleCoordinate(x):
  return (float(x) / 1024.0) * math.pi *2

def printPoints(settings, points):
  print "CREATE TABLE %s (reqseq INT, zindex BIGINT, x REAL, y REAL, z REAL )" % settings.pointTable
  print "GO"
  i = 0
  for (x,y,z) in points:
    zorder = interleave(int(x),int(y),int(z))
    # print "-- x=%s, y=%s, z=%s" % (x, y, z)
    print "INSERT INTO %s VALUES ( %s, %s, %s, %s, %s )" % (settings.pointTable , i, zorder, scaleCoordinate(x), scaleCoordinate(y), scaleCoordinate(z))
    print "GO"
    i += 1

if __name__ == "__main__":
  main()
