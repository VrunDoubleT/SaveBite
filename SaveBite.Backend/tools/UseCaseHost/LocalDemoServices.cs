using SaveBite.Backend.Services.Interfaces;
using SaveBite.Backend.Models.Requests;
public sealed class LocalDemoUploads : ICloudinaryService
{
 public const string Root = "/tmp/savebite-uc050-assets";
 public async Task<CloudinaryUploadResult> UploadImageAsync(IFormFile file, CancellationToken ct = default) {
  if(file is SaveBite.Backend.Uploads.StoredUploadFile stored)return stored.Use();
  Directory.CreateDirectory(Root); var name = Guid.NewGuid().ToString("N") + ".png";
  await using var output = File.Create(Path.Combine(Root,name)); await file.CopyToAsync(output,ct);
  return new("https://127.0.0.1:8086/api/demo-assets/"+name,"uc050/"+name);
 }
 public Task<CloudinaryUploadResult> UploadDocumentAsync(IFormFile file,CancellationToken ct=default)=>UploadImageAsync(file,ct);
 public string TemporaryPublicId(Guid id)=>"uc050/"+id.ToString("N");
 public Task<CloudinaryUploadResult> UploadTemporaryAsync(IFormFile file,Guid id,bool raw,CancellationToken ct=default)=>UploadImageAsync(file,ct);
 public Task<bool> DeleteImageAsync(string publicId)=>DeleteAssetAsync(publicId,"image");
 public Task<bool> DeleteAssetAsync(string publicId,string resourceType,CancellationToken ct=default) { if(publicId.StartsWith("uc050/")){var path=Path.Combine(Root,Path.GetFileName(publicId));if(File.Exists(path))File.Delete(path);}return Task.FromResult(true); }
}
public sealed class LocalDemoTravel : ITravelTimeService
{
 public Task<TravelEstimate> EstimateAsync(CheckoutLocation origin,double lat,double lon,CancellationToken ct=default)=>Task.FromResult(new TravelEstimate(1.2m,8));
}
