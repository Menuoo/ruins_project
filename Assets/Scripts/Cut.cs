using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;
using static UnityEditor.PlayerSettings;

public class Cut : MonoBehaviour
{
    [SerializeField] float power = 200f;
    [SerializeField] Rigidbody rb;
    [SerializeField] MeshFilter cube;
    [SerializeField] MeshCollider collid;

    int lastId = -1;

    Mesh alterMesh;

    Vector3 cutPos = Vector3.zero;
    Vector3 locPos = Vector3.zero;

    Vector3 cutFwd = Vector3.zero;
    Vector3 cutNorm = Vector3.zero;

    List<Vector3> newGoats = new List<Vector3>();

    private void OnDrawGizmos()
    {
        int i = 0;
        foreach (var v in newGoats)//Vector3 v in alterMesh.vertices) // split into top and bot
        {
            Gizmos.color = Color.HSVToRGB(i * 1.0f / newGoats.Count, 1f, 1f);
            Gizmos.DrawSphere(Matrix4x4.Rotate(this.transform.rotation).MultiplyPoint(v) + this.transform.position, 0.02f);
            i++;
        }
    }


    private void Start()
    {
        Configure();
    }

    public void Configure()
    {
        alterMesh = cube.mesh;
        rb.sleepThreshold = 0.0f;
    }

    // Update is called once per frame
    void Update()
    {
        rb.AddForce(Vector3.zero);
        if (Input.GetKeyDown(KeyCode.Space))
        {
            PerformCut(true);
        }
    }


    public void PerformCut(bool condition)
    {
        //BasicHalfing();
        AddGeometry(condition);


        // Recalculate Bounds of mesh as well as the collider.
        alterMesh.RecalculateBounds();
        collid.sharedMesh = alterMesh;

        /*collid.center = alterMesh.bounds.center;
            //new Vector3(0, alterMesh.bounds.size.y * gameObject.transform.localScale.reciprocal().y * 0.5f, 0);
        collid.size = alterMesh.bounds.size;
            //Vector3.Scale(alterMesh.bounds.size, gameObject.transform.localScale.reciprocal());*/
    }


    public void PerformCut(bool condition, Vector3 pos, Vector3 fwd, Vector3 up, int newId)
    {
        if (newId == lastId)
        {
            return;
        }
        lastId = newId;

        cutPos = pos;
        cutFwd = fwd;
        cutNorm = up;

        locPos = this.transform.InverseTransformPoint(cutPos);

        //Debug.Log("loc: " + locPos);
        //Debug.Log("fwd: " + cutFwd);

        RealCut1(condition);

        alterMesh.RecalculateBounds();
        collid.sharedMesh = alterMesh;

        rb.AddForce(condition ? cutNorm * power : -cutNorm * power);
    }

