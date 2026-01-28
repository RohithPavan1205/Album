from django.http import JsonResponse
from django.views.decorators.http import require_GET
from django.shortcuts import get_object_or_404
from .models import Client

@require_GET
def client_assets(request, client_id):
    """
    Returns JSON with image URLs and video URL for a given client_id.
    """
    # Using filter().first() instead of get_object_or_404 to avoid HTML error page if not found, 
    # though valid API design might prefer 404. User asked for JSON.
    try:
        client = Client.objects.get(client_id=client_id)
    except Client.DoesNotExist:
        return JsonResponse({'error': 'Client not found'}, status=404)

    assets = client.assets.all()
    
    # Extract URLs
    images = [asset.firebase_url for asset in assets if asset.asset_type == 'IMAGE']
    
    # Get the *last* video uploaded or the first one? usually "one video" implies uniqueness.
    # We will take the most recent one.
    video_assets = assets.filter(asset_type='VIDEO').order_by('-uploaded_at')
    video_url = video_assets.first().firebase_url if video_assets.exists() else None
    
    return JsonResponse({
        'client_id': client.client_id,
        'images': images,
        'video': video_url
    })
