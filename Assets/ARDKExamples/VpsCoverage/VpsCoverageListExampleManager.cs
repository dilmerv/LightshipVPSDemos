// Copyright 2022 Niantic, Inc. All Rights Reserved.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Niantic.ARDK;
using Niantic.ARDK.Configuration;
using Niantic.ARDK.LocationService;
using Niantic.ARDK.VirtualStudio.VpsCoverage;
using Niantic.ARDK.VPSCoverage;

using UnityEngine;
using UnityEngine.UI;

using LocationServiceStatus = Niantic.ARDK.LocationService.LocationServiceStatus;

namespace ARDKExamples.VpsCoverage
{
  public class VpsCoverageListExampleManager: MonoBehaviour
  {
    public enum MapApp
    {
      GoogleMaps,
      AppleMaps
    }

    [Header("Runtime Settings")]
    [SerializeField]
    [Tooltip("Request VPS Coverage from server (Live Device) or from mock responses")]
    private RuntimeEnvironment _runtimeEnvironment = RuntimeEnvironment.Default;

    [SerializeField]
    [Tooltip("GPS used in Editor")]
    // Default is the Ferry Building in San Francisco
    private LatLng _spoofLocation = new LatLng(37.79531921750984, -122.39360429639748);

    [SerializeField]
    [Tooltip("Select the app to show directions")]
    private MapApp _mapApp = MapApp.GoogleMaps;

    [SerializeField]
    [Tooltip("These responses are used in mock mode")]
    private VpsCoverageResponses _mockResponses;

    [Header("ScrollList Setup")]
    [SerializeField]
    [Tooltip("The scroll list holding the items for each target information")]
    private ScrollRect _scrollList;

    [SerializeField]
    [Tooltip("Template item for target information")]
    private GameObject _itemPrefab;

    [SerializeField]
    [Tooltip("Targets items above this GameObject will have details downloaded")]
    private RectTransform _loadMoreItemsThreshold;

    [Tooltip("Target details can be downloaded in batches")]
    [SerializeField]
    [Min(1)]
    private int _loadingBatchSize = 4;

    [Header("UI Setup")]
    [SerializeField]
    [Tooltip("Button to request to reload the list")]
    private Button _requestButton;
    
    [SerializeField]
    [Tooltip("Text displayed on the request button")]
    private Text _requestButtonText;
    
    [SerializeField]
    [Tooltip("Slider GameObject to set query radius")]
    private Slider _slider;

    [SerializeField]
    [Tooltip("Text field to display current slider value")]
    private Text _queryRadiusText;

    private ICoverageClient _coverageClient;
    private ILocationService _locationService;

    private readonly List<GameObject> _items = new List<GameObject>();
    private readonly List<CoverageArea> _CoverageAreas = new List<CoverageArea>();
    private GameObject _scrollListContent;

    private List<string> _targetIds = new List<string>();
    private int _nextItemToLoad = 0;

    private LatLng _requestLocation;
    private int _queryRadius;