    public void RealCut1(bool condition)  // simple heuristic: anything lower than y=0 goes. we create new vertices at that height (all in local for simplicity)\
    {
        Debug.Log("------------------");

        if (condition)
        {
            Cut newCutObj = Instantiate(this, this.transform.position, this.transform.rotation);
            newCutObj.PerformCut(false, cutPos, cutFwd, cutNorm, lastId);
        }
        else
        {
            Configure();
        }


        List<Vector3> newV = new List<Vector3>();
        List<int> newTri = new List<int>();

        List<Vector3> edgeV = new List<Vector3>();
        List<int> edgeNum = new List<int>();


        int i = 0;
        List<int> topV = new List<int>();

        float planeD = -(cutNorm.x * locPos.x + cutNorm.y * locPos.y + cutNorm.z * locPos.z);

        foreach (Vector3 v in alterMesh.vertices) // split into top and bot
        {
            Vector3 interPoint = Vector3.zero;
            float yVal = 0f;

            /*if (cutFwd.x > cutFwd.z)
            {
                Debug.Log("fwd: " + cutFwd);
                interPoint = locPos + cutFwd * ((v.x - locPos.x) / cutFwd.x);
                Debug.Log("interpoint: " + interPoint);
            }
            else 
            {
                Debug.Log("fwd: " + cutFwd);
                interPoint = locPos + cutFwd * ((v.z - locPos.z) / cutFwd.z);
                Debug.Log("interpoint: " + interPoint);
            }*/

            /*if (Mathf.Abs(cutFwd.y) > 0.0001f)
                yVal = locPos.y + ((v.x - locPos.x) * cutFwd.x + (v.z - locPos.z) * cutFwd.z) * cutFwd.y;
            else yVal = locPos.y;*/

            yVal = -(cutNorm.x * v.x + cutNorm.z * v.z + planeD) / cutNorm.y;
            interPoint.y = yVal;

            //Debug.Log("this vertex: " + v);
            //Debug.Log("xDist= " + (v.x - locPos.x) + "; zDist= " + (v.z - locPos.z));
            //Debug.Log("yVal= " + yVal);

            if (condition)
            {
                if (v.y >= interPoint.y)
                {
                    //Debug.Log(i);
                    topV.Add(i);
                }
            }
            else
            {
                if (v.y < interPoint.y)
                {
                    //Debug.Log(i);
                    topV.Add(i);
                }
            }

            i++;
        }

        //Debug.Log(alterMesh.vertices.Length);
        //Debug.Log(alterMesh.triangles.Length);

        i = 0;
        for (int j = 0; j < alterMesh.triangles.Length; j += 3)
        {
            int a = alterMesh.triangles[j];     // always alone
            int b = alterMesh.triangles[j + 1];
            int c = alterMesh.triangles[j + 2];

            Vector3 vertA = alterMesh.vertices[a];
            Vector3 vertB = alterMesh.vertices[b];
            Vector3 vertC = alterMesh.vertices[c];

            bool A = topV.Contains(a);
            bool B = topV.Contains(b);
            bool C = topV.Contains(c);

            if (A & B & C)
            {
                //Debug.Log(string.Format("{0}, {1}, {2}", a, b, c));

                // form triangle
                newTri.Add(i++);
                newTri.Add(i++);
                newTri.Add(i++);

                newV.Add(vertA);
                newV.Add(vertB);
                newV.Add(vertC);

                continue;
            }
            else if (!A & !B & !C)
            {
                // dont form anything
                continue;
            }

            // make it so that A is always the singular vertex
            bool caseA = B == C;
            bool caseB = A == C;
            bool caseC = A == B;

            if (caseB)
            {
                int temp = b;
                b = a;
                a = temp;

                temp = c;
                c = b;
                b = temp;
            }
            else if (caseC)
            {
                int temp = c;
                c = a;
                a = temp;

                temp = c;
                c = b;
                b = temp;
            }

            vertA = alterMesh.vertices[a];
            vertB = alterMesh.vertices[b];
            vertC = alterMesh.vertices[c];

            int d = -1; // vert between A and B
            int e = -1; // vert between A and C


            // INTERPOLATION (HARD) (UNFINISHED)
            float yVal = -(cutNorm.x * vertA.x + cutNorm.z * vertA.z + planeD) / cutNorm.y;

            float facD = 0.5f;
            float facE = 0.5f;

            //Debug.Log("D: " + facD + " E: " + facE);

            float aVal = -(cutNorm.x * vertA.x + cutNorm.z * vertA.z + planeD) / cutNorm.y;
            float bVal = -(cutNorm.x * vertB.x + cutNorm.z * vertB.z + planeD) / cutNorm.y;
            float cVal = -(cutNorm.x * vertC.x + cutNorm.z * vertC.z + planeD) / cutNorm.y;

            facD = Mathf.Abs(aVal - vertA.y) / (Mathf.Abs(aVal - vertA.y) + Mathf.Abs(bVal - vertB.y));
            facE = Mathf.Abs(aVal - vertA.y) / (Mathf.Abs(aVal - vertA.y) + Mathf.Abs(cVal - vertC.y));

            Vector3 vertD = Vector3.Lerp(vertA, vertB, facD);  // (placeholder fac = 0.5)
            Vector3 vertE = Vector3.Lerp(vertA, vertC, facE);  // (placeholder fac = 0.5)

            //Debug.Log(facD);
            //Debug.Log(facE);


            if (!edgeV.Contains(vertD))
                edgeV.Add(vertD);
            if (!edgeV.Contains(vertE))
                edgeV.Add(vertE);


            // choose between the 2 cases, based on whether single vertex is [TOP] or [BOTTOM]
            if (topV.Contains(a))
            {
                newTri.Add(i++);
                newTri.Add(i++);
                newTri.Add(i++);

                newV.Add(vertA);
                newV.Add(vertD);
                newV.Add(vertE);
            }
            else
            {
                newTri.Add(i++);
                newTri.Add(i++);
                newTri.Add(i++);
                newTri.Add(i++);
                newTri.Add(i++);
                newTri.Add(i++);

                newV.Add(vertB);
                newV.Add(vertC);
                newV.Add(vertE);
                newV.Add(vertB);
                newV.Add(vertE);
                newV.Add(vertD);
            }
        }


        Quaternion q = new Quaternion();
        q.SetFromToRotation(condition ? cutNorm : -cutNorm, Vector3.up); // depends which half we're in

        Matrix4x4 rot = Matrix4x4.Rotate(q);
        List<float> deg = new List<float>(); 
        List<Vector3> posits = new List<Vector3>();

        float sumX = 0f;
        float sumZ = 0f;

        // set up the vertices of the new plane
        for (int j = 0; j < edgeV.Count; j++)
        {
            Vector3 temp = rot.MultiplyPoint(edgeV[j]);
            temp.y = 0;
            posits.Add(temp);

            sumX += temp.x;
            sumZ += temp.z;

            //Debug.Log(deg[j]);
            //Debug.Log(sortV[j]);
        }

        sumX /= edgeV.Count;
        sumZ /= edgeV.Count;

        for (int j = 0; j < edgeV.Count; j++)
        {
            Vector3 temp = posits[j];
            temp.x = temp.x + sumX;
            temp.z = temp.z + sumZ;

            temp = temp.normalized;
            deg.Add(Mathf.Rad2Deg * Mathf.Atan2(temp.z, temp.x));
        }

        // sort the vertices of the new plane
        for (int j = 0; j < edgeV.Count - 1; j++)
        {
            for (int k = j; k < edgeV.Count; k++)
            {
                if (deg[j] > deg[k])
                {
                    float tempo = deg[j];
                    deg[j] = deg[k];
                    deg[k] = tempo;

                    Vector3 temp = edgeV[j];
                    edgeV[j] = edgeV[k];
                    edgeV[k] = temp;
                }
            }
            newV.Add(edgeV[j]);
        }
        newV.Add(edgeV[edgeV.Count - 1]);

        newGoats = edgeV;

        int current = i;

        //Debug.Log("STOP THE COUNT: " + edgeV.Count);
        for (int j = 1; j < edgeV.Count - 1; j++)
        {
            newTri.Add(current);
            newTri.Add(current + j);
            newTri.Add(current + j + 1);

            Debug.Log("triangle! -- " + current + "," + (current + j + 1) + "," + (current + j + 1));

            //Debug.Log("the last: " + (current + j + 1) + " out of: " + (newV.Count - 1));
        }



        Debug.Log("TRI COUNT: " + newTri.Count);
        //Debug.Log(newV.ToArray().Length);

        List<Vector2> uvs = new List<Vector2>();

        foreach (var v in newV) // handle UV stuffs
        {
            uvs.Add(v + new Vector3(0.5f, 0.5f, 0.5f));
        }


        alterMesh.Clear();

        alterMesh.vertices = newV.ToArray();
        alterMesh.uv = uvs.ToArray();
        alterMesh.triangles = newTri.ToArray();

        alterMesh.RecalculateNormals();


        Debug.Log("--------------------------");
    }

