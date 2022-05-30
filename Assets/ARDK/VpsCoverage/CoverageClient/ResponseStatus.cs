// Copyright 2022 Niantic, Inc. All Rights Reserved.

namespace Niantic.ARDK.VPSCoverage
{
  /// Status of a response from the VPS Coverage server.
  public enum ResponseStatus
  {
    // From API
    Unset,
    Success,
    InvalidRequest,
    InternalError,
    TooManyEntitiesRequested,

    // From UnityWebRequest.Result
    ConnectionError,
    ProtocolError, // all 4xx and 5xx => see Gateway

    // From Gateway
    Forbidden = 403,
    TooManyRequests = 429,
    InternalGatewayError = 500

  }
}
