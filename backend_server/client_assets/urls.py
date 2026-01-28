from django.urls import path
from . import views

urlpatterns = [
    path('client/<str:client_id>/assets', views.client_assets, name='client_assets'),
]
