using UnityEngine;

[DisallowMultipleComponent]
public class PerObjectMaterialProperties : MonoBehaviour
{
    static int baseColorId = Shader.PropertyToID("_BaseColor");

    [SerializeField]
    Color baseColor = Color.white;

    //this is the material property block per object, 
    //you can set properties on this instead of setting on material(Save materials, like instance) 
    //and set this block to material
    //DO NOT WORK WITH CSRP!!!!!
    static MaterialPropertyBlock block;

    private void OnValidate()
    {
        if (block == null)
        {
            block = new MaterialPropertyBlock();
        }
        //block.Clear();//Clear and set the MPB will also release the effect on materials 
        block.SetColor(baseColorId, baseColor);
        GetComponent<Renderer>().SetPropertyBlock(block);
    }

    private void Awake()
    {
        //Onvalidate does not get called in builds, Manually calling
        //OnValidate();
    }
}
