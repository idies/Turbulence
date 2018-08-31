# -*- coding: utf-8 -*-
from __future__ import unicode_literals

from django.db import models, migrations


class Migration(migrations.Migration):

    dependencies = [
        ('jhtdb', '0005_auto_20150929_1933'),
    ]

    operations = [
        migrations.AddField(
            model_name='dataset',
            name='defaultthreshold',
            field=models.FloatField(default=23.0),
        ),
        migrations.AddField(
            model_name='dataset',
            name='timefactor',
            field=models.IntegerField(default=1),
        ),
    ]
