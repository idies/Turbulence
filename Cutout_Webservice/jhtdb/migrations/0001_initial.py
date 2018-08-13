# -*- coding: utf-8 -*-
from __future__ import unicode_literals

from django.db import models, migrations


class Migration(migrations.Migration):

    dependencies = [
    ]

    operations = [
        migrations.CreateModel(
            name='Dafafield',
            fields=[
                ('id', models.AutoField(verbose_name='ID', serialize=False, auto_created=True, primary_key=True)),
                ('longname', models.CharField(max_length=50)),
                ('shortname', models.CharField(max_length=1)),
                ('components', models.IntegerField()),
            ],
            options={
                'ordering': ('longname',),
            },
        ),
        migrations.CreateModel(
            name='Dataset',
            fields=[
                ('id', models.AutoField(verbose_name='ID', serialize=False, auto_created=True, primary_key=True)),
                ('dataset_text', models.CharField(max_length=50)),
                ('dbname_text', models.CharField(max_length=30)),
                ('xstart', models.IntegerField()),
                ('ystart', models.IntegerField()),
                ('zstart', models.IntegerField()),
                ('xend', models.IntegerField()),
                ('yend', models.IntegerField()),
                ('zend', models.IntegerField()),
            ],
            options={
                'ordering': ('dataset_text',),
            },
        ),
        migrations.AddField(
            model_name='dafafield',
            name='dataset',
            field=models.ManyToManyField(to='jhtdb.Dataset'),
        ),
    ]
