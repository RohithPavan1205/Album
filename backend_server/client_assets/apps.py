from django.apps import AppConfig
import sys

class ClientAssetsConfig(AppConfig):
    default_auto_field = 'django.db.models.BigAutoField'
    name = 'client_assets'

    def ready(self):
        # Don't run this during migrations or other management commands usually
        if 'runserver' in sys.argv:
            from .firebase_utils import initialize_firebase
            initialize_firebase()
