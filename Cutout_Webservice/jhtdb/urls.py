from django.conf.urls import url
from . import views
#from jhtdb.views import Getcutout

urlpatterns = [
    # ex: /jhtdb/
    url(r'^$', views.index, name='index'),
    #url(r'^geturl/$', views.geturl, name='geturl'),
    url(r'^getcutout/(?P<webargs>.*)$', views.getcutout, name='getcutout'),
    #url(r'^getcutout/(?P<webargs>.*)$', Getcutout.as_view()),
    url(r'^preview/(?P<webargs>.*)$', views.preview, name='preview'),
    url(r'^getprogress/(?P<webargs>.*)$', views.getprogress, name='getprogress'),
    url(r'^poll_for_download/$', views.poll_for_download, name='poll_for_download'),
    url(r'^tests/(?P<webargs>.*)$', views.tests, name='tests'),
]
