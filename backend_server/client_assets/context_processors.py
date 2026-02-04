"""
Context processors for adding global template variables
"""
from .models import Client, MediaAsset


def dashboard_stats(request):
    """
    Add dashboard statistics to all admin templates
    """
    if request.path.startswith('/admin/'):
        total_clients = Client.objects.count()
        total_images = MediaAsset.objects.filter(asset_type='IMAGE').count()
        total_videos = MediaAsset.objects.filter(asset_type='VIDEO').count()
        total_assets = MediaAsset.objects.count()
        recent_assets = MediaAsset.objects.select_related('client').order_by('-uploaded_at')[:5]
        
        return {
            'total_clients': total_clients,
            'total_images': total_images,
            'total_videos': total_videos,
            'total_assets': total_assets,
            'recent_assets': recent_assets,
        }
    return {}
