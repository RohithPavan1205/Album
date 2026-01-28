from django.db import models

class Client(models.Model):
    client_id = models.CharField(max_length=100, unique=True, help_text="Unique ID for the client")
    client_name = models.CharField(max_length=255)
    created_at = models.DateTimeField(auto_now_add=True)

    def __str__(self):
        return f"{self.client_name} ({self.client_id})"

class MediaAsset(models.Model):
    ASSET_TYPES = [
        ('IMAGE', 'Image'),
        ('VIDEO', 'Video'),
    ]
    client = models.ForeignKey(Client, on_delete=models.CASCADE, related_name='assets')
    asset_type = models.CharField(max_length=10, choices=ASSET_TYPES)
    firebase_url = models.URLField(max_length=500, help_text="Direct download link from Firebase")
    uploaded_at = models.DateTimeField(auto_now_add=True)

    def __str__(self):
        return f"[{self.asset_type}] {self.client.client_name}"
