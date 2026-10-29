using System.Text;
using SaveBite.Backend.Exceptions;
using SkiaSharp;
using System.Runtime.InteropServices;
using ZXing;
namespace SaveBite.Backend.Helpers;

// Read a VietQR bank-account destination; embedded payment amounts are deliberately ignored.
public static class RefundQrReader
{
    public static Task<(string BankBin, string AccountNumber, string AccountHolder)> ReadAsync(IFormFile file, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        using var stream = file.OpenReadStream();
        using var data = SKData.Create(stream);
        using var codec = SKCodec.Create(data);
        if (codec == null) throw AppException.BadRequest("Upload a readable PNG, JPEG or WebP bank QR image.");
        var info = codec.Info;
        if (info.Width <= 0 || info.Height <= 0 || info.Width > 4096 || info.Height > 4096 || (long)info.Width * info.Height > 12_000_000)
            throw AppException.BadRequest("The QR image is too large. Upload a smaller image.");
        using var decoded = SKBitmap.Decode(codec);
        if (decoded == null) throw AppException.BadRequest("The QR image cannot be decoded.");
        using var image = new SKBitmap(info.Width, info.Height, SKColorType.Rgba8888, SKAlphaType.Opaque);
        using (var canvas = new SKCanvas(image)) { canvas.Clear(SKColors.White); canvas.DrawBitmap(decoded, 0, 0, new SKSamplingOptions(SKFilterMode.Nearest)); }
        var pixels = new byte[checked(image.Width * image.Height * 4)];
        Marshal.Copy(image.GetPixels(), pixels, 0, pixels.Length);
        var reader = new BarcodeReaderGeneric { AutoRotate = true, Options = new ZXing.Common.DecodingOptions {
            TryHarder = true, PossibleFormats = new[] { BarcodeFormat.QR_CODE } } };
        var qr = reader.Decode(new RGBLuminanceSource(pixels, image.Width, image.Height, RGBLuminanceSource.BitmapFormat.RGBA32));
        return Task.FromResult(Parse(qr?.Text ?? throw AppException.BadRequest("No readable QR was found. Upload a clear bank QR or enter bank details.")));
    }

    // Validate the EMV checksum and the NAPAS bank-account transfer scope before extracting bank details.
    public static (string BankBin, string AccountNumber, string AccountHolder) Parse(string payload)
    {
        var bytes = Encoding.UTF8.GetBytes(payload);
        if (bytes.Length < 12 || bytes.Length > 4096 || !payload[^8..].StartsWith("6304", StringComparison.Ordinal))
            throw AppException.BadRequest("This is not a supported VietQR bank-account code.");
        ushort crc = 0xFFFF;
        foreach (var b in bytes[..^4])
        {
            crc ^= (ushort)(b << 8);
            for (var i = 0; i < 8; i++) crc = (ushort)((crc & 0x8000) != 0 ? (crc << 1) ^ 0x1021 : crc << 1);
        }
        if (!ushort.TryParse(payload[^4..], System.Globalization.NumberStyles.HexNumber, null, out var supplied) || supplied != crc)
            throw AppException.BadRequest("The bank QR checksum is invalid.");
        var root = Fields(bytes);
        var merchant = Fields(Encoding.UTF8.GetBytes(root.GetValueOrDefault("38", "")));
        var bank = Fields(Encoding.UTF8.GetBytes(merchant.GetValueOrDefault("01", "")));
        var bin = bank.GetValueOrDefault("00", ""); var account = bank.GetValueOrDefault("01", "");
        if (root.GetValueOrDefault("00") != "01" || root.GetValueOrDefault("53") != "704"
            || merchant.GetValueOrDefault("00") != "A000000727" || merchant.GetValueOrDefault("02") != "QRIBFTTA"
            || bin.Length != 6 || !bin.All(char.IsAsciiDigit) || account.Length is < 1 or > 50 || !account.All(char.IsAsciiDigit))
            throw AppException.BadRequest("Use a Vietnamese bank-account QR, not a card, wallet or merchant payment code.");
        return (bin, account, root.GetValueOrDefault("59", ""));
    }
    private static Dictionary<string, string> Fields(byte[] data)
    {
        var fields = new Dictionary<string, string>();
        for (var offset = 0; offset < data.Length;)
        {
            if (offset + 4 > data.Length || !int.TryParse(Encoding.ASCII.GetString(data, offset + 2, 2), out var length)
                || length < 0 || offset + 4 + length > data.Length) throw AppException.BadRequest("The QR data is malformed.");
            var tag = Encoding.ASCII.GetString(data, offset, 2);
            if (!tag.All(char.IsAsciiDigit) || !fields.TryAdd(tag, Encoding.UTF8.GetString(data, offset + 4, length)))
                throw AppException.BadRequest("The QR data is malformed.");
            offset += 4 + length;
        }
        return fields;
    }
}
