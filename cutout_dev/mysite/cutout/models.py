from django.db import models

# Create your models here.
class Dataset(models.Model):
    dataset_text = models.CharField(max_length=200)
    dbname_text = models.CharField(max_length=30)
    def __unicode__(self):
        return u'{0}'.format(self.dataset_text)
