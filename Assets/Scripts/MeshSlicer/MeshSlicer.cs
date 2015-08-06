using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

public class MeshSlicer : MonoBehaviour
{
    public class SplitMesh
    {
        public List<Vector3> vertices;
        public List<Triangle3D> triangles;
        public List<List<Triangle3D.Vertex>> capVertGroups;


        public SplitMesh()
        {
            vertices = new List<Vector3>();
            triangles = new List<Triangle3D>();
        }

        public SplitMesh(List<Vector3> vertices, List<Triangle3D> triangles, List<List<Triangle3D.Vertex>> capVertGroups)
        {
            this.vertices = vertices;
            this.triangles = triangles;
            this.capVertGroups = capVertGroups;
        }

        public void Add(List<Vector3> newVertices, List<Triangle3D> newTriangles)
        {
            foreach (Triangle3D newTriangle in newTriangles)
            {
                newTriangle.idxV0 += triangles.Count;
                newTriangle.idxV1 += triangles.Count;
                newTriangle.idxV2 += triangles.Count;
            }

            triangles.AddRange(newTriangles);
            vertices.AddRange(newVertices);
        }

        public void Add(SplitMesh mesh)
        {
            foreach (Triangle3D newTriangle in mesh.triangles)
            {
                newTriangle.idxV0 += triangles.Count;
                newTriangle.idxV1 += triangles.Count;
                newTriangle.idxV2 += triangles.Count;
            }

            triangles.AddRange(mesh.triangles);
            vertices.AddRange(mesh.vertices);
        }
    }

    public static List<Vector3> debugPolyLoop;
    public static List<Vector3> debugEdgePoints;
    public static List<Vector3[]> debugLoopEdgePoints;
    public static List<Vector3[]> debugEdges;

    public static void CloneMesh(Mesh sourceMesh, Mesh clonedMesh)
    {
        clonedMesh.Clear();
        clonedMesh.subMeshCount = sourceMesh.subMeshCount;
        clonedMesh.vertices = (Vector3[])sourceMesh.vertices.Clone();
        clonedMesh.uv = (Vector2[])sourceMesh.uv.Clone();
        clonedMesh.uv2 = (Vector2[])sourceMesh.uv2.Clone();
        clonedMesh.uv2 = (Vector2[])sourceMesh.uv2.Clone();
        clonedMesh.normals = (Vector3[])sourceMesh.normals.Clone();
        clonedMesh.tangents = new Vector4[clonedMesh.vertices.Length];
        for (int i = 0; i < sourceMesh.subMeshCount; i++)
        {
            clonedMesh.SetTriangles(sourceMesh.GetTriangles(i), i);
        }
    }

    public static Mesh[] ChunkMesh(GameObject sourceGameObject, Mesh[] slicedMeshes, int verticalSlices, int horizontalSlices, bool cap)
    {
        Mesh sourceMesh = sourceGameObject.GetComponent<MeshFilter>().mesh;

        Bounds bounds = sourceGameObject.GetComponent<Renderer>().bounds;
        Vector3 minBounds = bounds.min;
        Vector3 maxBounds = bounds.max;

        GameObject clipperPlane = GameObject.CreatePrimitive(PrimitiveType.Plane);

        if (verticalSlices == 1)
        {
            MeshSlicer.SliceHorz(sourceGameObject, clipperPlane, sourceMesh, slicedMeshes, 0, horizontalSlices, bounds, cap);
        }
        else
        {
            for (int j = 0; j < verticalSlices - 1; j++)
            {
                clipperPlane.transform.rotation = Quaternion.Euler(0, 0, 0);
                clipperPlane.transform.position = new Vector3(0, Mathf.Lerp(maxBounds.y, minBounds.y, (float)(j + 1) / (float)verticalSlices), 0);

                Mesh[] vertClipMeshes = new Mesh[2];
                vertClipMeshes[0] = slicedMeshes[j * horizontalSlices];
                vertClipMeshes[1] = slicedMeshes[(j + 1) * horizontalSlices];

                MeshSlicer.CutTriangleMesh(vertClipMeshes, sourceMesh, new Plane(clipperPlane.transform.up, clipperPlane.transform.position), sourceGameObject.transform, clipperPlane.transform, cap);

                MeshSlicer.SliceHorz(sourceGameObject, clipperPlane, slicedMeshes[j * horizontalSlices], slicedMeshes, j * horizontalSlices, horizontalSlices, bounds, cap);

                sourceMesh = vertClipMeshes[1];
            }
            MeshSlicer.SliceHorz(sourceGameObject, clipperPlane, slicedMeshes[(verticalSlices - 1) * horizontalSlices], slicedMeshes, (verticalSlices - 1) * horizontalSlices, horizontalSlices, bounds, cap);
        }

        Destroy(clipperPlane);
        return slicedMeshes;
    }

    public static void SliceHorz(GameObject sourceGameObject, GameObject clipperPlane, Mesh targetMesh, Mesh[] slicedMeshes, int idx, int slices, Bounds bounds, bool cap)
    {
        Vector3 minBounds = bounds.min;
        Vector3 maxBounds = bounds.max;

        Mesh cuttingMesh = targetMesh;
        Mesh[] slicedHorzMeshes = new Mesh[slices];
        for (int i = 0; i < slices; i++)
        {
            slicedHorzMeshes[i] = new Mesh();
        }

        clipperPlane.transform.rotation = Quaternion.Euler(0, 0, 90);
        for (int i = 0; i < slices - 1; i++)
        {
            clipperPlane.transform.position = new Vector3(Mathf.Lerp(minBounds.x, maxBounds.x, (float)(i + 1) / (float)slices), 0, 0);

            Mesh[] horzClipMeshes = new Mesh[2];
            horzClipMeshes[0] = slicedHorzMeshes[i];
            horzClipMeshes[1] = slicedHorzMeshes[i + 1];

            if (MeshSlicer.CutTriangleMesh(horzClipMeshes, cuttingMesh, new Plane(clipperPlane.transform.up, clipperPlane.transform.position), sourceGameObject.transform, clipperPlane.transform, cap))
            {
                cuttingMesh = horzClipMeshes[1];
            }
        }

        for (int i = 0; i < slices; i++)
        {
            slicedMeshes[idx + i].Clear();
            slicedMeshes[idx + i] = slicedHorzMeshes[i];
        }
    }

