from django.contrib import admin
from django.urls import path
from django.shortcuts import render, redirect
from django.contrib import messages
from django import forms
import uuid
from .models import Client, MediaAsset
from .firebase_utils import upload_file



class MultipleFileInput(forms.ClearableFileInput):
    allow_multiple_selected = True

    def value_from_datadict(self, data, files, name):
        if hasattr(files, 'getlist'):
            return files.getlist(name)
        return files.get(name)

class MultipleFileField(forms.FileField):
    def to_python(self, data):
        if not data:
            return None
        # data is expected to be a list from MultipleFileInput
        if isinstance(data, list):
             return [super(MultipleFileField, self).to_python(f) for f in data]
        return super(MultipleFileField, self).to_python(data)

    def clean(self, data, initial=None):
        # Validates that input is a list of files
        if not data and self.required:
             raise forms.ValidationError(self.error_messages['required'], code='required')
        return data

class BatchUploadForm(forms.Form):
    client = forms.ModelChoiceField(queryset=Client.objects.all(), empty_label="Select Client ID")
    images = MultipleFileField(
        widget=MultipleFileInput(attrs={'multiple': True}), 
        required=False,
        help_text="Select multiple images to upload"
    )
    video = forms.FileField(
        required=False,
        help_text="Select one video to upload"
    )

@admin.register(Client)
class ClientAdmin(admin.ModelAdmin):
    list_display = ('client_name', 'client_id', 'created_at')
    search_fields = ('client_name', 'client_id')

@admin.register(MediaAsset)
class MediaAssetAdmin(admin.ModelAdmin):
    list_display = ('client', 'asset_type', 'uploaded_at', 'firebase_url')
    list_filter = ('asset_type', 'client')
    change_list_template = "admin/mediaasset_change_list.html"

    def get_urls(self):
        urls = super().get_urls()
        my_urls = [
            path('batch_upload/', self.admin_site.admin_view(self.batch_upload_view), name='mediaasset_batch_upload'),
        ]
        return my_urls + urls

    def batch_upload_view(self, request):
        if request.method == 'POST':
            form = BatchUploadForm(request.POST, request.FILES)
            if form.is_valid():
                client = form.cleaned_data['client']
                # Now images is already a list from cleaned_data
                images = form.cleaned_data['images'] 
                if images is None: images = []
                
                video = form.cleaned_data['video']
                
                uploaded_count = 0
                
                # Upload Images
                for img in images:
                    # Generic unique filename
                    filename = f"clients/{client.client_id}/images/{uuid.uuid4()}_{img.name}"
                    try:
                        url = upload_file(img, filename, request) # Pass request
                        MediaAsset.objects.create(
                            client=client,
                            asset_type='IMAGE',
                            firebase_url=url
                        )
                        uploaded_count += 1
                    except Exception as e:
                        messages.error(request, f"Failed to upload {img.name}: {e}")
                
                # Upload Video
                if video:
                    filename = f"clients/{client.client_id}/videos/{uuid.uuid4()}_{video.name}"
                    try:
                        url = upload_file(video, filename, request) # Pass request
                        MediaAsset.objects.create(
                            client=client,
                            asset_type='VIDEO',
                            firebase_url=url
                        )
                        uploaded_count += 1
                    except Exception as e:
                        messages.error(request, f"Failed to upload video: {e}")
                
                if uploaded_count > 0:
                    messages.success(request, f"Successfully uploaded {uploaded_count} assets for {client.client_name}.")
                return redirect('admin:client_assets_mediaasset_changelist')
        else:
            form = BatchUploadForm()
        
        context = {
           **self.admin_site.each_context(request),
           'form': form,
           'title': 'Batch Upload Assets',
           'opts': self.model._meta,
        }
        return render(request, 'admin/batch_upload.html', context)
