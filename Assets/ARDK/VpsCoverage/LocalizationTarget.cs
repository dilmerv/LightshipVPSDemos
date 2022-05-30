// Copyright 2022 Niantic, Inc. All Rights Reserved.

using System;
using System.Threading.Tasks;

using Niantic.ARDK.LocationService;
using Niantic.ARDK.Utilities;
using Niantic.ARDK.VPSCoverage.GeoserviceMessages;

using UnityEngine;
using UnityEngine.UI;

namespace Niantic.ARDK.VPSCoverage
{
  /// A real world target for localization using VPS.
  [Serializable]
  public struct LocalizationTarget
  {
    [SerializeField]
    private string _identifier;
    /// Unique identifier of the LocalizationTarget.
    public string Identifier { get { return _identifier; } }

    [SerializeField]
    private string _name;
    /// Name of the LocalizationTarget.
    public string Name { get { return _name; } }

    [SerializeField]
    private LatLng _center;
    /// Geolocation of the LocalizationTarget.
    public LatLng Center { get { return _center; } }

    [SerializeField]
    private string _imageURL;
    /// Url where hint image is stored.
    public string ImageURL { get { return _imageURL; } }

    internal LocalizationTarget(_LocalizationTargetsResponse.VpsLocalizationTarget vpsLocalizationTarget)
      : this(vpsLocalizationTarget.id, vpsLocalizationTarget.name, vpsLocalizationTarget.image_url, vpsLocalizationTarget.shape.point)
    {
    }

    internal LocalizationTarget(string identifier, string name, string imageUrl, LatLng center)
    {
      _identifier = identifier;
      _name = name;
      _imageURL = imageUrl;
      _center = center;
    }
    
    /// Downloads the image from the provided url as a texture, using the async await pattern.
    /// @param onImageReceived Callback for downloaded image as texture. When download fails,
    /// texture is returned as null.
    public async Task<Texture> DownloadImageAsync()
    {
      Texture image = await _HttpClient.DownloadImageAsync(_imageURL);
      return image;
    }
    
    /// Downloads the image from the provided url as a texture, using the callback pattern.
    /// @param onImageReceived Callback for downloaded image as texture. When download fails,
    /// texture is returned as null.
    public async void DownloadImage(Action<Texture> onImageDownloaded)
    {
      Texture image = await _HttpClient.DownloadImageAsync(_imageURL);
      onImageDownloaded?.Invoke(image);
    }
    
    /// Downloads the image from the provided url as a texture cropped to a fixed size, using the
    /// async await pattern.
    /// The source image is first resampled so the image is fitting for the limiting dimension, then
    /// it gets cropped to the fixed size.
    /// @param width Fixed width of cropped image
    /// @param height Fixed height of cropped image
    /// @param onImageReceived Callback for downloaded image as texture. When download fails,
    /// texture is returned as null.
    public async Task<Texture> DownloadImageAsync(int width, int height)
    {
      Texture image = await _HttpClient.DownloadImageAsync(_imageURL + "=w" + width + "-h" + height + "-c");
      return image;
    }
    
    /// Downloads the image from the provided url as a texture cropped to a fixed size, using the
    /// callback pattern.
    /// The source image is first resampled so the image is fitting for the limiting dimension, then
    /// it gets cropped to the fixed size.
    /// @param width Fixed width of cropped image
    /// @param height Fixed height of cropped image
    /// @param onImageReceived Callback for downloaded image as texture. When download fails,
    /// texture is returned as null.
    public async void DownloadImage(int width, int height, Action<Texture> onImageDownloaded)
    {
      Texture image = await _HttpClient.DownloadImageAsync(_imageURL + "=w" + width + "-h" + height + "-c");
      onImageDownloaded?.Invoke(image);
    }

    // JSONUtility can not handle the nested array format of GeoJson and Newtonsoft will add an
    // additional dependency. Manual built string works for now.
    public string ToGeoJson()
    {
      string geoJson =
        "{" +
          "\"type\": \"FeatureCollection\", \"features\": " +
          "[{" +
            "\"type\": \"Feature\",\"geometry\": " +
            "{" +
              "\"type\": \"Point\"," + "\"properties\": " +
              "{" +
                "\"location_type\": \"localization_target\", " +
                "\"location_image\": \"" + ImageURL + "\", " +
                "\"location_target_identifier\": \"" + Identifier + "\", " +
                "\"location_name\": \"" + Name + "\" " +
              "}," +
              "\"coordinates\": " +
              "[" + 
                Center.Longitude + "," + Center.Latitude + 
              "]" + 
            "}" +
          "}]" +
        "}";

      return geoJson;
    }
  }
}