    public static void CapMesh(out List<Triangle3D> triangleBucketList, out List<Triangle3D.Vertex> vertexBucketList, Triangle3D.Vertex[] sortedVerts, Transform objectTransform, Transform planeTransform, int triOffset)
    {
        triangleBucketList = new List<Triangle3D>();
        vertexBucketList = new List<Triangle3D.Vertex>();

        if (sortedVerts.Length < 3)
        {
            return;
        }

        Vector3 planeUp = planeTransform.up;

        // Project points to the plane and add them to the vertex bucket
        List<Vector3> projectedPoints = new List<Vector3>();
        Matrix4x4 mat = Matrix4x4.TRS(objectTransform.position, Quaternion.Inverse(planeTransform.rotation), Vector3.one);
        for (int i = 0; i < sortedVerts.Length; i++)
        {
            Vector3 planePoint = mat.MultiplyPoint(sortedVerts[i].pos);
            projectedPoints.Add(new Vector3(planePoint.x, planePoint.z, 0));
            vertexBucketList.Add(new Triangle3D.Vertex(objectTransform.InverseTransformPoint(sortedVerts[i].pos)));
        }

        // Triangulate
        int[] indices = Triangulator.Triangulate(projectedPoints.ToArray());

        Vector3 average = Vector3.zero;
        // Calculate planar mapping
        float maxX = average.x;
        float minX = average.x;
        float maxZ = average.y;
        float minZ = average.y;
        for (int i = 0; i < projectedPoints.Count; i++)
        {
            maxX = Mathf.Max(projectedPoints[i].x, maxX);
            minX = Mathf.Min(projectedPoints[i].x, minX);
            maxZ = Mathf.Max(projectedPoints[i].y, maxZ);
            minZ = Mathf.Min(projectedPoints[i].y, minZ);

        }
        float nMaxX = maxX - minX;
        float nMaxZ = maxZ - minZ;

        // Created triangles
        for (int i = 0; i < indices.Length; i += 3)
        {
            int idx0 = indices[i + 0];
            int idx1 = indices[i + 1];
            int idx2 = indices[i + 2];

            Vector2 UV1 = new Vector2((projectedPoints[idx0].x - minX) / nMaxX, (projectedPoints[idx0].y - minZ) / nMaxZ);

            Vector2 UV2 = new Vector2((projectedPoints[idx1].x - minX) / nMaxX, (projectedPoints[idx1].y - minZ) / nMaxZ);

            Vector2 UV3 = new Vector2((projectedPoints[idx2].x - minX) / nMaxX, (projectedPoints[idx2].y - minZ) / nMaxZ);

            Vector3 tangent = Vector3.zero;

            triangleBucketList.Add(new Triangle3D(vertexBucketList, new Triangle3D.Vertex[] { vertexBucketList[idx0], vertexBucketList[idx1], vertexBucketList[idx2] }, new Vector3[] { planeUp, planeUp, planeUp }, new Vector2[] { UV1, UV2, UV3 }, new Vector4[] { new Vector4(tangent.x, tangent.y, tangent.z, 1), new Vector4(tangent.x, tangent.y, tangent.z, 1), new Vector4(tangent.x, tangent.y, tangent.z, 1) }, new int[] { idx0 + triOffset, idx1 + triOffset, idx2 + triOffset }, 0));
        }
    }

    public static Triangle3D.Vertex[] GetPolyLoop(ref List<Vector3[]> filteredList)
    {
        List<Triangle3D.Vertex> sortedVerts = new List<Triangle3D.Vertex>(filteredList.Count * 2);
        Vector3 start = filteredList[0][0];
        sortedVerts.Add(new Triangle3D.Vertex(start));
        Vector3 end = filteredList[0][1];
        filteredList.Remove(filteredList[0]);
        bool foundNext = true;
        while (filteredList.Count > 0 && foundNext)
        {
            foundNext = false;
            for (int i = 0; i < filteredList.Count; i++)
            {
                if (end == filteredList[i][0])
                {
                    if (!ArePointsCoincident(end, sortedVerts))
                    {
                        sortedVerts.Add(new Triangle3D.Vertex(end));
                    }
                    foundNext = true;
                    end = filteredList[i][1];
                    filteredList.Remove(filteredList[i]);
                    break;
                }

                else if (end == filteredList[i][1])
                {
                    if (!ArePointsCoincident(end, sortedVerts))
                    {
                        sortedVerts.Add(new Triangle3D.Vertex(end));
                    }
                    foundNext = true;
                    end = filteredList[i][0];
                    filteredList.Remove(filteredList[i]);
                    break;
                }
            }
        }

        if (end == start)
        {
            //Debug.Log("Found loop " + filteredList.Count);
            return sortedVerts.ToArray();
        }
        //Debug.Log("Found open poly " + filteredList.Count);
        return sortedVerts.ToArray();
    }

    public static bool ArePointsCoincident(Vector3 point, IEnumerable<Triangle3D.Vertex> allVerts)
    {
        foreach (Triangle3D.Vertex vert in allVerts)
        {
            if (Vector3.Distance(vert.pos, point) < 0.0001f)
            {
                return true;
            }
        }
        return false;
    }

