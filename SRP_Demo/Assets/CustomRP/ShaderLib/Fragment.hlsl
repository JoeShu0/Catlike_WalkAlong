#ifndef FRAGMENT_INCLUDED
#define FRAGMENT_INCLUDED

struct Fragment {
	float2 positionSS;
	float depth;
};

Fragment GetFragment (float4 positionSS) {
	Fragment f;
	f.positionSS = positionSS.xy;
	f.depth = IsOrthographicCamera() ? 
		OrthographicDepthBufferToLinear(positionSS.z) : 
		positionSS.w; //this is the viewspace Depth (distance to camera XY plane, only for normal cam)
	return f;
}

#endif