import firebase_admin
from firebase_admin import credentials, storage
from django.conf import settings
import os
import shutil
from pathlib import Path

_is_initialized = False

def initialize_firebase():
    global _is_initialized
    if _is_initialized:
        return

    try:
        # Check if we have credentials
        cred_path = getattr(settings, 'FIREBASE_CREDENTIALS_PATH', None)
        bucket_name = getattr(settings, 'FIREBASE_STORAGE_BUCKET', 'your-project-id.appspot.com')
        
        if not firebase_admin._apps:
            if cred_path and os.path.exists(cred_path):
                cred = credentials.Certificate(cred_path)
                firebase_admin.initialize_app(cred, {
                    'storageBucket': bucket_name
                })
                _is_initialized = True
            else:
                 print(f"Warning: Firebase credentials file not found at {cred_path}. Using local storage fallback.")
        else:
            _is_initialized = True
    except Exception as e:
        print(f"Error initializing Firebase: {e}")

def upload_file(file_obj, filename, request=None):
    """
    Uploads a file object to Firebase Storage and returns the public URL.
    If Firebase is not configured, saves to local MEDIA_ROOT.
    """
    initialize_firebase()
    
    # Try Firebase first if initialized
    if _is_initialized and os.path.exists(getattr(settings, 'FIREBASE_CREDENTIALS_PATH', '')):
        try:
            bucket = storage.bucket()
            blob = bucket.blob(filename)
            
            # Upload from file-like object
            if hasattr(file_obj, 'seek'):
                file_obj.seek(0)
                
            blob.upload_from_file(file_obj, content_type=file_obj.content_type)
            blob.make_public()
            return blob.public_url
        except Exception as e:
             if not settings.DEBUG:
                 raise e
             print(f"Firebase upload failed: {e}. Falling back to local.")
    
    # Fallback: Local Storage (for dev / when no Firebase creds)
    if settings.DEBUG:
        media_root = Path(settings.MEDIA_ROOT)
        full_path = media_root / filename
        
        # Ensure directories exist
        full_path.parent.mkdir(parents=True, exist_ok=True)
        
        # Save file
        if hasattr(file_obj, 'seek'):
            file_obj.seek(0)
            
        with open(full_path, 'wb+') as destination:
            for chunk in file_obj.chunks():
                destination.write(chunk)
        
        # Construct URL
        # NOTE: Using 127.0.0.1 for editor testing. 
        # For actual device testing, user needs to change this to their IP or use ngrok.
        # We try to get the host from the request if provided.
        host = "http://127.0.0.1:8000"
        if request:
            host = f"{request.scheme}://{request.get_host()}"
            
        return f"{host}{settings.MEDIA_URL}{filename}"
        
    raise Exception("Firebase not configured and not in DEBUG mode.")