    public static SplitMesh GrabMeshOutline(Mesh mesh, Plane cuttingPlane, Transform transform, Transform rotation)
    {
        debugPolyLoop = new List<Vector3>();
        debugEdgePoints = new List<Vector3>();
        debugEdges = new List<Vector3[]>();
        debugLoopEdgePoints = new List<Vector3[]>();

        int vertCount = mesh.vertexCount;
        Vector3[] verts = mesh.vertices;
        Triangle3D.Vertex[] allVerts = new Triangle3D.Vertex[vertCount];
        for (int i = 0; i < vertCount; i++)
        {
            allVerts[i] = new Triangle3D.Vertex(transform.TransformPoint(verts[i]));
        }

        List<Triangle3D.Vertex> allVertList = new List<Triangle3D.Vertex>();
        Vector2[] originalUVs = mesh.uv;
        Vector3[] originalNormals = mesh.normals;
        Vector4[] originalTangents = mesh.tangents;

        int triCount = mesh.triangles.Length / 3;
        Triangle3D[] originalTriangles = new Triangle3D[triCount];

        int offset = 0;
        for (int j = 0; j < mesh.subMeshCount; j++)
        {
            uint triOffset = 0;
            int[] subMeshIndices = mesh.GetTriangles(j);
            int subMeshTriCount = subMeshIndices.Length / 3;

            for (int i = 0; i < subMeshTriCount; i++)
            {
                int idx0 = subMeshIndices[triOffset + 0];
                int idx1 = subMeshIndices[triOffset + 1];
                int idx2 = subMeshIndices[triOffset + 2];

                if (originalTriangles.Length <= offset)
                {
                    Debug.Log("Error");
                }


                originalTriangles[offset++] = new Triangle3D(allVertList, new Triangle3D.Vertex[] { allVerts[idx0], allVerts[idx1], allVerts[idx2] }, new Vector3[] { originalNormals[idx0], originalNormals[idx1], originalNormals[idx2] }, new Vector2[] { originalUVs[idx0], originalUVs[idx1], originalUVs[idx2] }, new Vector4[] { originalTangents[idx0], originalTangents[idx1], originalTangents[idx2] }, new int[] { subMeshIndices[triOffset + 0], subMeshIndices[triOffset + 1], subMeshIndices[triOffset + 2] }, j);

                triOffset += 3;
            }
        }

        if (originalTriangles.Length > 0)
        {
            int processedTriCount = 0;
            Triangle3D[] processedTris = new Triangle3D[originalTriangles.Length * 3];

            ClassificationUtil.Classification prevSide = ClassificationUtil.Classification.UNDEFINED;
            foreach (Triangle3D originalTriangle in originalTriangles)
            {
                ClassificationUtil.Classification side;
                Triangle3D[] splitTriangles = TriangleUtil.SplitTriangleWithPlane(originalTriangle, cuttingPlane, float.Epsilon, out side);
                if (prevSide != ClassificationUtil.Classification.UNDEFINED && prevSide != side)
                {
                }
                prevSide = side;
                if (splitTriangles != null)
                {
                    // Triangle was cut
                    foreach (Triangle3D splitTriangle in splitTriangles)
                    {
                        processedTris[processedTriCount] = splitTriangle;
                        processedTriCount++;

                    }
                }

                else
                {
                    // Triangle was not cut
                    processedTris[processedTriCount] = originalTriangle;
                    processedTriCount++;

                }
            }

            int triangleBucketACount = 0;
            int triangleBucketBCount = 0;
            Triangle3D[] triangleBucketA = new Triangle3D[processedTriCount];
            Triangle3D[] triangleBucketB = new Triangle3D[processedTriCount];
            for (int i = 0; i < processedTriCount; i++)
            {
                ClassificationUtil.Classification[] classes;
                ClassificationUtil.Classification triClass = ClassificationUtil.ClassifyPoints(processedTris[i].pos, cuttingPlane, out classes, float.Epsilon);

                if (triClass == ClassificationUtil.Classification.FRONT)
                {
                    triangleBucketA[triangleBucketACount++] = processedTris[i];
                }

                else if (triClass == ClassificationUtil.Classification.BACK)
                {
                    triangleBucketB[triangleBucketBCount++] = processedTris[i];
                }
            }

            if (triangleBucketACount == 0 || triangleBucketBCount == 0)
            {
                return null;
            }

            List<Triangle3D> totalCapTriBucket = new List<Triangle3D>();
            List<Triangle3D.Vertex> totalCapVertBucket = new List<Triangle3D.Vertex>();
            List<List<Triangle3D.Vertex>> capVertGroups = new List<List<Triangle3D.Vertex>>();
            while (debugEdges.Count > 2)
            {
                List<Triangle3D> capTriBucket = new List<Triangle3D>();
                List<Triangle3D.Vertex> capVertBucket = new List<Triangle3D.Vertex>();
                Triangle3D.Vertex[] sortedVerts = GetPolyLoop(ref debugEdges);
                if (sortedVerts != null)
                {
                    CapMesh(out capTriBucket, out capVertBucket, sortedVerts, transform, rotation, totalCapTriBucket.Count);
                }
#if false
				if(capVertBucket.Count > 2)
				{
					for(int i = 0; i < capVertBucket.Count - 1; i++)
					{
						Debug.DrawLine(transform.TransformPoint(capVertBucket[i].pos), transform.TransformPoint(capVertBucket[i + 1].pos));
					}
					Debug.DrawLine(transform.TransformPoint(capVertBucket[capVertBucket.Count - 1].pos), transform.TransformPoint(capVertBucket[0].pos));
				}
#endif
                totalCapTriBucket.AddRange(capTriBucket);
                totalCapVertBucket.AddRange(capVertBucket);
                capVertGroups.Add(capVertBucket);
            }

            Vector3[] vertexBucket = new Vector3[totalCapVertBucket.Count];
            for (int i = 0; i < totalCapVertBucket.Count; i++)
            {
                vertexBucket[i] = transform.TransformPoint(totalCapVertBucket[i].pos);
            }

            return new SplitMesh(new List<Vector3>(vertexBucket), totalCapTriBucket, capVertGroups);
        }

        else
        {
            Debug.LogError("Source geometry empty");
            return null;
        }
    }

