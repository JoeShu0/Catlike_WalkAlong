using System;
using UnityEngine.Rendering;

[Serializable]
public class CameraSettings
{
    [Serializable]
    public struct FinalBlendMode
    {
        public BlendMode source, destination;
    }

    [RenderingLayerMaskField]//this will enable a custom drawer for this attribute
    public int RenderingLayerMask = -1;

    //optional mask light per camera
    public bool maskLights = false;

    public bool overridePostFX = false;
    public PostFXSettings postFXSettings = default;

    public FinalBlendMode finalBlendMode = new FinalBlendMode
    {
        source = BlendMode.One,
        destination = BlendMode.Zero
    };
}
