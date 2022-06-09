using Niantic.ARDK.AR.Awareness;
using Niantic.ARDK.AR.Awareness.Semantics;
using Niantic.ARDK.Extensions;
using Niantic.ARDK.Utilities.Input.Legacy;
using System.Linq;
using UnityEngine;

public class SemanticQueries : MonoBehaviour
{
    private ISemanticBuffer semanticBuffer;

    [SerializeField]
    private ARSemanticSegmentationManager segmentationManager;

    [SerializeField]
    private Camera arCamera;

    private void Start()
    {
        segmentationManager.SemanticBufferUpdated += SegmentationManager_SemanticBufferUpdated;    
    }

    private void SegmentationManager_SemanticBufferUpdated(ContextAwarenessStreamUpdatedArgs<ISemanticBuffer> args)
    {
        semanticBuffer = args.Sender.AwarenessBuffer;
    }

    private void Update()
    {
        if (PlatformAgnosticInput.touchCount <= 0) return;

        var touch = PlatformAgnosticInput.GetTouch(0);
        
        if (touch.phase == TouchPhase.Began)
        {
            Logger.Instance.LogInfo($"Channels available: {semanticBuffer.ChannelCount}");
            semanticBuffer.ChannelNames.ToList().ForEach(c => Logger.Instance.LogInfo($"Channel: {c}"));

            int x = (int)touch.position.x;
            int y = (int)touch.position.y;

            Logger.Instance.LogInfo($"Touch x: {x} Touch y: {y}");
            segmentationManager.SemanticBufferProcessor.GetChannelNamesAt(x, y)
                .ToList().ForEach(c =>
                {
                    Logger.Instance.LogInfo($"ChannelNames at touch x: {x} touch y: {y} | Channel -> {c}");
                });

            int[] indicesAtPixel = segmentationManager.SemanticBufferProcessor.GetChannelIndicesAt(x, y);
            foreach(var i in indicesAtPixel)
            {
                Logger.Instance.LogInfo($"{i}");
            }
        }
    }
}