    public static bool CutTriangleMeshOneSide(Mesh outputMeshes, Mesh sourceMesh, Plane cuttingPlane, Transform transform, Transform rotation, bool frontSide)
    {
        return CutTriangleMeshOneSide(outputMeshes, sourceMesh, cuttingPlane, transform, rotation, frontSide, true);
    }

    public static bool CutTriangleMeshOneSide(Mesh outputMeshes, Mesh sourceMesh, Plane cuttingPlane, Transform transform, Transform rotation, bool frontSide, bool cap)
    {
        float epsilon = 0.00001f;

        hashCheck = new Dictionary<Vector3, int>();
        debugPolyLoop = new List<Vector3>();
        debugEdgePoints = new List<Vector3>();
        debugEdges = new List<Vector3[]>();
        debugLoopEdgePoints = new List<Vector3[]>();

        int vertCount = sourceMesh.vertexCount;
        Vector3[] verts = sourceMesh.vertices;
        Triangle3D.Vertex[] allVerts = new Triangle3D.Vertex[vertCount];
        for (int i = 0; i < vertCount; i++)
        {
            allVerts[i] = new Triangle3D.Vertex(transform.TransformPoint(verts[i]));
        }

        List<Triangle3D.Vertex> allVertList = new List<Triangle3D.Vertex>();
        //int[] originalIndices = sourceMesh.triangles;
        Vector2[] originalUVs = sourceMesh.uv;
        Vector3[] originalNormals = sourceMesh.normals;
        Vector4[] originalTangents = sourceMesh.tangents;

        int triCount = sourceMesh.triangles.Length / 3;
        Triangle3D[] originalTriangles = new Triangle3D[triCount];

        int offset = 0;
        for (int j = 0; j < sourceMesh.subMeshCount; j++)
        {
            uint triOffset = 0;
            int[] subMeshIndices = sourceMesh.GetTriangles(j);
            int subMeshTriCount = subMeshIndices.Length / 3;

            for (int i = 0; i < subMeshTriCount; i++)
            {
                int idx0 = subMeshIndices[triOffset + 0];
                int idx1 = subMeshIndices[triOffset + 1];
                int idx2 = subMeshIndices[triOffset + 2];
                originalTriangles[offset++] = new Triangle3D(allVertList, new Triangle3D.Vertex[] { allVerts[idx0], allVerts[idx1], allVerts[idx2] }, new Vector3[] { originalNormals[idx0], originalNormals[idx1], originalNormals[idx2] }, new Vector2[] { originalUVs[idx0], originalUVs[idx1], originalUVs[idx2] }, new Vector4[] { originalTangents[idx0], originalTangents[idx1], originalTangents[idx2] }, new int[] { subMeshIndices[triOffset + 0], subMeshIndices[triOffset + 1], subMeshIndices[triOffset + 2] }, j);
                triOffset += 3;
            }
        }

        if (originalTriangles.Length > 0)
        {
            int processedTriCount = 0;
            Triangle3D[] processedTris = new Triangle3D[originalTriangles.Length * 3];

            ClassificationUtil.Classification prevSide = ClassificationUtil.Classification.UNDEFINED;
            foreach (Triangle3D originalTriangle in originalTriangles)
            {
                ClassificationUtil.Classification side;
                Triangle3D[] splitTriangles = TriangleUtil.SplitTriangleWithPlane(originalTriangle, cuttingPlane, epsilon, out side, cap);
                if (prevSide != ClassificationUtil.Classification.UNDEFINED && prevSide != side)
                {
                }
                prevSide = side;
                if (splitTriangles != null)
                {
                    foreach (Triangle3D splitTriangle in splitTriangles)
                    {
                        processedTris[processedTriCount] = splitTriangle;
                        processedTriCount++;
                    }
                }

                else
                {
                    processedTris[processedTriCount] = originalTriangle;
                    processedTriCount++;
                }
            }

            //if (!cut)
            //{
            //    CloneMesh(sourceMesh, outputMeshes);
            //    return false;
            //}

            int triangleBucketCount = 0;
            Triangle3D[] triangleBucket = new Triangle3D[processedTriCount];
            for (int i = 0; i < processedTriCount; i++)
            {
                ClassificationUtil.Classification[] classes;
                ClassificationUtil.Classification triClass = ClassificationUtil.ClassifyPoints(processedTris[i].pos, cuttingPlane, out classes, epsilon);

                if (triClass == ClassificationUtil.Classification.FRONT && frontSide)
                {
                    triangleBucket[triangleBucketCount++] = processedTris[i];
                }

                else if (triClass == ClassificationUtil.Classification.BACK && !frontSide)
                {
                    triangleBucket[triangleBucketCount++] = processedTris[i];
                }
            }

            if (triangleBucketCount == 0)
            {
                outputMeshes.Clear();
                return false;
            }

            List<Triangle3D> totalCapTriBucket = new List<Triangle3D>();
            List<Triangle3D.Vertex> totalCapVertBucket = new List<Triangle3D.Vertex>();

            while (cap && debugEdges.Count > 2)
            {
                List<Triangle3D> capTriBucket = new List<Triangle3D>();
                List<Triangle3D.Vertex> capVertBucket = new List<Triangle3D.Vertex>();
                Triangle3D.Vertex[] sortedVerts = GetPolyLoop(ref debugEdges);

                if (sortedVerts != null)
                {
                    CapMesh(out capTriBucket, out capVertBucket, sortedVerts, transform, rotation, totalCapTriBucket.Count);
                }

                totalCapTriBucket.AddRange(capTriBucket);
                totalCapVertBucket.AddRange(capVertBucket);
            }

            if (triangleBucketCount > 0)
            {
                SortMesh(outputMeshes, triangleBucket, triangleBucketCount, transform, totalCapTriBucket, totalCapVertBucket, !frontSide);
            }
            return true;
        }

        else
        {
            return false;
        }
    }

