from django.contrib import admin
from .models import Dataset
from .models import Datafield
from .models import Polycache
# Register your models here.

admin.site.register(Polycache)
admin.site.register(Dataset)
admin.site.register(Datafield)

