from django.conf.urls import include, url
from django.contrib import admin

urlpatterns = [
    url(r'^cutout/', include('cutout.urls')),
    url(r'^jhtdb/', include('jhtdb.urls')),
    url(r'^admin/', include(admin.site.urls)),
    url(r'^$', include('jhtdb.urls')),
]

#For dev server to server the correct static files
from django.contrib.staticfiles.urls import staticfiles_urlpatterns
urlpatterns += staticfiles_urlpatterns()


