# Backend Server Setup Guide

## Prerequisites
- Python 3.8+
- Firebase Project with Storage enabled

## Setup

1. **Install Dependencies**
   ```bash
   pip install -r requirements.txt
   ```

2. **Firebase Configuration**
   - Go to [Firebase Console](https://console.firebase.google.com/) > Project Settings > Service Accounts.
   - Click "Generate New Private Key".
   - Save the JSON file as `firebase_credentials.json` in this directory (`backend_server/`).
   - Open `config/settings.py` and update `FIREBASE_STORAGE_BUCKET` with your bucket name (e.g., `your-project.appspot.com`).

3. **Database Setup**
   ```bash
   python manage.py makemigrations
   python manage.py migrate
   ```

4. **Create Admin User**
   ```bash
   python manage.py createsuperuser
   ```

5. **Run Server**
   ```bash
   python manage.py runserver
   ```

## Admin Usage
- Go to `http://127.0.0.1:8000/admin/`.
- Login.
- Go to "Media Assets".
- Click "Upload Assets (Batch)" button in the top right.
- Select a Client, multiple images, and a video.
- Click Upload.

## API Usage
- **Get Client Assets**: `GET /api/client/<client_id>/assets`
  
  Response:
  ```json
  {
      "client_id": "client_123",
      "images": ["https://storage.googleapis.com/...", ...],
      "video": "https://storage.googleapis.com/..."
  }
  ```
