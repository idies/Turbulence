# -*- coding: utf-8 -*-
from __future__ import unicode_literals

from django.db import models, migrations


class Migration(migrations.Migration):

    dependencies = [
        ('jhtdb', '0007_auto_20151013_1812'),
    ]

    operations = [
        migrations.AddField(
            model_name='polycache',
            name='filterwidth',
            field=models.IntegerField(default=1),
        ),
    ]
