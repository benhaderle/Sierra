using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Ground : MonoBehaviour
{
    public LayerMask layerMask;
    public int textureSize = 1024;
    public Shader dispShader;

    public RenderTexture displacementTex;
    public Material snowMat, dispMat;

    
    // Start is called before the first frame update
    void Start()
    {
        dispMat = new Material(dispShader);
        dispMat.SetColor("_Color", Color.red);

        displacementTex = new RenderTexture(textureSize, textureSize, 0, RenderTextureFormat.ARGBFloat);
        
        snowMat = GetComponent<MeshRenderer>().material;
        snowMat.SetTexture("_DispTex", displacementTex);
    }

    private void OnTriggerStay(Collider other)
    {
        RaycastHit hit, uvRadHit;
        if(Physics.Raycast(other.transform.position, Vector3.down, out hit, 10, layerMask))
        {
            float height = other.transform.position.y - hit.point.y;
            Physics.Raycast(other.transform.position + new Vector3(((SphereCollider)other).radius, 0, 0), Vector3.down, out uvRadHit, 10, layerMask);
            float uvRadius = Vector2.Distance(hit.textureCoord, uvRadHit.textureCoord);
            dispMat.SetVector("_DispPos", new Vector4(hit.textureCoord.x, hit.textureCoord.y, height, uvRadius));
            Debug.Log(uvRadius);
            RenderTexture temp = RenderTexture.GetTemporary(textureSize, textureSize, 0, RenderTextureFormat.ARGBFloat);
            Graphics.Blit(displacementTex, temp);
            Graphics.Blit(temp, displacementTex, dispMat);
            RenderTexture.ReleaseTemporary(temp);

            
        }
    }
                                       
}
