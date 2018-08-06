using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeshContainer : MonoBehaviour {

    // Use this for initialization
    void Start()
    {
    }

    public void setMesh(Mesh mesh)
    {
        GetComponent<MeshFilter>().mesh = mesh;
    }

    // Update is called once per frame
    void Update () {
		
	}
}
