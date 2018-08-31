# -*- coding: utf-8 -*-
from __future__ import unicode_literals

from django.db import models, migrations


class Migration(migrations.Migration):

    dependencies = [
        ('jhtdb', '0002_auto_20150831_2041'),
    ]

    operations = [
        migrations.RenameModel(
            old_name='Dafafield',
            new_name='Datafield',
        ),
    ]
