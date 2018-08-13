# -*- coding: utf-8 -*-
from __future__ import unicode_literals

from django.db import models, migrations


class Migration(migrations.Migration):

    dependencies = [
        ('jhtdb', '0006_auto_20151008_2036'),
    ]

    operations = [
        migrations.AddField(
            model_name='dataset',
            name='xspacing',
            field=models.FloatField(default=1.0),
        ),
        migrations.AddField(
            model_name='dataset',
            name='yspacing',
            field=models.FloatField(default=1.0),
        ),
        migrations.AddField(
            model_name='dataset',
            name='zspacing',
            field=models.FloatField(default=1.0),
        ),
    ]
