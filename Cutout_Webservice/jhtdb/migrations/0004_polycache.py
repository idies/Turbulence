# -*- coding: utf-8 -*-
from __future__ import unicode_literals

from django.db import models, migrations


class Migration(migrations.Migration):

    dependencies = [
        ('jhtdb', '0003_auto_20150831_2058'),
    ]

    operations = [
        migrations.CreateModel(
            name='Polycache',
            fields=[
                ('id', models.AutoField(verbose_name='ID', serialize=False, auto_created=True, primary_key=True)),
                ('zindexstart', models.IntegerField(default=0)),
                ('zindexend', models.IntegerField(default=0)),
                ('filename', models.CharField(max_length=50)),
                ('compute_time', models.IntegerField(default=0)),
                ('threshold', models.IntegerField(default=0)),
                ('timestep', models.IntegerField(default=0)),
                ('computation', models.CharField(default=b'cvo', max_length=5)),
                ('dataset', models.ForeignKey(to='jhtdb.Dataset')),
            ],
            options={
                'ordering': ('zindexstart',),
            },
        ),
    ]