    public static bool CutTriangleMesh(Mesh[] outputMeshes, Mesh sourceMesh, Plane cuttingPlane, Transform transform, Transform rotation)
    {
        return CutTriangleMesh(outputMeshes, sourceMesh, cuttingPlane, transform, rotation, true);
    }

    public static bool CutTriangleMesh(Mesh[] outputMeshes, Mesh sourceMesh, Plane cuttingPlane, Transform transform, Transform rotation, bool cap)
    {
        float epsilon = 0.00001f;

        debugPolyLoop = new List<Vector3>();
        debugEdgePoints = new List<Vector3>();
        debugEdges = new List<Vector3[]>();
        debugLoopEdgePoints = new List<Vector3[]>();

        int vertCount = sourceMesh.vertexCount;
        Vector3[] verts = sourceMesh.vertices;
        Triangle3D.Vertex[] allVerts = new Triangle3D.Vertex[vertCount];
        for (int i = 0; i < vertCount; i++)
        {
            allVerts[i] = new Triangle3D.Vertex(transform.TransformPoint(verts[i]));
        }

        List<Triangle3D.Vertex> allVertList = new List<Triangle3D.Vertex>();
        Vector2[] originalUVs = sourceMesh.uv;
        Vector3[] originalNormals = sourceMesh.normals;
        Vector4[] originalTangents = sourceMesh.tangents;

        int triCount = sourceMesh.triangles.Length / 3;
        Triangle3D[] originalTriangles = new Triangle3D[triCount];

        int offset = 0;
        for (int j = 0; j < sourceMesh.subMeshCount; j++)
        {
            uint triOffset = 0;
            int[] subMeshIndices = sourceMesh.GetTriangles(j);
            int subMeshTriCount = subMeshIndices.Length / 3;

            for (int i = 0; i < subMeshTriCount; i++)
            {
                int idx0 = subMeshIndices[triOffset + 0];
                int idx1 = subMeshIndices[triOffset + 1];
                int idx2 = subMeshIndices[triOffset + 2];


                originalTriangles[offset++] = new Triangle3D(allVertList, new Triangle3D.Vertex[] { allVerts[idx0], allVerts[idx1], allVerts[idx2] }, new Vector3[] { originalNormals[idx0], originalNormals[idx1], originalNormals[idx2] }, new Vector2[] { originalUVs[idx0], originalUVs[idx1], originalUVs[idx2] }, new Vector4[] { originalTangents[idx0], originalTangents[idx1], originalTangents[idx2] }, new int[] { subMeshIndices[triOffset + 0], subMeshIndices[triOffset + 1], subMeshIndices[triOffset + 2] }, j);

                triOffset += 3;
            }
        }

        if (originalTriangles.Length > 0)
        {
            int processedTriCount = 0;
            Triangle3D[] processedTris = new Triangle3D[originalTriangles.Length * 3];

            ClassificationUtil.Classification prevSide = ClassificationUtil.Classification.UNDEFINED;
            foreach (Triangle3D originalTriangle in originalTriangles)
            {
                ClassificationUtil.Classification side;
                Triangle3D[] splitTriangles = TriangleUtil.SplitTriangleWithPlane(originalTriangle, cuttingPlane, epsilon, out side, cap);
                if (prevSide != ClassificationUtil.Classification.UNDEFINED && prevSide != side)
                {
                }
                prevSide = side;
                if (splitTriangles != null)
                {
                    // Triangle was cut
                    foreach (Triangle3D splitTriangle in splitTriangles)
                    {
                        processedTris[processedTriCount] = splitTriangle;
                        processedTriCount++;

                    }
                }

                else
                {
                    // Triangle was not cut
                    processedTris[processedTriCount] = originalTriangle;
                    processedTriCount++;

                }
            }

            int triangleBucketACount = 0;
            int triangleBucketBCount = 0;
            Triangle3D[] triangleBucketA = new Triangle3D[processedTriCount];
            Triangle3D[] triangleBucketB = new Triangle3D[processedTriCount];
            for (int i = 0; i < processedTriCount; i++)
            {
                ClassificationUtil.Classification[] classes;
                ClassificationUtil.Classification triClass = ClassificationUtil.ClassifyPoints(processedTris[i].pos, cuttingPlane, out classes, epsilon);

                if (triClass == ClassificationUtil.Classification.FRONT)
                {
                    triangleBucketA[triangleBucketACount++] = processedTris[i];
                }

                else if (triClass == ClassificationUtil.Classification.BACK)
                {
                    triangleBucketB[triangleBucketBCount++] = processedTris[i];
                }
            }

            if (triangleBucketACount == 0 || triangleBucketBCount == 0)
            {
                return false;
            }

            List<Triangle3D> totalCapTriBucket = new List<Triangle3D>();
            List<Triangle3D.Vertex> totalCapVertBucket = new List<Triangle3D.Vertex>();

            while (cap && debugEdges.Count > 2)
            {
                List<Triangle3D> capTriBucket = new List<Triangle3D>();
                List<Triangle3D.Vertex> capVertBucket = new List<Triangle3D.Vertex>();

                Triangle3D.Vertex[] sortedVerts = GetPolyLoop(ref debugEdges);

                if (sortedVerts != null)
                {
                    CapMesh(out capTriBucket, out capVertBucket, sortedVerts, transform, rotation, totalCapTriBucket.Count);
                }

				if(capVertBucket.Count > 2)
				{
					for(int i = 0; i < capVertBucket.Count - 1; i++)
					{
						Debug.DrawLine(transform.TransformPoint(capVertBucket[i].pos), transform.TransformPoint(capVertBucket[i + 1].pos));
					}
					Debug.DrawLine(transform.TransformPoint(capVertBucket[capVertBucket.Count - 1].pos), transform.TransformPoint(capVertBucket[0].pos));
				}

                totalCapTriBucket.AddRange(capTriBucket);
                totalCapVertBucket.AddRange(capVertBucket);
            }

            if (triangleBucketACount > 0)
            {
                SortMesh(outputMeshes[0], triangleBucketA, triangleBucketACount, transform, totalCapTriBucket, totalCapVertBucket, false);
            }
            if (triangleBucketBCount > 0)
            {
                SortMesh(outputMeshes[1], triangleBucketB, triangleBucketBCount, transform, totalCapTriBucket, totalCapVertBucket, true);
            }
            return true;
        }

        else
        {
            Debug.Log("source geometry empty");
            return false;
        }
    }