    #region "AddGeometry"
    public void AddGeometry(bool condition)  // simple heuristic: anything lower than y=0 goes. we create new vertices at that height (all in local for simplicity)\
    {
        if (condition)
        {
            Cut newCutObj = Instantiate(this, this.transform.position, this.transform.rotation);
            newCutObj.PerformCut(false);
        }
        else
        {
            Configure();
        }


        List<Vector3> newV = new List<Vector3>();
        List<int> newTri = new List<int>();

        List<Vector3> edgeV = new List<Vector3>();
        List<int> edgeNum = new List<int>();


        int i = 0;
        List<int> topV = new List<int>();

        foreach (Vector3 v in alterMesh.vertices) // split into top and bot
        {
            if (condition)
            {
                if (v.x > 0f)
                {
                    //Debug.Log(i);
                    topV.Add(i);
                }
            }
            else
            {
                if (v.x < 0f)
                {
                    //Debug.Log(i);
                    topV.Add(i);
                }
            }

            i++;
        }

        //Debug.Log(alterMesh.vertices.Length);
        //Debug.Log(alterMesh.triangles.Length);

        i = 0;
        for (int j = 0; j < alterMesh.triangles.Length; j += 3)
        {
            int a = alterMesh.triangles[j];     // always alone
            int b = alterMesh.triangles[j + 1];
            int c = alterMesh.triangles[j + 2];

            Vector3 vertA = alterMesh.vertices[a];
            Vector3 vertB = alterMesh.vertices[b];
            Vector3 vertC = alterMesh.vertices[c];

            bool A = topV.Contains(a);
            bool B = topV.Contains(b);
            bool C = topV.Contains(c);

            if (A & B & C)
            {
                //Debug.Log(string.Format("{0}, {1}, {2}", a, b, c));

                // form triangle
                newTri.Add(i++);
                newTri.Add(i++);
                newTri.Add(i++);

                newV.Add(vertA);
                newV.Add(vertB);
                newV.Add(vertC);

                continue;
            }
            else if (!A & !B & !C)
            {
                // dont form anything
                continue;
            }

            // make it so that A is always the singular vertex
            bool caseA = B == C;
            bool caseB = A == C;
            bool caseC = A == B;

            if (caseB)
            {
                int temp = b;
                b = a;
                a = temp;

                temp = c;
                c = b;
                b = temp;
            }
            else if (caseC)
            {
                int temp = c;
                c = a;
                a = temp;

                temp = c;
                c = b;
                b = temp;
            }

            vertA = alterMesh.vertices[a];
            vertB = alterMesh.vertices[b];
            vertC = alterMesh.vertices[c];

            int d = -1; // vert between A and B
            int e = -1; // vert between A and C

            Vector3 vertD = Vector3.Lerp(vertA, vertB, 0.5f);  // NOT FINAL
            Vector3 vertE = Vector3.Lerp(vertA, vertC, 0.5f);  // NOT FINAL

            Debug.Log(vertD);
            Debug.Log(vertE);

            // choose between the 2 cases: single vertex is [TOP] or [BOTTOM]

            if (topV.Contains(a))
            {
                newTri.Add(i++);
                newTri.Add(i++);
                newTri.Add(i++);

                newV.Add(vertA);
                newV.Add(vertD);
                newV.Add(vertE);

                Debug.Log(vertA);
            }
            else 
            {
                newTri.Add(i++);
                newTri.Add(i++);
                newTri.Add(i++); 
                newTri.Add(i++);
                newTri.Add(i++);
                newTri.Add(i++);

                newV.Add(vertB);
                newV.Add(vertC);
                newV.Add(vertE);
                newV.Add(vertB);
                newV.Add(vertE);
                newV.Add(vertD);
            }
        }

        //Debug.Log(newTri.Count);
        //Debug.Log(newV.ToArray().Length);


        List<Vector2> uvs = new List<Vector2>();

        foreach (var v in newV)
        {
            uvs.Add(v + new Vector3(0.5f, 0.5f, 0.5f));
        }


        alterMesh.Clear();

        alterMesh.vertices = newV.ToArray();
        alterMesh.uv = uvs.ToArray();
        alterMesh.triangles = newTri.ToArray();

        alterMesh.RecalculateNormals();
    }
    #endregion




