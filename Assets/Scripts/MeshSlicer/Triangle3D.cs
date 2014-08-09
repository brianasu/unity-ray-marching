using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

public class Triangle3D
{
    public class Edge
    {
        public int startIdx;
        public int endIdx;
        public int faceCount = 0;
        public Edge(int startIdx, int endIdx)
        {
            this.startIdx = startIdx;
            this.endIdx = endIdx;
        }
    }

    public class Vertex
    {
        public Vector3 pos;
        public Vertex(Vector3 pos)
        {
            this.pos = pos;
        }
    }

    //Mesh vertices
    private List<Vertex> _meshVertices;
    private Vertex[] _vertices;
    private Vector4[] _tangents;
    private Vector2[] _uvs;
    private Vector3[] _normals;
    private int[] _indices;
    private int _subMeshGroup;

    public Triangle3D(List<Vertex> meshVertices, Vertex[] verts, Vector3[] normals, Vector2[] uvs, Vector4[] tangents, int[] indices, int subMeshGroup)
    {
        _vertices = verts;
        _uvs = uvs;
        _indices = indices;
        _normals = normals;
        _meshVertices = meshVertices;
        _tangents = tangents;
        _subMeshGroup = subMeshGroup;
    }

    public Triangle3D(Triangle3D triangle3D, int offset, bool flip)
    {
        _meshVertices = triangle3D.meshVertices;
        _subMeshGroup = triangle3D.subMeshGroup;

        if (flip)
        {
            _vertices = new Vertex[] { triangle3D.vertices[2], triangle3D.vertices[1], triangle3D.vertices[0] };
            _uvs = new Vector2[] { triangle3D.uvs[2], triangle3D.uvs[1], triangle3D.uvs[0] };
            _indices = new int[] { triangle3D.indices[2] + offset, triangle3D.indices[1] + offset, triangle3D.indices[0] + offset };
            _normals = new Vector3[] { -triangle3D.normals[2], -triangle3D.normals[1], -triangle3D.normals[0] };
            _tangents = new Vector4[] { triangle3D.tangents[2], triangle3D.tangents[1], triangle3D.tangents[0] };
        }
        else
        {
            _vertices = new Vertex[] { triangle3D.vertices[0], triangle3D.vertices[1], triangle3D.vertices[2] };
            _uvs = new Vector2[] { triangle3D.uvs[0], triangle3D.uvs[1], triangle3D.uvs[2] };
            _indices = new int[] { triangle3D.indices[0] + offset, triangle3D.indices[1] + offset, triangle3D.indices[2] + offset };
            _normals = new Vector3[] { triangle3D.normals[0], triangle3D.normals[1], triangle3D.normals[2] };
            _tangents = new Vector4[] { triangle3D.tangents[0], triangle3D.tangents[1], triangle3D.tangents[2] };
        }
    }

    public int subMeshGroup
    {
        get { return _subMeshGroup; }
    }

    public List<Vertex> meshVertices
    {
        get { return _meshVertices; }
    }


    public Vector3[] pos
    {
        get { return new Vector3[] { v0.pos, v1.pos, v2.pos }; }
    }

    public Vector3 pos0
    {
        get { return v0.pos; }
    }
    public Vector3 pos1
    {
        get { return v1.pos; }
    }
    public Vector3 pos2
    {
        get { return v2.pos; }
    }

    public Vertex[] vertices
    {
        get { return _vertices; }
    }
    public Vertex v0
    {
        get { return _vertices[0]; }
    }
    public Vertex v1
    {
        get { return _vertices[1]; }
    }
    public Vertex v2
    {
        get { return _vertices[2]; }
    }


    public int[] indices
    {
        get { return _indices; }
    }
    public int idxV0
    {
        get { return _indices[0]; }
		set { _indices[0] = value; }
    }
    public int idxV1
    {
        get { return _indices[1]; }
		set { _indices[1] = value; }
    }
    public int idxV2
    {
        get { return _indices[2]; }
		set { _indices[2] = value; }
    }


    public Vector2[] uvs
    {
        get { return _uvs; }
    }
    public Vector2 uv0
    {
        get { return _uvs[0]; }
    }
    public Vector2 uv1
    {
        get { return _uvs[1]; }
    }
    public Vector2 uv2
    {
        get { return _uvs[2]; }
    }


    public Vector3[] normals
    {
        get { return _normals; }
    }
    public Vector3 nv0
    {
        get { return _normals[0]; }
    }
    public Vector3 nv1
    {
        get { return _normals[1]; }
    }
    public Vector3 nv2
    {
        get { return _normals[2]; }
    }


    public Vector4[] tangents
    {
        get { return _tangents; }
    }
    public Vector4 tv0
    {
        get { return _tangents[0]; }
    }
    public Vector4 tv1
    {
        get { return _tangents[1]; }
    }
    public Vector4 tv2
    {
        get { return _tangents[2]; }
    }
}
