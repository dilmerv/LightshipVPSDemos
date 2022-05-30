// Copyright 2022 Niantic, Inc. All Rights Reserved.
using System;
using System.Collections.Generic;
using System.Linq;

using Niantic.ARDK;
using Niantic.ARDK.Configuration;
using Niantic.ARDK.LocationService;
using Niantic.ARDK.VirtualStudio.VpsCoverage;
using Niantic.ARDK.VPSCoverage;

using UnityEngine;
using UnityEngine.UI;

namespace ARDKExamples.VpsCoverage
{
    public class VpsCoverageExampleManager : MonoBehaviour
    {
        [SerializeField]
        private RuntimeEnvironment _coverageClientRuntime = RuntimeEnvironment.Default;

        [SerializeField]
        private VpsCoverageResponses _mockResponses;

        [SerializeField]
        [Tooltip("GPS used in Editor")]
        // Default is the Ferry Building in San Francisco
        private LatLng _spoofLocation = new LatLng(37.79531921750984, -122.39360429639748);

        [SerializeField]
        [Range(0, 500)]
        private int _queryRadius = 250;

        [SerializeField]
        private RawImage _targetImage;

        private ICoverageClient _coverageClient;
        private ILocationService _locationService;

        void Awake()
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

            // The mockResponses object is a ScriptableObject containing the data that a Mock
            // implementation of the ICoverageClient will return. This is a required argument for using
            // the mock client on a mobile device. It is optional in the Unity Editor; the mock client
            // will simply use the data provided in the ARDK/VirtualStudio/VpsCoverage/VPS Coverage Responses.asset file.
            _coverageClient = CoverageClientFactory.Create(_coverageClientRuntime, _mockResponses);

#if UNITY_EDITOR
            var spoofService = (SpoofLocationService)_locationService;

            // In editor, the specified spoof location will be used.
            spoofService.SetLocation(_spoofLocation);
#endif

            _locationService.LocationUpdated += OnLocationUpdated;
            _locationService.Start();
        }

        private void OnLocationUpdated(LocationUpdatedArgs args)
        {
            _locationService.LocationUpdated -= OnLocationUpdated;
            Debug.Log("OnLocationUpdated");
            _coverageClient.RequestCoverageAreas(args.LocationInfo, _queryRadius, ProcessAreasResult);
        }

        private void ProcessAreasResult(CoverageAreasResult result)
        {
            Debug.Log("ProcessAreasResult status: " + result.Status);
            var allTargets = new List<string>();
            if (result.Status != ResponseStatus.Success)
            {
                
                return;
            }

            Debug.Log($"ProcessAreasResult success and areas count : {result?.Areas?.Length ?? 0}");

            foreach (var area in result.Areas)
            {
                allTargets.AddRange(area.LocalizationTargetIdentifiers);
            }

            _coverageClient.RequestLocalizationTargets(allTargets.ToArray(), ProcessTargetsResult);
        }

        private void ProcessTargetsResult(LocalizationTargetsResult result)
        {
            if (result.Status != ResponseStatus.Success)
                return;
            foreach (var target in result.ActivationTargets)
            {
                Debug.Log($"{target.Key}: {target.Value.Name}");
            }

            if (result.ActivationTargets.Count > 0)
            {
                Vector2 imageSize = _targetImage.rectTransform.sizeDelta;
                LocalizationTarget firstTarget = result.ActivationTargets.FirstOrDefault().Value;

                firstTarget.DownloadImage((int)imageSize.x, (int)imageSize.y, args => _targetImage.texture = args);

#if UNITY_EDITOR
                (_locationService as SpoofLocationService).StartTravel(result.ActivationTargets.FirstOrDefault().Value.Center, 1);
#endif
            }

        }
    }
}
