using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PhotoEye : MonoBehaviour
{
    BoxCollider bc;

    // Start is called before the first frame update
    void Start()
    {
        Debug.Log("fuck");
        HingeJoint hinge = gameObject.GetComponent(typeof(HingeJoint)) as HingeJoint;
        bc = gameObject.GetComponent(typeof(BoxCollider)) as BoxCollider;
        if (bc != null)
        {
            Debug.Log("yeet");
        }
        else
        {
            Debug.Log("double fuck");
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
