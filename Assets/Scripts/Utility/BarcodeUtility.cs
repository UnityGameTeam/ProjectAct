using UnityEngine;
using ZXing;
using ZXing.QrCode;

public class BarcodeUtility
{
    public static Color32[] Encode(string textForEncoding, int width, int height)
    {
        var writer = new BarcodeWriter
        {
            Format = BarcodeFormat.QR_CODE,
            Options = new QrCodeEncodingOptions
            {
                Height = height,
                Width = width
            }
        };
        return writer.Write(textForEncoding);
    }

    public static Texture2D EncodeToTexture2D(string textForEncoding, int width, int height)
    {
        var color32 = Encode(textForEncoding, width, height);
        var encoded = new Texture2D(width, height);
        encoded.SetPixels32(color32);
        encoded.Apply();
        return encoded;
    }

    public static void EncodeToTexture2D(string textForEncoding, Texture2D encoded)
    {
        var color32 = Encode(textForEncoding, encoded.width, encoded.height);
        encoded.SetPixels32(color32);
        encoded.Apply();
    }
}


