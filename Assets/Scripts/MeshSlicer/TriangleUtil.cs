using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

public class TriangleUtil
{

    /// <summary>
    /// Search for duplicate edges?  Optimize using a hash
    /// </summary>
    /// <param name="indexA"></param>
    /// <param name="indexB"></param>
    /// <param name="classes"></param>
    /// <param name="verts"></param>
    public static void SetEdgeHash(int indexA, int indexB, ClassificationUtil.Classification[] classes, Vector3[] verts)
    {
        if (classes[indexA] == ClassificationUtil.Classification.COINCIDING && classes[indexB] == ClassificationUtil.Classification.COINCIDING)
        {
            if (!MeshSlicer.hashCheck.ContainsKey(verts[indexA] + verts[indexB]))
            {
                MeshSlicer.hashCheck.Add(verts[indexA] + verts[indexB], 0);
                MeshSlicer.debugEdges.Add(new Vector3[] { verts[indexA], verts[indexB] });
            }
        }
    }

    public static void SetEdge(int indexA, int indexB, ClassificationUtil.Classification[] classes, Vector3[] points)
    {
        bool found = false;
        if (classes[indexA] == ClassificationUtil.Classification.COINCIDING && classes[indexB] == ClassificationUtil.Classification.COINCIDING)
        {
            foreach (Vector3[] edges in MeshSlicer.debugEdges)
            {
                if (edges[0] == points[indexA] && edges[1] == points[indexB] || edges[1] == points[indexA] && edges[0] == points[indexB])
                {
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                MeshSlicer.debugEdges.Add(new Vector3[] { points[indexA], points[indexB] });
            }
        }
    }

	public static void SetEdge(int indexA, int indexB, ClassificationUtil.Classification[] classes, Triangle3D triangle)
	{
		bool found = false;
		if(classes[indexA] == ClassificationUtil.Classification.COINCIDING && classes[indexB] == ClassificationUtil.Classification.COINCIDING)
		{
			foreach(Vector3[] edges in MeshSlicer.debugEdges)
			{
				if(edges[0] == triangle.vertices[indexA].pos && edges[1] == triangle.vertices[indexB].pos || edges[1] == triangle.vertices[indexA].pos && edges[0] == triangle.vertices[indexB].pos)
				{
					found = true;
					break;
				}
			}
			
			if(!found)
			{
				MeshSlicer.debugEdges.Add(new Vector3[] { triangle.vertices[indexA].pos, triangle.vertices[indexB].pos });
			}
		}
	}

	public static Triangle3D[] SplitTriangleWithPlane(Triangle3D triangle, Plane plane, float e, out ClassificationUtil.Classification side)
	{
		return SplitTriangleWithPlane(triangle, plane, e, out side, true);
	}

    public static void SplitTriangleWithPlane(ref int vertexCount, Vector3[] points, int[] indices, Plane plane, float e, out ClassificationUtil.Classification side, bool cap, out int[] newTris, out Vector3[] newPoints)
    {
        ClassificationUtil.Classification[] classes;
        side = ClassificationUtil.ClassifyPoints(points, plane, out classes, e);

        if (side != ClassificationUtil.Classification.STRADDLE)
        {
            if (cap)
            {
                SetEdgeHash(0, 1, classes, points);
                SetEdgeHash(1, 2, classes, points);
                SetEdgeHash(2, 0, classes, points);
            }
            newTris = null;
            newPoints = null;
            return;
        }

        int totalVertexCount = vertexCount;

        //int iA;
        Vector3 pA;
        //Vector3 normA;
        //Vector2 uvA;
        //Vector4 tA;

        uint aLength = 0;
        int[] indicesA = new int[4];
        Vector3[] verticesA = new Vector3[4];
        //Vector3[] normalsA = new Vector3[4];
        //Vector2[] uvsA = new Vector2[4];
        //Vector4[] tangentsA = new Vector4[4];

        int iB;
        Vector3 pB;
        //Vector3 normB;
        //Vector2 uvB;
        //Vector4 tB;

        uint bLength = 0;
        int[] indicesB = new int[4];
        Vector3[] verticesB = new Vector3[4];
        //Vector3[] normalsB = new Vector3[4];
        //Vector2[] uvsB = new Vector2[4];
        //Vector4[] tangentsB = new Vector4[4];

        float sideA;
        float sideB;
        Intersection isect;
        //Vector2 newUV;

        List<Vector3> cVerts = new List<Vector3>();

        //int[] indices = triangles;
        //Triangle3D.Vertex[] points
        //Vector3[] normals = new Vector3[] { triangle.nv0, triangle.nv1, triangle.nv2 };
        //Vector2[] uvs = new Vector2[] { triangle.uv0, triangle.uv1, triangle.uv2 };
        //Vector4[] tangents = new Vector4[] { triangle.tv0, triangle.tv1, triangle.tv2 };

        float[] distance = new float[3];
        for (int i = 0; i < points.Length; i++)
        {
            distance[i] = plane.GetDistanceToPoint(points[i]);
        }

        for (int i = 0; i < points.Length; i++)
        {
            int j = (i + 1) % points.Length;

            //iA = indices[i];
            iB = indices[j];

            pA = points[i];
            pB = points[j];

            //uvA = uvs[i];
            //uvB = uvs[j];

            //normA = normals[i];
            //normB = normals[j];

            //tA = tangents[i];
            //tB = tangents[j];

            sideA = distance[i];
            sideB = distance[j];


            if (sideB > e)
            {
                if (sideA < -e)
                {
                    isect = Intersection.LinePlane(pA, pB, plane, e, null);
                    if (isect.status != Intersection.IntersectionType.INTERSECTION)
                    {
                        SplitTriangleWithPlane(ref totalVertexCount, points, indices, new Plane(plane.normal, plane.distance + 1.0f), e, out side, cap, out newTris, out newPoints);
                    }

                    indicesA[aLength] = totalVertexCount;
                    indicesB[bLength] = totalVertexCount;
                    totalVertexCount++;

                    verticesA[aLength] = isect.vert;
                    verticesB[bLength] = isect.vert;

                    //newUV = InterpolateUV(uvA, uvB, isect.alpha);
                    //uvsA[aLength] = newUV;
                    //uvsB[bLength] = newUV;

                    //tangentsA[aLength] = tB;
                    //tangentsB[bLength] = tB;

                    //normalsA[aLength] = Vector3.Lerp(normA, normB, isect.alpha);
                    //normalsB[bLength] = Vector3.Lerp(normA, normB, isect.alpha);

                    aLength++;
                    bLength++;

                    if (!cVerts.Contains(isect.vert))
                    {
                        cVerts.Add(isect.vert);
                    }
                }

                indicesA[aLength] = iB;
                verticesA[aLength] = pB;
                //uvsA[aLength] = uvB;
                //normalsA[aLength] = normB;
                //tangentsA[aLength] = tB;

                aLength++;
            }

            else if (sideB < -e)
            {
                if (sideA > e)
                {
                    isect = Intersection.LinePlane(pA, pB, plane, e, null);
                    if (isect.status != Intersection.IntersectionType.INTERSECTION)
                    {
                        SplitTriangleWithPlane(ref totalVertexCount, points, indices, new Plane(plane.normal, plane.distance + 1.0f), e, out side, cap, out newTris, out newPoints);
                    }

                    indicesA[aLength] = totalVertexCount;
                    indicesB[bLength] = totalVertexCount;
                    totalVertexCount++;

                    verticesA[aLength] = isect.vert;
                    verticesB[bLength] = isect.vert;

                    //newUV = InterpolateUV(uvA, uvB, isect.alpha);
                    //uvsA[aLength] = newUV;
                    //uvsB[bLength] = newUV;

                    //tangentsA[aLength] = tB;
                    //tangentsB[bLength] = tB;

                    //normalsA[aLength] = Vector3.Lerp(normA, normB, isect.alpha);
                    //normalsB[bLength] = Vector3.Lerp(normA, normB, isect.alpha);

                    aLength++;
                    bLength++;

                    if (!cVerts.Contains(isect.vert))
                    {
                        cVerts.Add(isect.vert);
                    }
                }

                indicesB[bLength] = iB;
                verticesB[bLength] = pB;
                //uvsB[bLength] = uvB;
                //normalsB[bLength] = normB;
                //tangentsB[bLength] = tB;

                bLength++;
            }
            else
            {
                indicesA[aLength] = iB;
                verticesA[aLength] = pB;
                //uvsA[aLength] = uvB;
                //normalsA[aLength] = normB;
                //tangentsA[aLength] = tB;

                aLength++;

                indicesB[bLength] = iB;
                verticesB[bLength] = pB;
                //uvsB[bLength] = uvB;
                //normalsB[bLength] = normB;
                //tangentsB[bLength] = tB;

                bLength++;

                cVerts.Add(pB);

                if (!cVerts.Contains(pB))
                {
                    cVerts.Add(pB);
                }
            }
        }

        for (int i = 0; i < cVerts.Count - 1; i++)
        {
            MeshSlicer.debugEdges.Add(new Vector3[] { cVerts[i], cVerts[i + 1] });
        }

        if (aLength > 3 || bLength > 3)
        {
            newTris = new int[3 * 3];
            newPoints = new Vector3[3 * 3];
        }
        else
        {
            newTris = new int[3 * 2];
            newPoints = new Vector3[3 * 2];
        }

        newPoints[0] = verticesA[0];
        newPoints[1] = verticesA[1];
        newPoints[2] = verticesA[2];
        newTris[0] = indicesA[0];
        newTris[1] = indicesA[1];
        newTris[2] = indicesA[2];

        newPoints[3] = verticesB[0];
        newPoints[4] = verticesB[1];
        newPoints[5] = verticesB[2];
        newTris[3] = indicesB[0];
        newTris[4] = indicesB[1];
        newTris[5] = indicesB[2];

        if (aLength > 3)
        {
            newPoints[6] = verticesA[0];
            newPoints[7] = verticesA[2];
            newPoints[8] = verticesA[3];
            newTris[6] = indicesA[0];
            newTris[7] = indicesA[2];
            newTris[8] = indicesA[3];
        }
        else if (bLength > 3)
        {
            newPoints[6] = verticesB[0];
            newPoints[7] = verticesB[2];
            newPoints[8] = verticesB[3];
            newTris[6] = indicesB[0];
            newTris[7] = indicesB[2];
            newTris[8] = indicesB[3];
        }

        vertexCount = totalVertexCount;
    }

	public static Triangle3D[] SplitTriangleWithPlane(Triangle3D triangle, Plane plane, float e, out ClassificationUtil.Classification side, bool cap)
	{
		
		ClassificationUtil.Classification[] classes;
		side = ClassificationUtil.ClassifyTriangle(triangle, plane, out classes, e);
		
		
		if(side != ClassificationUtil.Classification.STRADDLE)
		{
			if(cap)
			{
                SetEdge(0, 1, classes, triangle.pos);
                SetEdge(1, 2, classes, triangle.pos);
                SetEdge(2, 0, classes, triangle.pos);
			}
			return null;
		}
		
		//int iA;
		Triangle3D.Vertex pA;
		Vector3 normA;
		Vector2 uvA;
		//Vector4 tA;
		
		uint aLength = 0;
		int[] indicesA = new int[4];
		Triangle3D.Vertex[] verticesA = new Triangle3D.Vertex[4];
		Vector3[] normalsA = new Vector3[4];
		Vector2[] uvsA = new Vector2[4];
		Vector4[] tangentsA = new Vector4[4];
		
		int iB;
		Triangle3D.Vertex pB;
		Vector3 normB;
		Vector2 uvB;
		Vector4 tB;
		
		uint bLength = 0;
		int[] indicesB = new int[4];
		Triangle3D.Vertex[] verticesB = new Triangle3D.Vertex[4];
		Vector3[] normalsB = new Vector3[4];
		Vector2[] uvsB = new Vector2[4];
		Vector4[] tangentsB = new Vector4[4];
		
		float sideA;
		float sideB;
		Intersection isect;
		Vector2 newUV;
		
		List<Vector3> cVerts = new List<Vector3> ();
		
		int[] indices = new int[] { triangle.idxV0, triangle.idxV1, triangle.idxV2 };
		Triangle3D.Vertex[] points = new Triangle3D.Vertex[] { triangle.v0, triangle.v1, triangle.v2 };
		Vector3[] normals = new Vector3[] { triangle.nv0, triangle.nv1, triangle.nv2 };
		Vector2[] uvs = new Vector2[] { triangle.uv0, triangle.uv1, triangle.uv2 };
		Vector4[] tangents = new Vector4[] { triangle.tv0, triangle.tv1, triangle.tv2 };
		
		float[] distance = new float[3];
		for(int i = 0; i < points.Length; i++)
		{
			distance[i] = plane.GetDistanceToPoint(points[i].pos);
		}
		
		for(int i = 0; i < points.Length; i++)
		{
			int j = (i + 1) % points.Length;
			
			//iA = indices[i];
			iB = indices[j];
			
			pA = points[i];
			pB = points[j];
			
			uvA = uvs[i];
			uvB = uvs[j];
			
			normA = normals[i];
			normB = normals[j];
			
			//tA = tangents[i];
			tB = tangents[j];
			
			sideA = distance[i];
			sideB = distance[j];
			
			
			if(sideB > e)
			{
				if(sideA < -e)
				{
					isect = Intersection.LinePlane(pA.pos, pB.pos, plane, e, null);
					if(isect.status != Intersection.IntersectionType.INTERSECTION)
					{
						plane.distance += Mathf.Epsilon;
						return SplitTriangleWithPlane(triangle, new Plane (plane.normal, plane.distance + 1.0f), e, out side, cap);
					}
					
					// New vertex was created
					int newIndex = triangle.meshVertices.Count;
					triangle.meshVertices.Add(new Triangle3D.Vertex (isect.vert));
					
					indicesA[aLength] = newIndex;
					indicesB[bLength] = newIndex;
					
					verticesA[aLength] = new Triangle3D.Vertex (isect.vert);
					verticesB[bLength] = new Triangle3D.Vertex (isect.vert);

					
					newUV = InterpolateUV(uvA, uvB, isect.alpha);
					uvsA[aLength] = newUV;
					uvsB[bLength] = newUV;
					
					tangentsA[aLength] = tB;
					tangentsB[bLength] = tB;
					
					normalsA[aLength] = Vector3.Lerp(normA, normB, isect.alpha);
					normalsB[bLength] = Vector3.Lerp(normA, normB, isect.alpha);
					
					aLength++;
					bLength++;
					
					if(!cVerts.Contains(isect.vert))
					{
						cVerts.Add(isect.vert);
					}
				}
				
				indicesA[aLength] = iB;
				verticesA[aLength] = pB;
				uvsA[aLength] = uvB;
				normalsA[aLength] = normB;
				tangentsA[aLength] = tB;
				
				aLength++;
			}

			else if(sideB < -e)
			{
				if(sideA > e)
				{
					isect = Intersection.LinePlane(pA.pos, pB.pos, plane, e, null);
					if(isect.status != Intersection.IntersectionType.INTERSECTION)
					{
                        return SplitTriangleWithPlane(triangle, new Plane(plane.normal, plane.distance + 1.0f), e, out side, cap);
					}
					
					// New vertex was created
					int newIndex = triangle.meshVertices.Count;
					triangle.meshVertices.Add(new Triangle3D.Vertex (isect.vert));
					
					indicesA[aLength] = newIndex;
					indicesB[bLength] = newIndex;
					
					verticesA[aLength] = new Triangle3D.Vertex (isect.vert);
					verticesB[bLength] = new Triangle3D.Vertex (isect.vert);
					
					newUV = InterpolateUV(uvA, uvB, isect.alpha);
					uvsA[aLength] = newUV;
					uvsB[bLength] = newUV;
					
					tangentsA[aLength] = tB;
					tangentsB[bLength] = tB;
					
					normalsA[aLength] = Vector3.Lerp(normA, normB, isect.alpha);
					normalsB[bLength] = Vector3.Lerp(normA, normB, isect.alpha);
					
					aLength++;
					bLength++;
					
					if(!cVerts.Contains(isect.vert))
					{
						cVerts.Add(isect.vert);
					}
				}
				
				indicesB[bLength] = iB;
				verticesB[bLength] = pB;
				uvsB[bLength] = uvB;
				normalsB[bLength] = normB;
				tangentsB[bLength] = tB;
				
				bLength++;
			}
			else
			{
				indicesA[aLength] = iB;
				verticesA[aLength] = pB;
				uvsA[aLength] = uvB;
				normalsA[aLength] = normB;
				tangentsA[aLength] = tB;
				
				aLength++;
				
				indicesB[bLength] = iB;
				verticesB[bLength] = pB;
				uvsB[bLength] = uvB;
				normalsB[bLength] = normB;
				tangentsB[bLength] = tB;
				
				bLength++;
				
				cVerts.Add(pB.pos);
				
				if(!cVerts.Contains(pB.pos))
				{
					cVerts.Add(pB.pos);
				}
			}
		}
		
		for(int i = 0; i < cVerts.Count - 1; i++)
		{
			MeshSlicer.debugEdges.Add(new Vector3[] { cVerts[i], cVerts[i + 1] });
		}
		
		Triangle3D[] tris;
		if(aLength > 3 || bLength > 3)
		{
			tris = new Triangle3D[3];
		}

		else
		{
			tris = new Triangle3D[2];
		}
		
		tris[0] = new Triangle3D (triangle.meshVertices, new Triangle3D.Vertex[] { verticesA[0], verticesA[1], verticesA[2] }, new Vector3[] { normalsA[0], normalsA[1], normalsA[2] }, new Vector2[] { uvsA[0], uvsA[1], uvsA[2] }, new Vector4[] { tangentsA[0], tangentsA[1], tangentsA[2] }, new int[] { indicesA[0], indicesA[1], indicesA[2] }, triangle.subMeshGroup);
		
		tris[1] = new Triangle3D (triangle.meshVertices, new Triangle3D.Vertex[] { verticesB[0], verticesB[1], verticesB[2] }, new Vector3[] { normalsB[0], normalsB[1], normalsB[2] }, new Vector2[] { uvsB[0], uvsB[1], uvsB[2] }, new Vector4[] { tangentsB[0], tangentsB[1], tangentsB[2] }, new int[] { indicesB[0], indicesB[1], indicesB[2] }, triangle.subMeshGroup);
		
		if(aLength > 3)
		{
			tris[2] = new Triangle3D (triangle.meshVertices, new Triangle3D.Vertex[] { verticesA[0], verticesA[2], verticesA[3] }, new Vector3[] { normalsA[0], normalsA[2], normalsA[3] }, new Vector2[] { uvsA[0], uvsA[2], uvsA[3] }, new Vector4[] { tangentsA[0], tangentsA[2], tangentsA[3] }, new int[] { indicesA[0], indicesA[2], indicesA[3] }, triangle.subMeshGroup);
			
			
		}

		else if(bLength > 3)
		{
			tris[2] = new Triangle3D (triangle.meshVertices, new Triangle3D.Vertex[] { verticesB[0], verticesB[2], verticesB[3] }, new Vector3[] { normalsB[0], normalsB[2], normalsB[3] }, new Vector2[] { uvsB[0], uvsB[2], uvsB[3] }, new Vector4[] { tangentsB[0], tangentsB[2], tangentsB[3] }, new int[] { indicesB[0], indicesB[2], indicesB[3] }, triangle.subMeshGroup);
		}
		return tris;
	}

	public static Vector2 InterpolateUV(Vector2 a, Vector2 b, float alpha)
	{
		Vector2 dst = new Vector2 ();
		dst.x = a.x + alpha * (b.x - a.x);
		dst.y = a.y + alpha * (b.y - a.y);
		return dst;
	}
	
}