    void Start()
    {
      // This is necessary for setting the user id associated with the current user. 
      // We strongly recommend generating and using User IDs. Accurate user information allows
      //  Niantic to support you in maintaining data privacy best practices and allows you to
      //  understand usage patterns of features among your users.  
      // ARDK has no strict format or length requirements for User IDs, although the User ID string
      //  must be a UTF8 string. We recommend avoiding using an ID that maps back directly to the
      //  user. So, for example, don’t use email addresses, or login IDs. Instead, you should
      //  generate a unique ID for each user. We recommend generating a GUID.
      // When the user logs out, clear ARDK's user id with ArdkGlobalConfig.ClearUserIdOnLogout

      //  Sample code:
      //  // GetCurrentUserId() is your code that gets a user ID string from your login service
      //  var userId = GetCurrentUserId(); 
      //  ArdkGlobalConfig.SetUserIdOnLogin(userId);
      
      _locationService = LocationServiceFactory.Create();
      _locationService.LocationUpdated += args =>
      {
        _requestButtonText.text = "Request for GPS location";
        _requestButton.interactable = true;
      };

      _coverageClient = CoverageClientFactory.Create(_runtimeEnvironment, _mockResponses);
      _scrollListContent = _scrollList.content.gameObject;

#if UNITY_EDITOR
      var spoofService = (SpoofLocationService)_locationService;
      spoofService.SetLocation(_spoofLocation.Latitude, _spoofLocation.Longitude);
#elif UNITY_ANDROID
            _mapApp = MapApp.GoogleMaps;
#elif UNITY_IOS
            _mapApp = MapApp.AppleMaps;
#endif

      _scrollList.onValueChanged.AddListener(OnScroll);
      _slider.onValueChanged.AddListener(OnRadiusChanged);
      _queryRadius = (int)_slider.value;
      _queryRadiusText.text = _queryRadius.ToString();
    }

    private void Update()
    {
        if (_locationService.Status != LocationServiceStatus.Running)
        {
            _locationService.Start();
            Debug.Log("Location service not running");
        }
    }

    public void RequestAreas(bool useSpoof)
    {
      ClearListContent();

      if (useSpoof)
      {
         Debug.Log("Request Areas useSpoof=true");
        _requestLocation = _spoofLocation;
        _coverageClient.RequestCoverageAreas(_spoofLocation, _queryRadius, ProcessAreasResult);
      }
      else
      {


                Debug.Log("Request Areas useSpoof=false");
                _requestLocation = new LatLng(_locationService.LastData);

                Debug.Log($"lat: {_locationService.LastData.Coordinates.Latitude} long: {_locationService.LastData.Coordinates.Longitude}");
                _coverageClient.RequestCoverageAreas(_locationService.LastData, _queryRadius, ProcessAreasResult);
      }
    }

    private void ProcessAreasResult(CoverageAreasResult areasResult)
    {
      if (CheckAreasResult(areasResult))
      {
        FillScrollList(areasResult);
        ResizeListContent();
      }
    }

    private void LoadNextTargetDetails(int batchSize)
    {
      int count = Math.Min(Math.Max(0, _targetIds.Count - _nextItemToLoad), batchSize);
      string[] targetIds = _targetIds.GetRange(_nextItemToLoad, count).ToArray();
      int itemIndex = _nextItemToLoad;
      _nextItemToLoad += batchSize;
      _coverageClient.RequestLocalizationTargets(targetIds,
        targetsResult =>
        {
          if (targetsResult.Status != ResponseStatus.Success)
          {
            Debug.LogWarning
            (
              "LocalizationTarget request failed with status: " +
              targetsResult.Status +
              "\nSkipping batch"
            );

            return;
          }

          foreach (string targetId in targetIds)
          {
            LocalizationTarget target = targetsResult.ActivationTargets[targetId];
            FillTargetItem(itemIndex, target);
            itemIndex++;
          }
        });
    }
    
    private void OnRadiusChanged(float newRadius)
    {
      _queryRadius = (int)newRadius;
      _queryRadiusText.text = _queryRadius.ToString();
    }

    private bool IsUnloadedItemAboveThreshold()
    {
      return _nextItemToLoad < _targetIds.Count &&
        _items[_nextItemToLoad].transform.position.y > _loadMoreItemsThreshold.position.y;
    }

    private void OnScroll(Vector2 scrollDirection)
    {
      while (IsUnloadedItemAboveThreshold())
        LoadNextTargetDetails(_loadingBatchSize);
    }

    private void ClearListContent()
    {
      foreach (GameObject item in _items)
        Destroy(item);

      _CoverageAreas.Clear();
      _targetIds.Clear();
      _items.Clear();
      _nextItemToLoad = 0;
    }

