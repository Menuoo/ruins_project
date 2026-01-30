using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Cutter : MonoBehaviour
{
    public LayerMask cuttables;
    public Player player;
    public BoxCollider collid;
    public int id = 0;

    private void OnTriggerEnter(Collider other)
    {
        Vector3 fwd = player.CutForward();
        Vector3 up = player.CutNormal();
        Vector3 pos = player.CutPosition();

        if (collid.enabled)
        {
            //collid.enabled = false;
            other.gameObject.GetComponent<Cut>().PerformCut(true, pos, fwd, up, id);
        }
    }
}
