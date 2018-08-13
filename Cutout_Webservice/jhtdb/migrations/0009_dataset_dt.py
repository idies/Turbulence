# -*- coding: utf-8 -*-
from __future__ import unicode_literals

from django.db import models, migrations


class Migration(migrations.Migration):

    dependencies = [
        ('jhtdb', '0008_polycache_filterwidth'),
    ]

    operations = [
        migrations.AddField(
            model_name='dataset',
            name='dt',
            field=models.FloatField(default=0.002),
        ),
    ]