    private bool CheckAreasResult(CoverageAreasResult areasResult)
    {
      if (areasResult.Status != ResponseStatus.Success)
      {
                 Debug.Log("Areas: " + areasResult?.Areas?.Length);
        Debug.LogWarning("CoverageAreas request failed with status: " + areasResult.Status);
        return false;
      }

      if (areasResult.Areas.Length == 0)
      {
        Debug.Log
        (
          "No areas found at " +
          _locationService.LastData.Coordinates +
          " with radius " +
          _queryRadius
        );

        return false;
      }

      return true;
    }

    private void FillScrollList(CoverageAreasResult result)
    {
      _CoverageAreas.AddRange(result.Areas.ToList());

      _CoverageAreas.Sort
      (
        (a, b) => a.Centroid.Distance(_requestLocation)
          .CompareTo(b.Centroid.Distance(_requestLocation))
      );

      foreach (var area in _CoverageAreas)
      {
        foreach (var target in area.LocalizationTargetIdentifiers)
        {
          _targetIds.Add(target);
          GameObject newTargetItem = Instantiate(_itemPrefab, _scrollListContent.transform, false);
          if (area.LocalizabilityQuality == CoverageArea.Localizability.EXPERIMENTAL)
            newTargetItem.GetComponent<Image>().color = new Color(1, 0.9409157f, 0.6933962f);

          _items.Add(newTargetItem);
        }
      }
    }

    private void ResizeListContent()
    {
      VerticalLayoutGroup layout = _scrollListContent.GetComponent<VerticalLayoutGroup>();
      RectTransform contentTransform = _scrollListContent.GetComponent<RectTransform>();
      float itemHeight = _itemPrefab.GetComponent<RectTransform>().sizeDelta.y;
      contentTransform.sizeDelta = new Vector2
      (
        contentTransform.sizeDelta.x,
        layout.padding.top + _scrollListContent.transform.childCount * (layout.spacing + itemHeight)
      );

      // Scroll all the way up
      contentTransform.anchoredPosition = new Vector2(0, Int32.MinValue);
    }
    
    private void FillTargetItem(int itemIndex, LocalizationTarget target)
    {
      Transform itemTransform = _items[itemIndex].transform;
      itemTransform.name = target.Name;

      Transform image = itemTransform.Find("Image");
      target.DownloadImage(downLoadedImage => image.GetComponent<RawImage>().texture = downLoadedImage);

      Transform title = itemTransform.Find("Info/Title");
      title.GetComponent<Text>().text = target.Name;

      Transform distance = itemTransform.Find("Info/Distance");
      double distanceInM = target.Center.Distance(_requestLocation);
      distance.GetComponent<Text>().text += "Distance: " + distanceInM.ToString("N0") + " m";

      Transform button = itemTransform.Find("Info/Button");
      button.GetComponent<Button>()
        .onClick.AddListener
          (delegate { OpenRouteInMapApp(_locationService.LastData.Coordinates, target.Center); });
    }

    private void OpenRouteInMapApp(LatLng from, LatLng to)
    {
      StringBuilder sb = new StringBuilder();

      if (_mapApp == MapApp.GoogleMaps)
      {
        sb.Append("https://www.google.com/maps/dir/?api=1&origin=");
        sb.Append(from.Latitude);
        sb.Append("+");
        sb.Append(from.Longitude);
        sb.Append("&destination=");
        sb.Append(to.Latitude);
        sb.Append("+");
        sb.Append(to.Longitude);
        sb.Append("&travelmode=walking");
      }
      else if (_mapApp == MapApp.AppleMaps)
      {
        sb.Append("http://maps.apple.com/?saddr=");
        sb.Append(from.Latitude);
        sb.Append("+");
        sb.Append(from.Longitude);
        sb.Append("&daddr=");
        sb.Append(to.Latitude);
        sb.Append("+");
        sb.Append(to.Longitude);
        sb.Append("&dirflg=w");
      }

      Application.OpenURL(sb.ToString());
    }
  }
}
