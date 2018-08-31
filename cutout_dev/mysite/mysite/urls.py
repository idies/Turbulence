from django.conf.urls import include, url
from django.contrib import admin
from django.conf import settings


urlpatterns = [
    url(r'^cutout/', include('cutout.urls')),
    url(r'^jhtdb/', include('jhtdb.urls')),
    url(r'^admin/', include(admin.site.urls)),
    url(r'^$', include('jhtdb.urls')),
]

#for debug toolbar
if settings.DEBUG:
    import debug_toolbar
    urlpatterns = [
        url(r'^__debug__/', include(debug_toolbar.urls)),
    ] + urlpatterns
    


#For dev server to server the correct static files
from django.contrib.staticfiles.urls import staticfiles_urlpatterns
urlpatterns += staticfiles_urlpatterns()


