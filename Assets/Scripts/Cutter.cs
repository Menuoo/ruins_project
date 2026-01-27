using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Cutter : MonoBehaviour
{
    public LayerMask cuttables;
    public Player player;
    public BoxCollider collid;

    private void OnTriggerEnter(Collider other)
    {
        Vector3 fwd = player.CutForward();
        Vector3 up = player.CutNormal();
        Vector3 pos = player.CutPosition();

        //RaycastHit hit;

        //Vector3 dir = other.transform.position - pos;

        if (true)//Physics.Raycast(pos, vec, out hit, 5f, cuttables))
        {
            //pos = hit.point;

            other.gameObject.GetComponent<Cut>().PerformCut(true, pos, fwd, up);
        }

        collid.enabled = false;
    }
}
