from django.shortcuts import render
from django.template import RequestContext, loader
from django.http import HttpResponse
from django.core.files.temp import NamedTemporaryFile
from django import forms
import vtk
import tempfile
import h5py
import zipfile

from .models import Dataset
import odbccutout
# Create your views here.

#def get_filetypes():

class CutoutForm(forms.Form):
    token = forms.CharField(label = 'token', max_length=50)
    fileformat = forms.ChoiceField(choices=[('vtk', 'VTK'), ('hdf5', 'HDF5')])
    dataset = forms.ModelChoiceField(queryset=Dataset.objects.all().order_by('dataset_text'), to_field_name="dbname_text")
    datafields = forms.MultipleChoiceField(choices=[('u', 'Velocity'), ('p',
        'Pressure')])
    cdatafields = forms.ChoiceField(choices=[('', '---------'),
        ('vo', 'Vorticity'),
        ('qc', 'Q-Criterion'),
        ('cvo', 'Vorticity Contour'),
        ('qcc', 'Q-Criterion Contour')])
    timestart = forms.CharField(label =  'timestart', max_length=5)
    timeend = forms.CharField(label =  'timeend', max_length=5)
    x = forms.CharField(label =  'x', max_length=5)
    xEnd = forms.CharField(label =  'xEnd', max_length=5)
    y = forms.CharField(label =  'y', max_length=5)
    yEnd = forms.CharField(label =  'yEnd', max_length=5)
    z = forms.CharField(label =  'z', max_length=5)
    zEnd = forms.CharField(label =  'zEnd', max_length=5)
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
    template = loader.get_template('cutout/index.html')

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
        url = "http://" + server + "/cutout/getcutout/"+ token + "/" + dataset + "/" + datafields + "/" + ts + "," +te + "/" + xs + "," + xe +"/" + ys + "," + ye +"/" + zs + "," + ze + "/" + filetype + "/"
        if (request.POST.get("step_checkbox", "")):
            url = url + "/" + request.POST.get("tstep") + "," + request.POST.get("xstep") + "," + request.POST.get("ystep") + "," + request.POST.get("zstep") + "/" + request.POST.get("filter") 
        download_link = url
        dataset_list = Dataset.objects.order_by('dataset_text')
        form = CutoutForm(request.POST)
        context = RequestContext(request, { 'dataset_list': dataset_list, 'download_link': download_link, 'form': form}) 
    else:
        form = CutoutForm()
        download_link = "Link: " #placeholder until download link is generated
        context = RequestContext(request, { 'dataset_list': dataset_list, 'form': form}) 
    return HttpResponse(template.render(context))
    

def getcutout(request, webargs):
    params = "parameters=%s" % webargs
    w = webargs.split("/")
    ts = int(w[3].split(',')[0])
    te = int(w[3].split(',')[1])
    #o = odbccutout.OdbcCutout()
    if ((len(w) >= 8) and (w[7] == 'vtk')):    
        #Setup temporary file (would prefer an in-memory buffer, but this will have to do for now)
        if (len(w) >= 10):
            tspacing = int(w[8].split(",")[0])
        else:
            tspacing = 1
        cfieldlist = w[2].split(",")
        firstval = cfieldlist[0]
        if ((firstval == 'cvo') or (firstval == 'qcc')): #we may need to return a vtp file
            tmp = NamedTemporaryFile(suffix='.vtp')
            suffix = 'vtp'
            writer = vtk.vtkXMLPolyDataWriter()                         
            outfile = w[7] + '-contour'
        elif (w[1] == "channel"):
	    tmp = NamedTemporaryFile(suffix='.vtr')
	    suffix = 'vtr'
	    writer = vtk.vtkXMLRectilinearGridWriter()
	    outfile = 'cutout' + w[7]
        else:
            tmp = NamedTemporaryFile(suffix='.vti')
            suffix = 'vti'
            writer = vtk.vtkXMLImageDataWriter()                        
            outfile = 'cutout' + w[7]        
        writer.SetFileName(tmp.name)
        writer.SetCompressorTypeToZLib()
        writer.SetDataModeToBinary()
        #if multiple timesteps, zip the file.
        if ((te-ts) > 1):
            #Write each timestep to file and read it back in.  Seems to be the only way I know how to put all timesteps in one file for now
            #Create a timestep for each file and then send the user a zip file
            ziptmp = NamedTemporaryFile(suffix='.zip')
            z = zipfile.ZipFile(ziptmp.name, 'w')
            for timestep in range (ts,te, tspacing ):            
                image = odbccutout.OdbcCutout().getvtkimage(webargs, timestep)
                writer.SetInputData(image)
                writer.SetFileName(tmp.name)                        
                writer.Write()
                #Now add this file to the zipfile
                z.write(tmp.name, 'cutout' + str(timestep) + '.' + suffix)
            z.close()
            ct = 'application/zip'
            suffix = 'zip'
            response = HttpResponse(ziptmp, content_type=ct)

        else:
            image = odbccutout.OdbcCutout().getvtkimage(webargs, ts)
            writer.SetInputData(image)
            writer.SetFileName(tmp.name)                        
            writer.Write()
            ct = 'data/vtk'
            response = HttpResponse(tmp, content_type=ct)
        response['Content-Disposition'] = 'attachment;filename=' +  outfile +'.' + suffix
    else: #for backward compatibility, we serve hdf5 if not specified
        #Create an HDF5 file here
        h5file = odbccutout.OdbcCutout().gethdf(webargs)
        response = HttpResponse(h5file, content_type='data/hdf5')
        attach = 'attachment;filename=' + w[1] + '.h5'
        response['Content-Disposition'] = attach

    return response
    #return HttpResponse(result)



