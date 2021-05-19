using System;
using UnityEngine;

[System.Serializable]
public struct CameraBufferSettings
{
    public bool allowHDR;
    public bool copyDepth, copyDepthReflection;
    public bool copyColor, copyColorReflection;
    [Range(0.1f, 2.0f)]
    public float renderScale;
    public enum BicubicRescalingMode { Off, UpOnly, UpAndDown }
    public BicubicRescalingMode bicubicResampling;
}