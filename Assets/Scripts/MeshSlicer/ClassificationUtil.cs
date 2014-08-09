using UnityEngine;
using System.Collections;

public class ClassificationUtil
{
    public enum Classification
    {
        UNDEFINED,
        FRONT,
        BACK,
        COINCIDING,
        STRADDLE
    }

    public static Classification ClassifyPoint(Vector3 point, Plane plane, float e)
    {
        float distance = plane.GetDistanceToPoint(point);
		
        if (distance < -e)
        {
            return Classification.BACK;
        }
        else if (distance > e)
        {
            return Classification.FRONT;
        }
        else
        {
            return Classification.COINCIDING;
        }
    }

    public static Classification ClassifyPoints(Vector3[] points, Plane plane, out Classification[] classes, float e)
    {		
        uint numpos = 0;
        uint numneg = 0;
        classes = new Classification[3];
        for(int i = 0; i < points.Length; i++)
        {
            float distance = plane.GetDistanceToPoint(points[i]);

            if (distance < -e)
            {
                classes[i] = Classification.BACK;
                numneg++;
            }
            else if (distance > e)
            {
                classes[i] = Classification.FRONT;
                numpos++;
            }
            else
            {
                classes[i] = Classification.COINCIDING;
            }
        }

        if (numpos > 0 && numneg == 0)
        {
            return Classification.FRONT;
        }
        else if (numpos == 0 && numneg > 0)
        {
            return Classification.BACK;
        }
        else if (numpos > 0 && numneg > 0)
        {
            return Classification.STRADDLE;
        }
        else
        {
            return Classification.COINCIDING;
        }
    }


    public static Classification ClassifyTriangle(Vector3[] points, Plane plane, out Classification[] classes, float e)
    {
        if (points == null)
        {
            classes = null;
            return Classification.UNDEFINED;
        }
        return ClassifyPoints(points, plane, out classes, e);
    }	
    	
    public static Classification ClassifyTriangle(Triangle3D triangle, Plane plane, out Classification[] classes, float e)
    {
        if (triangle == null)
        {
            classes = null;
            return Classification.UNDEFINED;
        }
        return ClassifyPoints(triangle.pos, plane, out classes, e);
    }	
}
