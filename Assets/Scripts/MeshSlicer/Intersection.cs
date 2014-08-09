using UnityEngine;
using System.Collections;

public class Intersection
{

    public enum IntersectionType
    {
        NONE,
        INTERSECTION,
        PARALLEL
    }

    public Vector3 point;
    public Vector3 vert;
    public float alpha = 0;
    public IntersectionType status;

    public Intersection()
    {
        this.point = new Vector3();
        this.vert = new Vector3();
    }

    public Intersection(Vector3 point, Vector3 vert)
    {
        this.point = point;
        this.vert = vert;
    }

    public static Intersection LinePlane(Vector3 pA, Vector3 pB, Plane plane, float e, Intersection dst)
    {
        if (dst == null)
        {
            dst = new Intersection();
        }
        float a = plane.normal.x;
        float b = plane.normal.y;
        float c = plane.normal.z;
        float d = plane.distance;
        float x1 = pA.x;
        float y1 = pA.y;
        float z1 = pA.z;
        float x2 = pB.x;
        float y2 = pB.y;
        float z2 = pB.z;

        float r0 = (a * x1) + (b * y1) + (c * z1) + d;
        float r1 = a * (x1 - x2) + b * (y1 - y2) + c * (z1 - z2);
        float u = r0 / r1;

        if (Mathf.Abs(u) < e)
        {
            dst.status = IntersectionType.PARALLEL;
        }
        else if ((u > 0 && u < 1))
        {
            dst.status = IntersectionType.INTERSECTION;
            Vector3 pt = dst.point;
            pt.x = x2 - x1;
            pt.y = y2 - y1;
            pt.z = z2 - z1;
            pt.x *= u;
            pt.y *= u;
            pt.z *= u;
            pt.x += x1;
            pt.y += y1;
            pt.z += z1;

            dst.alpha = u;

            dst.vert.x = pt.x;
            dst.vert.y = pt.y;
            dst.vert.z = pt.z;
        }
        else
        {
            dst.status = IntersectionType.NONE;
        }
        return dst;
    }
}
