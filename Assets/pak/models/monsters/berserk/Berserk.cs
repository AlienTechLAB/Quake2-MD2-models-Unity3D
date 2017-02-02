using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Berserk : MonoBehaviour
{
    //---------------------------------------------------------------------------------------------------------

    private void Update()
    {
        Vector3 euler = new Vector3(0, Time.deltaTime * 50, 0);
        transform.Rotate(euler);
    }

    //---------------------------------------------------------------------------------------------------------
}
