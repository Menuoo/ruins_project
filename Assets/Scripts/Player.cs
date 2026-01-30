using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;

public class Player : MonoBehaviour
{
    [SerializeField] float speed = 2f;
    [SerializeField] float lookSens = 5f;
    public CharacterController rb;
    public GameObject point;


    public BoxCollider plane;

    private void Start()
    {
        plane.enabled = false;
    }

    // Update is called once per frame
    void Update()
    {
        HandleLook();
        HandleMove();

        if (Input.GetKeyDown(KeyCode.E))
        {
            HandleCut();
        }
    }

    void HandleLook()
    {
        Vector2 comb = Vector2.zero;
        comb.x = Input.GetAxis("Mouse X");
        comb.y = -Input.GetAxis("Mouse Y");
        //comb.y = 0f;

        comb = comb * speed;

        this.transform.rotation = Quaternion.Euler(new Vector3(0f, this.transform.eulerAngles.y + comb.x, 0f));
        point.transform.rotation = Quaternion.Euler
            (point.transform.eulerAngles.x + comb.y,
            point.transform.eulerAngles.y /*point.transform.eulerAngles.y + comb.x*/,
            point.transform.eulerAngles.z);

    }

    void HandleMove()
    {
        Vector3 fwd = this.transform.forward;
        Vector3 right = this.transform.right;

        Vector3 comb = Vector3.zero;
        if (Input.GetKey(KeyCode.A))
            comb.x -= 1f;
        if (Input.GetKey(KeyCode.D))
            comb.x += 1f;
        if (Input.GetKey(KeyCode.W))
            comb.z += 1f;
        if (Input.GetKey(KeyCode.S))
            comb.z -= 1f;

        fwd = fwd * comb.z;
        right = right * comb.x;
        comb = fwd + right;

        rb.Move(comb.normalized * speed * Time.deltaTime);
    }

    public Vector3 CutForward()
    {
        return point.transform.forward;
    }

    public Vector3 CutNormal() 
    { 
        return point.transform.up;
    }

    public Vector3 CutPosition()
    {
        return point.transform.position;
    }

    void HandleCut()
    {
        plane.enabled = true;
        plane.GetComponent<Cutter>().id += 1;
        Invoke("HandleOff", 0.1f);
    }

    void HandleOff()
    {
        plane.enabled = false;
    }
}