    public static void SortMesh(Mesh targetMesh, int[] indices, Vector3[] vertices, int triangleBucketCount, bool flip)
    {
        Dictionary<Vector3, int> vertCache = new Dictionary<Vector3, int>(triangleBucketCount);
        int vertexBucketCount = 0;

        Vector3[] vertexBucket = new Vector3[triangleBucketCount];
        for (int i = 0; i < triangleBucketCount; i += 3)
        {
            if (!vertCache.ContainsKey(vertices[i + 0]))
            {
                vertCache[vertices[i + 0]] = vertexBucketCount;
                vertices[i + 0] = vertices[i + 0];
                indices[i + 0] = vertexBucketCount;
                vertexBucket[vertexBucketCount] = vertices[i + 0];
                vertexBucketCount++;
            }
            else
            {
                indices[i + 0] = vertCache[vertices[i + 0]];
            }

            if (!vertCache.ContainsKey(vertices[i + 1]))
            {
                vertCache[vertices[i + 1]] = vertexBucketCount;
                vertices[i + 1] = vertices[i + 1];
                indices[i + 1] = vertexBucketCount;
                vertexBucket[vertexBucketCount] = vertices[i + 1];
                vertexBucketCount++;
            }
            else
            {
                indices[i + 1] = vertCache[vertices[i + 1]];
            }

            if (!vertCache.ContainsKey(vertices[i + 2]))
            {
                vertCache[vertices[i + 2]] = vertexBucketCount;
                vertices[i + 2] = vertices[i + 2];
                indices[i + 2] = vertexBucketCount;
                vertexBucket[vertexBucketCount] = vertices[i + 2];
                vertexBucketCount++;
            }
            else
            {
                indices[i + 2] = vertCache[vertices[i + 2]];
            }
        }

        CreateNewMesh(targetMesh, indices, vertexBucket);
    }

    public static void SortMesh(Mesh targetMesh, Triangle3D[] triangleBucket, int triangleBucketCount, Transform objectTransform, List<Triangle3D> capTriBucket, List<Triangle3D.Vertex> capVertBucket, bool flip)
    {
        Dictionary<int, int> vertCache = new Dictionary<int, int>(triangleBucketCount * 3);
        Triangle3D.Vertex vert = null;

        int vertexBucketCount = 0;
        Vector3[] vertexBucket = new Vector3[triangleBucketCount * 3];
        for (int i = 0; i < triangleBucketCount; i++)
        {
            Triangle3D triangle = triangleBucket[i];
            for (int j = 0; j < triangle.vertices.Length; j++)
            {
                vert = triangle.vertices[j];
                int code = vert.GetHashCode();
                if (!vertCache.ContainsKey(code))
                {
                    vertCache[code] = vertexBucketCount;
                    triangle.vertices[j] = vert;
                    triangle.indices[j] = vertexBucketCount;
                    vertexBucket[vertexBucketCount] = objectTransform.InverseTransformPoint(vert.pos);
                    vertexBucketCount++;
                }
                else
                {
                    triangle.indices[j] = vertCache[code];
                }
            }
        }

        int triWithCapCount = triangleBucketCount + capTriBucket.Count;
        int vertWithCapCount = vertexBucketCount + capVertBucket.Count;
        Array.Resize<Triangle3D>(ref triangleBucket, triangleBucketCount + capTriBucket.Count);
        Array.Resize<Vector3>(ref vertexBucket, vertexBucketCount + capVertBucket.Count);

        int triOff = 0;
        for (int i = triangleBucketCount; i < triWithCapCount; i++)
        {
            triangleBucket[i] = new Triangle3D(capTriBucket[triOff++], vertexBucketCount, flip);
        }
        int vertOff = 0;
        for (int i = vertexBucketCount; i < vertWithCapCount; i++)
        {
            vertexBucket[i] = capVertBucket[vertOff++].pos;
        }
        CreateNewMesh(targetMesh, triangleBucket, vertexBucket);
    }

