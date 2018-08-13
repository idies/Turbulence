from django.shortcuts import render
from django.http import HttpResponse
from django.http import HttpResponseForbidden
from django.template import RequestContext, loader
from django.core.files.temp import NamedTemporaryFile
from django import forms
import json
import vtk
import tempfile
import h5py
import zipfile

from .models import Dataset
from .models import Datafield
from jhtdblib import JHTDBLib
from jhtdblib import CutoutInfo
from hdfdata import HDFData
from vtkdata import VTKData
from contextlib import closing
from tasks import add
from tasks import Getbigcutout
#from django.views.generic import View

# Create your views here.
class CutoutForm(forms.Form):
    token = forms.CharField(label = 'token', max_length=200, initial="edu.jhu.pha.turbulence.testing-201311")
    fileformat = forms.ChoiceField(choices=[('vtk', 'VTK'), ('hdf5', 'HDF5')])
    dataset = forms.ModelChoiceField(queryset=Dataset.objects.all().order_by('dataset_text'), to_field_name="dbname_text", help_text="Choose a dataset")
    #datafields = forms.MultipleChoiceField(choices=[('u', 'Velocity'), ('p', 'Pressure')])
    #datafields = forms.MultipleChoiceField()
    datafields = forms.ModelMultipleChoiceField(queryset=Datafield.objects.all(), to_field_name ="shortname")
    cdatafields = forms.ChoiceField(choices=[('', '---------'),
        ('vo', 'Vorticity'),
        ('qc', 'Q-Criterion'),
        ('cvo', 'Vorticity Contour'),
        ('qcc', 'Q-Criterion Contour')])
    timestart = forms.CharField(label =  'timestart', max_length=5, initial="0")
    timeend = forms.CharField(label =  'timeend', max_length=5, initial="1")
    x = forms.CharField(label =  'x', max_length=5, initial="0")
    xEnd = forms.CharField(label =  'xEnd', max_length=5, initial="10")
    y = forms.CharField(label =  'y', max_length=5, initial="0")
    yEnd = forms.CharField(label =  'yEnd', max_length=5, initial="10")
    z = forms.CharField(label =  'z', max_length=5, initial="0")
    zEnd = forms.CharField(label =  'zEnd', max_length=5, initial="10")
    tstep = forms.CharField(label =  'tstep', max_length=5, initial="1")
    xstep = forms.CharField(label =  'xstep', max_length=5, initial="1")
    ystep = forms.CharField(label =  'ystep', max_length=5, initial="1")
    zstep = forms.CharField(label =  'zstep', max_length=5, initial="1")
    filter = forms.CharField(label =  'filter', max_length=5, initial="1")
    threshold = forms.CharField(label =  'threshold', max_length=5)
    step_checkbox = forms.BooleanField(label = 'step_checkbox')


def index(request):
    dataset_list = Dataset.objects.order_by('dataset_text')
    #output = '<br /> '.join([p.dataset_text for p in dataset_list])
    #return HttpResponse(output)
    template = loader.get_template('jhtdb/index.html')
    if (request.method == "POST"):
        token = request.POST.get("token", "")
        ts = request.POST.get("timestart", "")
        te = request.POST.get("timeend", "")
        xs = request.POST.get("x", "")
        xe = request.POST.get("xEnd", "")
        ys = request.POST.get("y", "")
        ye = request.POST.get("yEnd", "")
        zs = request.POST.get("z", "")
        ze = request.POST.get("zEnd", "")
        dataset = request.POST.get("dataset", "")
        datafield = request.POST.getlist("datafields", "")
        datafields = ''.join(datafield)
        if (len(datafield) == 0):
            if (request.POST.get("threshold", "") is None):
                datafields = request.POST.get("cdatafields", "")
            else:
                threshold = request.POST.get("threshold", "")
                datafields = request.POST.get("cdatafields", "") + "," + threshold

        filetype = request.POST.get("fileformat", "")
        server = request.META['HTTP_HOST']
        url = "http://" + server + "/jhtdb/getcutout/"+ token + "/" + dataset + "/" + datafields + "/" + ts + "," +te + "/" + xs + "," + xe +"/" + ys + "," + ye +"/" + zs + "," + ze + "/" + filetype + "/"
        if (request.POST.get("step_checkbox", "")):
            url = url + "/" + request.POST.get("tstep") + "," + request.POST.get("xstep") + "," + request.POST.get("ystep") + "," + request.POST.get("zstep") + "/" + request.POST.get("filter")
        download_link = url
        dataset_list = Dataset.objects.order_by('dataset_text')
        datafield_list = Datafield.objects.order_by('longname')
        form = CutoutForm(request.POST)
        context = RequestContext(request, {'datafield_list': datafield_list, 'dataset_list': dataset_list, 'download_link': download_link, 'form': form})
    else:
        form = CutoutForm()
        datafield_list = Datafield.objects.order_by('longname')
        download_link = "Link: " #placeholder until download link is generated
        #import pdb;pdb.set_trace()
        context = RequestContext(request, { 'datafield_list': datafield_list, 'dataset_list': dataset_list, 'form': form})
    return HttpResponse(template.render(context))


