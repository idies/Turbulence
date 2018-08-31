"""
WSGI config for mysite project.

It exposes the WSGI callable as a module-level variable named ``application``.

For more information on this file, see
https://docs.djangoproject.com/en/1.8/howto/deployment/wsgi/
"""

import os, site, sys

os.environ["DJANGO_SETTINGS_MODULE"] = "mysite.settings"
from django.core.wsgi import get_wsgi_application
_application = get_wsgi_application()
env_variables_to_pass = ['db_turblib_string', 'db_channel_string']
def application(environ, start_response):
# pass the WSGI environment variables on through to os.environ
    for var in env_variables_to_pass:
        os.environ[var] = environ.get(var, '')
    return _application(environ, start_response)

