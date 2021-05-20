using System;
using UnityEngine;

[Serializable]
public struct CameraBufferSettings
{
    public bool allowHDR;
    public bool copyDepth, copyDepthReflection;
    public bool copyColor, copyColorReflection;
    [Range(0.1f, 2.0f)]
    public float renderScale;
    public enum BicubicRescalingMode { Off, UpOnly, UpAndDown }
    public BicubicRescalingMode bicubicResampling;

    [Serializable]
    public struct FXAA
    {
        public bool enabled;
        //   0.0833 - upper limit (default, the start of visible unfiltered edges)
        //   0.0625 - high quality (faster)
        //   0.0312 - visible limit (slower)
        [Range(0.0312f, 0.0833f)]
        public float fixedThreshold;
        //   0.333 - too little (faster)
        //   0.250 - low quality
        //   0.166 - default
        //   0.125 - high quality 
        //   0.063 - overkill (slower)
        [Range(0.063f, 0.333f)]
        public float relativeThreshold;
    }
    public FXAA fxaa;
}