from django.db import models

# Create your models here.
class Dataset(models.Model):
    dataset_text = models.CharField(max_length=50)
    dbname_text = models.CharField(max_length=30)
    xstart = models.IntegerField(default = 0)
    ystart = models.IntegerField(default = 0)
    zstart = models.IntegerField(default = 0)
    tstart = models.IntegerField(default = 0)
    xend = models.IntegerField(default = 1024)
    yend = models.IntegerField(default = 1024)
    zend = models.IntegerField(default = 1024)
    timeend = models.IntegerField(default = 1024)
    timefactor = models.IntegerField(default = 1)
    defaultthreshold = models.FloatField(default = 23.0)
    xspacing = models.FloatField(default = 1.0)
    yspacing = models.FloatField(default = 1.0)
    zspacing = models.FloatField(default = 1.0)
    dt = models.FloatField(default = .002)

    def __unicode__(self):
        return u'{0}'.format(self.dataset_text)
    class Meta:
        ordering = ('dataset_text',)

class Datafield(models.Model):
    longname = models.CharField(max_length=50)
    shortname = models.CharField(max_length = 1)
    components = models.IntegerField()
    dataset = models.ManyToManyField(Dataset)

    def __str__(self):
        return self.longname

    class Meta:
        ordering = ('longname',)

class Polycache(models.Model):
    zindexstart = models.IntegerField(default = 0)
    zindexend = models.IntegerField(default = 0)
    filename = models.CharField(max_length=50)
    compute_time = models.IntegerField(default = 0)
    cache_date = models.DateTimeField
    threshold = models.FloatField(default = 0)
    timestep = models.IntegerField(default = 0)
    dataset = models.ForeignKey(Dataset)
    filterwidth = models.IntegerField(default=1)
    computation = models.CharField(max_length=5, default = 'cvo') #Set to qcc or cvo depending on the calculation
    class Meta:
        ordering = ('zindexstart',)

