# -*- coding: utf-8 -*-
from __future__ import unicode_literals

from django.db import models, migrations


class Migration(migrations.Migration):

    dependencies = [
        ('jhtdb', '0004_polycache'),
    ]

    operations = [
        migrations.AlterField(
            model_name='polycache',
            name='threshold',
            field=models.FloatField(default=0),
        ),
    ]