#For progress bar
def getprogress(request, webargs):
    #return render(request, 'progressbar.html', {'progress': request.session["download_progress"]})
    #return HttpResponse(simplejson.dumps(request.session["download_progress"]))
    return HttpResponse(json.dumps(request.session["download_progress"]), content_type="application/json")

def poll_for_download(request):
    task_id = request.GET.get("task_id")
    filename = request.GET.get("filename")
    print("Polling task id  %s" % task_id)

    if request.is_ajax():
        task = Getbigcutout()
        result = task.AsyncResult(task_id)
        print("Ajax requested..checking result");

        if result.ready():
            print("Result is ready")
            return HttpResponse(json.dumps({"filename": result.get()}))
        print("Result isn't ready:", result)
        #import pdb; pdb.set_trace();
        print(result.result)

        return HttpResponse(json.dumps({"filename": None, "result": result.result})) #result.PROGRESS


    try:
        print ("Opening file")
        f = open("/var/www/cutoutcache/"+filename)
    except:
        return HttpResponseForbidden()
    else:
        response = HttpResponse(file, mimetype='text/csv')
        response['Content-Disposition'] = 'attachment; filename=%s' % filename
    return response

def getcutout(request, webargs):
    ci = CutoutInfo()
    #ci.ipaddr = request.META.get('REMOTE_ADDR', '')
    jhlib = JHTDBLib()
    #Parse web args into cutout info object
    ci=jhlib.parsewebargs(webargs)
    
    x_forwarded_for = request.META.get('HTTP_X_FORWARDED_FOR')
    if x_forwarded_for:
        ci.ipaddr = x_forwarded_for.split(',')[0]
    else:
        ci.ipaddr = request.META.get('REMOTE_ADDR')
    print ("From IP2: ")
    print ci.ipaddr
    numpoints = ci.xlen * ci.ylen * ci.zlen
    if ((numpoints > 16777215) and (ci.filetype == 'hdf5')): #task out anything larger than 256x256x256 and ignore shape

        isValid, limit = jhlib.verify(ci.authtoken,1)
        if limit != 0 and numpoints > limit:
            return HttpResponse("Error: number of points requested exceeds limit for given token")

        #ipaddr = request.META.get('REMOTE_ADDR', '')
        getcutout = Getbigcutout()
        task = getcutout.delay(webargs, ci.ipaddr)
        #Test without celery
        #getcutout.run(webargs, ipaddr)
        print ("Task id is  ")
        print task.task_id
        print ("From IP: ")
        print ci.ipaddr
        template = loader.get_template('poll_for_download.html')
        #print("returning http response. with task id  %s" % task.task_id)
        html = template.render({'task_id': task.task_id}, request)
        return HttpResponse(html)
    else:
        #Verify token (remove in the future--handled by stored procedure now.
        isValid, limit = jhlib.verify(ci.authtoken,1)
        if isValid:
            if limit != 0 and numpoints > limit:
                response = HttpResponse("Error: number of points requested exceeds limit for given token")
            else:
                if (ci.filetype == "vtk"):
                    print ("From IP3: ")
                    print ci.ipaddr
                    #vtkfile = VTKData().getvtk(ci) #Note: This could be a .vtr, .vti, or .zip depending on the request!
                    #Set the filename to the dataset name, and the suffix to the suffix of the temp file
                    #response['Content-Disposition'] = 'attachment;filename=' +  ci.dataset +'.' + vtkfile.name.split('.').pop()
                    #Since VTK can have different file types, getvtk makes those decisions and returns the HTTP response with the correct file info.
                    response = VTKData().getvtk(ci)
                else:
                    #Serve up an HDF5 file
                    h5file = HDFData().gethdf(ci)
                    response = HttpResponse(h5file, content_type='application/x-hdf;subtype=bag')
                    attach = 'attachment;filename=' + ci.dataset + '.h5'
                    response['Content-Disposition'] = attach
        else:
            response = HttpResponse("Error: token is invalid")
        print ("returing file")
        return response
    print("Shouldn't get here")

def preview(self, request, webargs):
    ci = CutoutInfo()
    jhlib = JHTDBLib()
    #Parse web args into cutout info object
    ci=jhlib.parsewebargs(webargs)
    template = loader.get_template('jhtdb/preview.html')
    #Verify token
    #sample link
    #getdata_link = "http://dsp033.pha.jhu.edu:8000/jhtdb/getcutout/edu.jhu.ssh-c11eeb58/isotropic1024coarse/pcvo,/0,1/0,64/0,64/0,64/vtk/"
    #This is kind of a hack, but we have to set the link up properly for the template.
    getdata_link = webargs.replace("cvo", "pcvo")
    isValid, limit = jhlib.verify(ci.authtoken,1)
    if isValid:
        context = RequestContext(request, { 'getdata_link': getdata_link})
        response = HttpResponse(template.render(context))
    else:
        response = HttpResponse("Error: token is invalid")
    return response

def tests(self, webargs):
    from testchannel import testchannel
    from django.template import Context, Template, loader
    print webargs[0]
    print ("was arg")
    chantimes = testchannel(int(webargs.split('/')[0]))
    #chantimes= [30.1, 20.1]
    context = Context({ 'chantimes': chantimes, 'c': 1})
    t = loader.get_template('jhtdb/tests.html')
    response = HttpResponse(t.render(context))
    return response



