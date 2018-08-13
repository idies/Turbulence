#!/usr/bin/env python

import os
import sys
import ctypes
import math
import numpy

import pyJHTDB
import pyJHTDB.dbinfo
import pyJHTDB.interpolator
import timeit


def testchannel(numpts):

    ltdb = pyJHTDB.libJHTDB()

    ltdb.initialize()

    ltdb.authToken = "edu.jhu.ssh-c11eeb58"
    #Channel flow sanity check
    #points = numpy.empty((12, 3), dtype = 'float32')
    #points = numpy.array([[1.0, .5, 1.0],[7.1, -.5, 1.0],[1.0, .5, 4.2],[7.2,.5,4.1],[13.0,.5,2.1],[20.1,.5,1.2],[14.3,-.5,4.9],[22.1,.5,5.2],[5.1,.5,7.44],[7.45,.7,7.54],[16.1,.3,8.6],[23.1,-.3,8.94]], dtype= 'float32')

    #for time in range(1,25,1):
        #print("Velocity basic testing time %s" % time)
        #print ltdb.getData(time, points, sinterp=208, data_set='channel', getFunction='getVelocity')

    #print("Velocity Gradient testing")
    #ltdb.getData(0.364, points, sinterp=208, data_set='channel', getFunction='getVelocityGradient', sinterp='FD4NoInt')
    
    #print("querying %s random points from each server" % numpts)
    #time = 1.4100
    time = 0
    #print(" at time %s" % time)

    start = timeit.default_timer()
    #print("Channeldb 01:")
    x = numpy.random.random_sample(size=(numpts,1))[:,:]*2*math.pi
    y = numpy.random.random_sample(size=(numpts,1))[:,:]* 2-1
    z = numpy.random.random_sample(size=(numpts,1))[:,:]*math.pi
    points = numpy.hstack((x,y,z))
    print points
    try:
        ltdb.getData(time, points.astype(numpy.float32), sinterp=208, data_set='channel', getFunction='getVelocity')
    except:
        print ("fail")
    endtime = timeit.default_timer()
    channel1 = endtime-start
    #print("Total time: %s" % (endtime-start))

    start = timeit.default_timer()
    #print("Channeldb 02:")
    x = numpy.random.random_sample(size=(numpts,1))[:,:]*2*math.pi + 2*math.pi  
    y = numpy.random.random_sample(size=(numpts,1))[:,:]* 2-1
    z = numpy.random.random_sample(size=(numpts,1))[:,:]*math.pi 
    points = numpy.hstack((x,y,z))
    #print points
    try:
        ltdb.getData(time, points.astype(numpy.float32), sinterp=208, data_set='channel', getFunction='getVelocity')

    except:
        pass
        print ("fail")
    endtime = timeit.default_timer()
    channel2 = endtime-start
    #print("Total time: %s" % (endtime-start))

    start = timeit.default_timer()
    print("Channeldb 03:")
    x = numpy.random.random_sample(size=(numpts,1))[:,:]*2*math.pi
    y = numpy.random.random_sample(size=(numpts,1))[:,:]* 2-1
    z = numpy.random.random_sample(size=(numpts,1))[:,:]*math.pi + math.pi
    points = numpy.hstack((x,y,z))
    #print points
    try:
        ltdb.getData(time, points.astype(numpy.float32), sinterp=208, data_set='channel', getFunction='getVelocity')
    except:
        print ("fail")
    endtime = timeit.default_timer()
    channel3 = endtime-start
    print("Total time: %s" % (endtime-start))

    start = timeit.default_timer()
    print("Channeldb 04:")
    x = numpy.random.random_sample(size=(numpts,1))[:,:]*2*math.pi + 2*math.pi
    y = numpy.random.random_sample(size=(numpts,1))[:,:]* 2-1
    z = numpy.random.random_sample(size=(numpts,1))[:,:]*math.pi + math.pi
    points = numpy.hstack((x,y,z))
    #print points
    try: 
        ltdb.getData(time, points.astype(numpy.float32), sinterp=208, data_set='channel', getFunction='getVelocity')
    except:
        print ("fail")
    endtime = timeit.default_timer()
    channel4 = endtime-start
    #print("Total time: %s" % (endtime-start))

    start = timeit.default_timer()
    #print("Channeldb 05:")
    x = (numpy.random.random_sample(size=(numpts,1))[:,:]*2*math.pi + 4*math.pi) #-.5 #to avoid a boundary
    y = numpy.random.random_sample(size=(numpts,1))[:,:]* 2-1
    z = numpy.random.random_sample(size=(numpts,1))[:,:]*math.pi - .01
    points = numpy.hstack((x,y,z))
    print points
    try:
        ltdb.getData(time, points.astype(numpy.float32), sinterp=208, data_set='channel', getFunction='getVelocity')
    except:
            print ("fail")
    endtime = timeit.default_timer()
    channel5 = endtime-start
    #print("Total time: %s" % (endtime-start))

    start = timeit.default_timer()
    #print("Channeldb 06:")
    x = numpy.random.random_sample(size=(numpts,1))[:,:]*2*math.pi + 6*math.pi
    y = numpy.random.random_sample(size=(numpts,1))[:,:]* 2-1
    z = numpy.random.random_sample(size=(numpts,1))[:,:]*math.pi
    points = numpy.hstack((x,y,z))
    #print points
    try:
        ltdb.getData(time, points.astype(numpy.float32), sinterp=208, data_set='channel', getFunction='getVelocity')
    except:
            print ("fail")
    endtime = timeit.default_timer()
    channel6 = endtime-start
    #print("Total time: %s" % (endtime-start))

    start = timeit.default_timer()
    #print("Channeldb 07:")
    x = numpy.random.random_sample(size=(numpts,1))[:,:]*2*math.pi + 4*math.pi
    y = numpy.random.random_sample(size=(numpts,1))[:,:]* 2-1
    z = numpy.random.random_sample(size=(numpts,1))[:,:]*math.pi + math.pi
    points = numpy.hstack((x,y,z))
    #print points
    try:
        ltdb.getData(time, points.astype(numpy.float32), sinterp=208, data_set='channel', getFunction='getVelocity')
    except:
            print ("fail")
    endtime = timeit.default_timer()
    channel7 = endtime-start
    #print("Total time: %s" % (endtime-start))

    start = timeit.default_timer()
    print("Channeldb 08:")
    x = numpy.random.random_sample(size=(numpts,1))[:,:]*2*math.pi + 6*math.pi
    y = numpy.random.random_sample(size=(numpts,1))[:,:]* 2-1
    z = numpy.random.random_sample(size=(numpts,1))[:,:]*math.pi + math.pi
    points = numpy.hstack((x,y,z))
    #print points
    try:
        ltdb.getData(time, points.astype(numpy.float32), sinterp=208, data_set='channel', getFunction='getVelocity')
    except:
            print ("fail")
    endtime = timeit.default_timer()
    channel8 = endtime-start
    #print("Total time: %s" % (endtime-start))

    start = timeit.default_timer()
    print("Channeldb 09:")
    x = numpy.random.random_sample(size=(numpts,1))[:,:]*2*math.pi
    y = numpy.random.random_sample(size=(numpts,1))[:,:]* 2-1
    z = numpy.random.random_sample(size=(numpts,1))[:,:]*math.pi + 2*math.pi
    points = numpy.hstack((x,y,z))
    #print points
    try:
        ltdb.getData(time, points.astype(numpy.float32), sinterp=208, data_set='channel', getFunction='getVelocity')
    except:
            print ("fail")
    endtime = timeit.default_timer()
    channel9 = endtime-start
    #print("Total time: %s" % (endtime-start))

    start = timeit.default_timer()
    print("Channeldb 10:")
    x = numpy.random.random_sample(size=(numpts,1))[:,:]*2*math.pi + 2*math.pi
    y = numpy.random.random_sample(size=(numpts,1))[:,:]* 2-1
    z = numpy.random.random_sample(size=(numpts,1))[:,:]*math.pi + 2*math.pi
    points = numpy.hstack((x,y,z))
    #print points
    try:
        ltdb.getData(time, points.astype(numpy.float32), sinterp=208, data_set='channel', getFunction='getVelocity')
    except:
            print ("fail")
    endtime = timeit.default_timer()
    channel10 = endtime-start
    #print("Total time: %s" % (endtime-start))

    start = timeit.default_timer()
    print("Channeldb 11:")
    x = numpy.random.random_sample(size=(numpts,1))[:,:]*2*math.pi + 4*math.pi
    y = numpy.random.random_sample(size=(numpts,1))[:,:]* 2-1
    z = numpy.random.random_sample(size=(numpts,1))[:,:]*math.pi + 2*math.pi
    points = numpy.hstack((x,y,z))
    #print points
    try:
        ltdb.getData(time, points.astype(numpy.float32), sinterp=208, data_set='channel', getFunction='getVelocity')
    except:
            print ("fail")
    endtime = timeit.default_timer()
    channel11 = endtime-start
    #print("Total time: %s" % (endtime-start))

    start = timeit.default_timer()
    print("Channeldb 12:")
    x = numpy.random.random_sample(size=(numpts,1))[:,:]*2*math.pi + 6*math.pi
    y = numpy.random.random_sample(size=(numpts,1))[:,:]* 2-1
    z = numpy.random.random_sample(size=(numpts,1))[:,:]*math.pi + 2*math.pi
    points = numpy.hstack((x,y,z))
    #print points
    try:
        ltdb.getData(time, points.astype(numpy.float32), sinterp=208, data_set='channel', getFunction='getVelocity')
    except:
            print ("fail")
    endtime = timeit.default_timer()
    channel12 = endtime-start
    #print("Total time: %s" % (endtime-start))

    #print ("Time summary")
    #print ("1: %s" % channel1)
    #print ("2: %s" % channel2)
    #print ("3: %s" % channel3)
    #print ("4: %s" % channel4)
    #print ("5: %s" % channel5)
    #print ("6: %s" % channel6)
    #print ("7: %s" % channel7)
    #print ("8: %s" % channel8)
    #print ("9: %s" % channel9)
    #print ("10: %s" % channel10)
    #print ("11: %s" % channel11)
    #print ("12: %s" % channel12)
    channeltimes = [channel1, channel2, channel3, channel4, channel5, channel6, channel7, channel8, channel9, channel10, channel11, channel12]
    return channeltimes
    