    /*void AddGeometry_Backup()  // backup version
    {
        List<Vector3> newV = new List<Vector3>();
        List<int> newTri = new List<int>();

        List<Vector3> edgeV = new List<Vector3>();
        List<int> edgeNum = new List<int>();


        int i = 0;
        List<int> topV = new List<int>();

        foreach (Vector3 v in alterMesh.vertices) // split into top and bot
        {
            if (v.y > 0)
            {
                //Debug.Log(i);
                topV.Add(i);
            }
            i++;
        }

        //Debug.Log(alterMesh.vertices.Length);
        //Debug.Log(alterMesh.triangles.Length);

        i = 0;
        for (int j = 0; j < alterMesh.triangles.Length; j += 3)
        { 
            int a = alterMesh.triangles[j];     // always alone
            int b = alterMesh.triangles[j + 1];
            int c = alterMesh.triangles[j + 2];

            Vector3 vertA = alterMesh.vertices[a];
            Vector3 vertB = alterMesh.vertices[b];
            Vector3 vertC = alterMesh.vertices[c];

            bool A = topV.Contains(a);
            bool B = topV.Contains(b);
            bool C = topV.Contains(c);

            if (A & B & C)
            {
                //Debug.Log(string.Format("{0}, {1}, {2}", a, b, c));

                // form triangle
                newTri.Add(i++);
                newTri.Add(i++);
                newTri.Add(i++);

                newV.Add(vertA);
                newV.Add(vertB);
                newV.Add(vertC);

                continue;
            }
            else if (!A & !B & !C)
            {
                // dont form anything
                continue;
            }

            // make it so that A is always the singular vertex
            bool caseA = B == C;
            bool caseB = A == C;
            bool caseC = A == B;

            if (caseB)
            {
                int temp = b;
                b = a;
                a = temp;

                temp = c;
                c = b;
                b = temp;
            }
            else if (caseC)
            {
                int temp = c;
                c = a;
                a = temp;

                temp = c;
                c = b;
                b = temp;
            }


            int d = -1; // vert between A and B
            int e = -1; // vert between A and C

            Vector3 vertD = Vector3.Lerp(vertA, vertB, (0f - vertA.y) / Vector3.Magnitude(vertB - vertA));  // NOT FINAL
            Vector3 vertE = Vector3.Lerp(vertA, vertC, (0f - vertA.y) / Vector3.Magnitude(vertC - vertA));  // NOT FINAL

            vertD.y = 0;
            vertE.y = 0;

            Debug.Log(vertD);
            Debug.Log(vertE);

            // choose between the 2 cases: single vertex is [TOP] or [BOTTOM]

            if (topV.Contains(a))
            {
                newTri.Add(i++);
                newTri.Add(i++);
                newTri.Add(i++);

                newV.Add(vertA);
                newV.Add(vertD);
                newV.Add(vertE);
            }
            else 
            {
                newTri.Add(i++);
                newTri.Add(i++);
                newTri.Add(i++); 
                newTri.Add(i++);
                newTri.Add(i++);
                newTri.Add(i++);

                newV.Add(vertB);
                newV.Add(vertC);
                newV.Add(vertE);
                newV.Add(vertB);
                newV.Add(vertE);
                newV.Add(vertD);
            }
        }

        Debug.Log(newTri.Count);

        alterMesh.triangles = newTri.ToArray();
        alterMesh.vertices = newV.ToArray();

        alterMesh.RecalculateNormals();
    }

    void BasicHalfing()
    {
        Vector3[] verts = alterMesh.vertices;
        int i = 0;

        foreach (Vector3 v in alterMesh.vertices)
        {
            //Debug.Log(v);
            if (v.y < 0)
            {
                verts[i].y = 0;
            }
            i++;
        }

        alterMesh.vertices = verts;
    }*/


    /*private void OnTriggerEnter(Collider other)
    {
        PerformCut(true);
        other.enabled = false;
    }*/
}

public static class Vector3Ext
{
    public static Vector3 reciprocal(this Vector3 input)
    {
        return new Vector3(1f / input.x, 1f / input.y, 1f / input.z);
    }
}