    public static void CreateNewMeshSimple(Mesh targetMesh, Triangle3D[] triangle3D, Vector3[] vertices)
    {
        int triLength = 0;
        int[] tris = new int[triangle3D.Length * 3];
        Vector2[] uvs = new Vector2[vertices.Length];
        Vector3[] normals = new Vector3[vertices.Length];
        Vector4[] tangents = new Vector4[vertices.Length];

        Triangle3D triangle;
        int idxV0;
        int idxV1;
        int idxV2;
        Vector2[] triUvs;
        Vector3[] triNormals;
        Vector4[] triTangents;

        for (int i = 0; i < triangle3D.Length; i++)
        {
            triangle = triangle3D[i];
            idxV0 = triangle.indices[0];
            idxV1 = triangle.indices[1];
            idxV2 = triangle.indices[2];

            triUvs = triangle.uvs;
            triNormals = triangle.normals;
            triTangents = triangle.tangents;

            tris[triLength++] = idxV0;
            tris[triLength++] = idxV1;
            tris[triLength++] = idxV2;

            uvs[idxV0] = triUvs[0];
            uvs[idxV1] = triUvs[1];
            uvs[idxV2] = triUvs[2];

            normals[idxV0] = triNormals[0];
            normals[idxV1] = triNormals[1];
            normals[idxV2] = triNormals[2];

            tangents[idxV0] = triTangents[0];
            tangents[idxV1] = triTangents[1];
            tangents[idxV2] = triTangents[2];
        }


        targetMesh.Clear();
        targetMesh.vertices = vertices;
        targetMesh.triangles = tris;
        targetMesh.normals = normals;
        targetMesh.uv = uvs;
        targetMesh.tangents = tangents;
    }


    public static void CreateNewMesh(Mesh targetMesh, int[] indices, Vector3[] vertices)
    {
        int meshLength = 0;
        int[] meshIdx = new int[indices.Length];

        //int[] tris = new int[indices.Length];
        //Vector2[] uvs = new Vector2[vertices.Length];
        //Vector3[] normals = new Vector3[vertices.Length];
        //Vector4[] tangents = new Vector4[vertices.Length];

        int idxV0;
        int idxV1;
        int idxV2;
        //Vector2[] triUvs;
        //Vector3[] triNormals;
        //Vector4[] triTangents;
        for (int i = 0; i < indices.Length; i += 3)
        {
            idxV0 = indices[i + 0];
            idxV1 = indices[i + 1];
            idxV2 = indices[i + 2];

            //triUvs = triangle.uvs;
            //triNormals = triangle.normals;
            //triTangents = triangle.tangents;

            meshIdx[meshLength++] = idxV0;
            meshIdx[meshLength++] = idxV1;
            meshIdx[meshLength++] = idxV2;

            //uvs[idxV0] = triUvs[0];
            //uvs[idxV1] = triUvs[1];
            //uvs[idxV2] = triUvs[2];
            //normals[idxV0] = triNormals[0];
            //normals[idxV1] = triNormals[1];
            //normals[idxV2] = triNormals[2];
            //tangents[idxV0] = triTangents[0];
            //tangents[idxV1] = triTangents[1];
            //tangents[idxV2] = triTangents[2];
        }

        targetMesh.Clear();
        //targetMesh.subMeshCount = 2;
        targetMesh.vertices = vertices;
        targetMesh.triangles = meshIdx;
        //targetMesh.normals = normals;
        //targetMesh.uv = uvs;
        //targetMesh.tangents = tangents;
    }

    public static void CreateNewMesh(Mesh targetMesh, Triangle3D[] triangle3D, Vector3[] vertices)
    {
        int subMeshALength = 0;
        int subMeshBLength = 0;
        int[] subMeshA = new int[triangle3D.Length * 3];
        int[] subMeshB = new int[triangle3D.Length * 3];

        int offset = 0;
        //int[] tris = new int[triangle3D.Length * 3];
        Vector2[] uvs = new Vector2[vertices.Length];
        Vector3[] normals = new Vector3[vertices.Length];
        Vector4[] tangents = new Vector4[vertices.Length];

        Triangle3D triangle;
        int idxV0;
        int idxV1;
        int idxV2;
        Vector2[] triUvs;
        Vector3[] triNormals;
        Vector4[] triTangents;
        for (int i = 0; i < triangle3D.Length; i++)
        {
            triangle = triangle3D[i];
            idxV0 = triangle.indices[0];
            idxV1 = triangle.indices[1];
            idxV2 = triangle.indices[2];

            triUvs = triangle.uvs;
            triNormals = triangle.normals;
            triTangents = triangle.tangents;

            if (triangle3D[i].subMeshGroup == 0)
            {
                subMeshA[subMeshALength++] = idxV0;
                subMeshA[subMeshALength++] = idxV1;
                subMeshA[subMeshALength++] = idxV2;
            }
            if (triangle3D[i].subMeshGroup == 1)
            {
                subMeshB[subMeshBLength++] = idxV0;
                subMeshB[subMeshBLength++] = idxV1;
                subMeshB[subMeshBLength++] = idxV2;
            }
            uvs[idxV0] = triUvs[0];
            uvs[idxV1] = triUvs[1];
            uvs[idxV2] = triUvs[2];
            normals[idxV0] = triNormals[0];
            normals[idxV1] = triNormals[1];
            normals[idxV2] = triNormals[2];
            tangents[idxV0] = triTangents[0];
            tangents[idxV1] = triTangents[1];
            tangents[idxV2] = triTangents[2];
            offset += 3;
        }

        targetMesh.Clear();
        targetMesh.subMeshCount = 2;
        targetMesh.vertices = vertices;
        targetMesh.SetTriangles(subMeshA, 0);
        targetMesh.SetTriangles(subMeshB, 1);
        targetMesh.normals = normals;
        targetMesh.uv = uvs;
        targetMesh.tangents = tangents;
    }

    public static Dictionary<Vector3, int> hashCheck;

