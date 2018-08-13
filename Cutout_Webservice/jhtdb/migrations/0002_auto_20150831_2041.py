# -*- coding: utf-8 -*-
from __future__ import unicode_literals

from django.db import models, migrations


class Migration(migrations.Migration):

    dependencies = [
        ('jhtdb', '0001_initial'),
    ]

    operations = [
        migrations.AddField(
            model_name='dataset',
            name='timeend',
            field=models.IntegerField(default=1024),
        ),
        migrations.AddField(
            model_name='dataset',
            name='tstart',
            field=models.IntegerField(default=0),
        ),
        migrations.AlterField(
            model_name='dataset',
            name='xend',
            field=models.IntegerField(default=1024),
        ),
        migrations.AlterField(
            model_name='dataset',
            name='xstart',
            field=models.IntegerField(default=0),
        ),
        migrations.AlterField(
            model_name='dataset',
            name='yend',
            field=models.IntegerField(default=1024),
        ),
        migrations.AlterField(
            model_name='dataset',
            name='ystart',
            field=models.IntegerField(default=0),
        ),
        migrations.AlterField(
            model_name='dataset',
            name='zend',
            field=models.IntegerField(default=1024),
        ),
        migrations.AlterField(
            model_name='dataset',
            name='zstart',
            field=models.IntegerField(default=0),
        ),
    ]