    public static bool CutTriangleMeshFast(Mesh[] outputMeshes, Mesh sourceMesh, Transform objectTransform, Transform planeTransform, bool cap)
    {
        float epsilon = 0.00001f;

        hashCheck = new Dictionary<Vector3, int>();
        debugPolyLoop = new List<Vector3>();
        debugEdgePoints = new List<Vector3>();
        debugEdges = new List<Vector3[]>();
        debugLoopEdgePoints = new List<Vector3[]>();

        int originalVertexCount = sourceMesh.vertexCount;
        Vector3[] originalVertices = sourceMesh.vertices;

        int originalIndexCount = sourceMesh.triangles.Length;
        int[] originalIndices = sourceMesh.triangles;

        Vector3 centre = objectTransform.transform.InverseTransformPoint(planeTransform.position);
        Vector3 up = objectTransform.transform.InverseTransformDirection(planeTransform.up);
        Plane cuttingPlane = new Plane(up, centre);

        int totalIndexCount = originalIndexCount;
        int totalVertexCount = originalVertexCount;

        int indexBufferCountA = 0;
        int vertexBufferCountA = 0;
        Vector3[] vertexBufferA = new Vector3[totalVertexCount * 10];
        int[] indexBufferA = new int[originalIndexCount * 10];

        int indexBufferCountB = 0;
        int vertexBufferCountB = 0;
        Vector3[] vertexBufferB = new Vector3[totalVertexCount * 10];
        int[] indexBufferB = new int[originalIndexCount * 10];

        Vector3[] points = new Vector3[3];
        int[] triIndices = new int[3];

        for (int i = 0; i < totalIndexCount; i += 3)
        {
            triIndices[0] = originalIndices[i];
            triIndices[1] = originalIndices[i + 1];
            triIndices[2] = originalIndices[i + 2];

            points[0] = originalVertices[triIndices[0]];
            points[1] = originalVertices[triIndices[1]];
            points[2] = originalVertices[triIndices[2]];

            int[] newTris;
            Vector3[] newPoints;

            ClassificationUtil.Classification side;
            TriangleUtil.SplitTriangleWithPlane(
                ref totalVertexCount,
                points,
                triIndices,
                cuttingPlane,
                epsilon,
                out side,
                cap,
                out newTris,
                out newPoints);

            // Slice the triangle then classify which side it's on
            if (newTris != null)
            {
                for (int j = 0; j < newTris.Length; j += 3)
                {
                    ClassificationUtil.Classification[] classes;
                    ClassificationUtil.Classification triClass = ClassificationUtil.ClassifyPoints(
                        new Vector3[] { newPoints[j + 0], newPoints[j + 1], newPoints[j + 2] },
                        cuttingPlane,
                        out classes,
                        epsilon);

                    //Debug.DrawLine(newPoints[j + 0], newPoints[j + 1]);
                    //Debug.DrawLine(newPoints[j + 1], newPoints[j + 2]);
                    //Debug.DrawLine(newPoints[j + 2], newPoints[j + 0]);

                    if (triClass == ClassificationUtil.Classification.FRONT)
                    {
                        indexBufferA[indexBufferCountA++] = newTris[j + 0];
                        indexBufferA[indexBufferCountA++] = newTris[j + 1];
                        indexBufferA[indexBufferCountA++] = newTris[j + 2];
                        vertexBufferA[vertexBufferCountA++] = newPoints[j + 0];
                        vertexBufferA[vertexBufferCountA++] = newPoints[j + 1];
                        vertexBufferA[vertexBufferCountA++] = newPoints[j + 2];
                    }
                    else if (triClass == ClassificationUtil.Classification.BACK)
                    {
                        indexBufferB[indexBufferCountB++] = newTris[j + 0];
                        indexBufferB[indexBufferCountB++] = newTris[j + 1];
                        indexBufferB[indexBufferCountB++] = newTris[j + 2];
                        vertexBufferB[vertexBufferCountB++] = newPoints[j + 0];
                        vertexBufferB[vertexBufferCountB++] = newPoints[j + 1];
                        vertexBufferB[vertexBufferCountB++] = newPoints[j + 2];
                    }
                }
            }
            else
            {
                ClassificationUtil.Classification[] classes;
                ClassificationUtil.Classification triClass = ClassificationUtil.ClassifyPoints(
                    new Vector3[] { points[0], points[1], points[2] },
                    cuttingPlane,
                    out classes,
                    epsilon);

                //Debug.DrawLine(points[0], points[1], Color.red);
                //Debug.DrawLine(points[1], points[2], Color.red);
                //Debug.DrawLine(points[2], points[0], Color.red);

                if (triClass == ClassificationUtil.Classification.FRONT)
                {
                    indexBufferA[indexBufferCountA++] = triIndices[0];
                    indexBufferA[indexBufferCountA++] = triIndices[1];
                    indexBufferA[indexBufferCountA++] = triIndices[2];
                    vertexBufferA[vertexBufferCountA++] = points[0];
                    vertexBufferA[vertexBufferCountA++] = points[1];
                    vertexBufferA[vertexBufferCountA++] = points[2];
                }
                else if (triClass == ClassificationUtil.Classification.BACK)
                {
                    indexBufferB[indexBufferCountB++] = triIndices[0];
                    indexBufferB[indexBufferCountB++] = triIndices[1];
                    indexBufferB[indexBufferCountB++] = triIndices[2];
                    vertexBufferB[vertexBufferCountB++] = points[0];
                    vertexBufferB[vertexBufferCountB++] = points[1];
                    vertexBufferB[vertexBufferCountB++] = points[2];
                }
            }
        }

        if (indexBufferCountA > 0)
        {
            SortMesh(outputMeshes[0], indexBufferA, vertexBufferA, indexBufferCountA, false);
        }
        if (indexBufferCountB > 0)
        {
            SortMesh(outputMeshes[1], indexBufferB, vertexBufferB, indexBufferCountB, true);
        }
        return true;
    }
}

