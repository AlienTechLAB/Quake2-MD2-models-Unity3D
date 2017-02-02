using System;
using System.Runtime.InteropServices;
using UnityEngine;

public struct MD2Header
{
    public UInt32 ident;              // magic number. must be equal to "IDP2"
    public UInt32 version;            // md2 version. must be equal to 8
    public UInt32 skinwidth;          // width of the texture
    public UInt32 skinheight;         // height of the texture
    public UInt32 framesize;          // size of one frame in bytes
    public UInt32 num_skins;          // number of textures
    public UInt32 num_xyz;            // number of vertices
    public UInt32 num_st;             // number of texture coordinates
    public UInt32 num_tris;           // number of triangles
    public UInt32 num_glcmds;         // number of opengl commands
    public UInt32 num_frames;         // total number of frames
    public UInt32 ofs_skins;          // offset to skin names (64 bytes each)
    public UInt32 ofs_st;             // offset to s-t texture coordinates
    public UInt32 ofs_tris;           // offset to triangles
    public UInt32 ofs_frames;         // offset to frame data
    public UInt32 ofs_glcmds;         // offset to opengl commands
    public UInt32 ofs_end;            // offset to end of file
};

public struct MD2Vertex
{
    public byte x, y, z;           // compressed vertex (x, y, z) coordinates
    public byte lightNormalIndex;  // "This is an index into a table of normals kept by Quake2"
};

public unsafe struct MD2FrameData
{
    public fixed float scale[3];        // scale values
    public fixed float translate[3];    // translation vector
    public fixed byte name[16];         // frame name
    public MD2Vertex verts;             // first vertex of this frame
};

public unsafe struct MD2Triangle
{
    public fixed ushort index_xyz[3];  // indexes to triangle's vertices
    public fixed ushort index_st[3];   // indexes to vertices' texture coorinates
};

public unsafe struct MD2TexCoord
{
    public short s;
    public short t;
};

public class MD2Frame
{
    public string Name;
    public Vector3[] Vertices = null;
    public Vector3[] Normals = null;
    public Vector2[] UVs = null;
    public MD2Triangle[] Triagles = null;
}

public class MD2File
{
    //---------------------------------------------------------------------------------------------------------

    private byte[]    RawFile = null;
    public  MD2Header Header;

    //---------------------------------------------------------------------------------------------------------

    public unsafe MD2File(byte[] md2File)
    {
        RawFile = md2File;

        fixed (byte* pRawData = &md2File[0])
        {
            Header = *(MD2Header*)pRawData;
        }
    }

    //---------------------------------------------------------------------------------------------------------

    public int GetFramesNo()
    {
        return (int)Header.num_frames;
    }

    //---------------------------------------------------------------------------------------------------------

    public MD2Frame[] GetAllFrames()
    {
        MD2Frame[] frames = new MD2Frame[GetFramesNo()];

        for (int frameNumber = 0; frameNumber < frames.Length; frameNumber++)
            frames[frameNumber] = GetFrame(frameNumber);

        return frames;
    }

    //---------------------------------------------------------------------------------------------------------

    public unsafe MD2Frame GetFrame(int frameNumber)
    {
        MD2Frame frame = new MD2Frame();
        frame.Vertices = ExtractVerticesFromFrame(frameNumber);
        frame.Normals = ExtractNormalsFromFrame(frameNumber);
        frame.UVs = ExtractSTsFromFile();
        frame.Triagles = ExtractAllTringles();
        frame.Name = ExtractNameFromFrame(frameNumber);
        return frame;
    }

    //---------------------------------------------------------------------------------------------------------

    private unsafe MD2Triangle[] ExtractAllTringles()
    {
        fixed (byte* pRawData = &RawFile[0])
        {
            MD2Triangle[] triangles = new MD2Triangle[Header.num_tris];

            for (int i = 0; i < Header.num_tris; i++)
            {
                MD2Triangle* pTriangle = (MD2Triangle*)&pRawData[i * sizeof(MD2Triangle) + Header.ofs_tris];
                triangles[i] = *pTriangle;
            }

            return triangles;
        }
    }

    //---------------------------------------------------------------------------------------------------------

    private unsafe Vector2[] ExtractSTsFromFile()
    {
        fixed (byte* pRawData = &RawFile[0])
        {
            Vector2[] uv = new Vector2[Header.num_st];

            for (int i = 0; i < Header.num_st; i++)
            {
                MD2TexCoord md2TexCoord = *((MD2TexCoord*)&pRawData[i * sizeof(MD2TexCoord) + Header.ofs_st]);
                uv[i].x = (float)md2TexCoord.s / (float)Header.skinwidth;
                uv[i].y = 1.0f - ((float)md2TexCoord.t / (float)Header.skinheight);
            }

            return uv;
        }
    }

    //---------------------------------------------------------------------------------------------------------

    private unsafe string ExtractNameFromFrame(int frameNumber)
    {
        fixed (byte* pRawData = &RawFile[0])
        {
            MD2FrameData* pFrame = (MD2FrameData*)&pRawData[frameNumber * Header.framesize + Header.ofs_frames];

            byte[] name = new byte[16];
            Marshal.Copy((IntPtr)pFrame->name, name, 0, name.Length);
            return System.Text.Encoding.Default.GetString(name);
        }
    }

    //---------------------------------------------------------------------------------------------------------

    private unsafe Vector3[] ExtractVerticesFromFrame(int frameNumber)
    {
        fixed (byte* pRawData = &RawFile[0])
        {
            MD2FrameData* pFrame = (MD2FrameData*)&pRawData[frameNumber * Header.framesize + Header.ofs_frames];
            Vector3 scale = new Vector3(pFrame->scale[0], pFrame->scale[1], pFrame->scale[2]);
            Vector3 translate = new Vector3(pFrame->translate[0], pFrame->translate[1], pFrame->translate[2]);
            Vector3[] vertices = new Vector3[Header.num_xyz];

            for (int i = 0; i < Header.num_xyz; i++)
            {
                MD2Vertex md2Vertex = ((MD2Vertex*)&pFrame->verts)[i];
                Vector3 vertex;
                vertex.x = -(md2Vertex.y * scale.y + translate.y);
                vertex.z = md2Vertex.x * scale.x + translate.x;
                vertex.y = md2Vertex.z * scale.z + translate.z;
                vertices[i] = vertex;
            }

            return vertices;
        }
    }

    //---------------------------------------------------------------------------------------------------------

    private unsafe Vector3[] ExtractNormalsFromFrame(int frameNumber)
    {
        fixed (byte* pRawData = &RawFile[0])
        {
            MD2FrameData* pFrame = (MD2FrameData*)&pRawData[frameNumber * Header.framesize + Header.ofs_frames];
            Vector3[] normals = new Vector3[Header.num_xyz];

            for (int i = 0; i < Header.num_xyz; i++)
            {
                MD2Vertex md2Vertex = ((MD2Vertex*)&pFrame->verts)[i];
                normals[i] = MD2LightNormalIndex[md2Vertex.lightNormalIndex];
            }

            return normals;
        }
    }

    //---------------------------------------------------------------------------------------------------------
    // Table of normals from anorms.h from Quake 2. Quake 2 uses constant normals, not calculated for each vertex.
    private static Vector3[] MD2LightNormalIndex = {
        new Vector3(-0.525731f,  0.000000f,  0.850651f),
        new Vector3(-0.442863f,  0.238856f,  0.864188f),
        new Vector3(-0.295242f,  0.000000f,  0.955423f),
        new Vector3(-0.309017f,  0.500000f,  0.809017f),
        new Vector3(-0.162460f,  0.262866f,  0.951056f),
        new Vector3( 0.000000f,  0.000000f,  1.000000f),
        new Vector3( 0.000000f,  0.850651f,  0.525731f),
        new Vector3(-0.147621f,  0.716567f,  0.681718f),
        new Vector3( 0.147621f,  0.716567f,  0.681718f),
        new Vector3( 0.000000f,  0.525731f,  0.850651f),
        new Vector3( 0.309017f,  0.500000f,  0.809017f),
        new Vector3( 0.525731f,  0.000000f,  0.850651f),
        new Vector3( 0.295242f,  0.000000f,  0.955423f),
        new Vector3( 0.442863f,  0.238856f,  0.864188f),
        new Vector3( 0.162460f,  0.262866f,  0.951056f),
        new Vector3(-0.681718f,  0.147621f,  0.716567f),
        new Vector3(-0.809017f,  0.309017f,  0.500000f),
        new Vector3(-0.587785f,  0.425325f,  0.688191f),
        new Vector3(-0.850651f,  0.525731f,  0.000000f),
        new Vector3(-0.864188f,  0.442863f,  0.238856f),
        new Vector3(-0.716567f,  0.681718f,  0.147621f),
        new Vector3(-0.688191f,  0.587785f,  0.425325f),
        new Vector3(-0.500000f,  0.809017f,  0.309017f),
        new Vector3(-0.238856f,  0.864188f,  0.442863f),
        new Vector3(-0.425325f,  0.688191f,  0.587785f),
        new Vector3(-0.716567f,  0.681718f, -0.147621f),
        new Vector3(-0.500000f,  0.809017f, -0.309017f),
        new Vector3(-0.525731f,  0.850651f,  0.000000f),
        new Vector3( 0.000000f,  0.850651f, -0.525731f),
        new Vector3(-0.238856f,  0.864188f, -0.442863f),
        new Vector3( 0.000000f,  0.955423f, -0.295242f),
        new Vector3(-0.262866f,  0.951056f, -0.162460f),
        new Vector3( 0.000000f,  1.000000f,  0.000000f),
        new Vector3( 0.000000f,  0.955423f,  0.295242f),
        new Vector3(-0.262866f,  0.951056f,  0.162460f),
        new Vector3( 0.238856f,  0.864188f,  0.442863f),
        new Vector3( 0.262866f,  0.951056f,  0.162460f),
        new Vector3( 0.500000f,  0.809017f,  0.309017f),
        new Vector3( 0.238856f,  0.864188f, -0.442863f),
        new Vector3( 0.262866f,  0.951056f, -0.162460f),
        new Vector3( 0.500000f,  0.809017f, -0.309017f),
        new Vector3( 0.850651f,  0.525731f,  0.000000f),
        new Vector3( 0.716567f,  0.681718f,  0.147621f),
        new Vector3( 0.716567f,  0.681718f, -0.147621f),
        new Vector3( 0.525731f,  0.850651f,  0.000000f),
        new Vector3( 0.425325f,  0.688191f,  0.587785f),
        new Vector3( 0.864188f,  0.442863f,  0.238856f),
        new Vector3( 0.688191f,  0.587785f,  0.425325f),
        new Vector3( 0.809017f,  0.309017f,  0.500000f),
        new Vector3( 0.681718f,  0.147621f,  0.716567f),
        new Vector3( 0.587785f,  0.425325f,  0.688191f),
        new Vector3( 0.955423f,  0.295242f,  0.000000f),
        new Vector3( 1.000000f,  0.000000f,  0.000000f),
        new Vector3( 0.951056f,  0.162460f,  0.262866f),
        new Vector3( 0.850651f, -0.525731f,  0.000000f),
        new Vector3( 0.955423f, -0.295242f,  0.000000f),
        new Vector3( 0.864188f, -0.442863f,  0.238856f),
        new Vector3( 0.951056f, -0.162460f,  0.262866f),
        new Vector3( 0.809017f, -0.309017f,  0.500000f),
        new Vector3( 0.681718f, -0.147621f,  0.716567f),
        new Vector3( 0.850651f,  0.000000f,  0.525731f),
        new Vector3( 0.864188f,  0.442863f, -0.238856f),
        new Vector3( 0.809017f,  0.309017f, -0.500000f),
        new Vector3( 0.951056f,  0.162460f, -0.262866f),
        new Vector3( 0.525731f,  0.000000f, -0.850651f),
        new Vector3( 0.681718f,  0.147621f, -0.716567f),
        new Vector3( 0.681718f, -0.147621f, -0.716567f),
        new Vector3( 0.850651f,  0.000000f, -0.525731f),
        new Vector3( 0.809017f, -0.309017f, -0.500000f),
        new Vector3( 0.864188f, -0.442863f, -0.238856f),
        new Vector3( 0.951056f, -0.162460f, -0.262866f),
        new Vector3( 0.147621f,  0.716567f, -0.681718f),
        new Vector3( 0.309017f,  0.500000f, -0.809017f),
        new Vector3( 0.425325f,  0.688191f, -0.587785f),
        new Vector3( 0.442863f,  0.238856f, -0.864188f),
        new Vector3( 0.587785f,  0.425325f, -0.688191f),
        new Vector3( 0.688191f,  0.587785f, -0.425325f),
        new Vector3(-0.147621f,  0.716567f, -0.681718f),
        new Vector3(-0.309017f,  0.500000f, -0.809017f),
        new Vector3( 0.000000f,  0.525731f, -0.850651f),
        new Vector3(-0.525731f,  0.000000f, -0.850651f),
        new Vector3(-0.442863f,  0.238856f, -0.864188f),
        new Vector3(-0.295242f,  0.000000f, -0.955423f),
        new Vector3(-0.162460f,  0.262866f, -0.951056f),
        new Vector3( 0.000000f,  0.000000f, -1.000000f),
        new Vector3( 0.295242f,  0.000000f, -0.955423f),
        new Vector3( 0.162460f,  0.262866f, -0.951056f),
        new Vector3(-0.442863f, -0.238856f, -0.864188f),
        new Vector3(-0.309017f, -0.500000f, -0.809017f),
        new Vector3(-0.162460f, -0.262866f, -0.951056f),
        new Vector3( 0.000000f, -0.850651f, -0.525731f),
        new Vector3(-0.147621f, -0.716567f, -0.681718f),
        new Vector3( 0.147621f, -0.716567f, -0.681718f),
        new Vector3( 0.000000f, -0.525731f, -0.850651f),
        new Vector3( 0.309017f, -0.500000f, -0.809017f),
        new Vector3( 0.442863f, -0.238856f, -0.864188f),
        new Vector3( 0.162460f, -0.262866f, -0.951056f),
        new Vector3( 0.238856f, -0.864188f, -0.442863f),
        new Vector3( 0.500000f, -0.809017f, -0.309017f),
        new Vector3( 0.425325f, -0.688191f, -0.587785f),
        new Vector3( 0.716567f, -0.681718f, -0.147621f),
        new Vector3( 0.688191f, -0.587785f, -0.425325f),
        new Vector3( 0.587785f, -0.425325f, -0.688191f),
        new Vector3( 0.000000f, -0.955423f, -0.295242f),
        new Vector3( 0.000000f, -1.000000f,  0.000000f),
        new Vector3( 0.262866f, -0.951056f, -0.162460f),
        new Vector3( 0.000000f, -0.850651f,  0.525731f),
        new Vector3( 0.000000f, -0.955423f,  0.295242f),
        new Vector3( 0.238856f, -0.864188f,  0.442863f),
        new Vector3( 0.262866f, -0.951056f,  0.162460f),
        new Vector3( 0.500000f, -0.809017f,  0.309017f),
        new Vector3( 0.716567f, -0.681718f,  0.147621f),
        new Vector3( 0.525731f, -0.850651f,  0.000000f),
        new Vector3(-0.238856f, -0.864188f, -0.442863f),
        new Vector3(-0.500000f, -0.809017f, -0.309017f),
        new Vector3(-0.262866f, -0.951056f, -0.162460f),
        new Vector3(-0.850651f, -0.525731f,  0.000000f),
        new Vector3(-0.716567f, -0.681718f, -0.147621f),
        new Vector3(-0.716567f, -0.681718f,  0.147621f),
        new Vector3(-0.525731f, -0.850651f,  0.000000f),
        new Vector3(-0.500000f, -0.809017f,  0.309017f),
        new Vector3(-0.238856f, -0.864188f,  0.442863f),
        new Vector3(-0.262866f, -0.951056f,  0.162460f),
        new Vector3(-0.864188f, -0.442863f,  0.238856f),
        new Vector3(-0.809017f, -0.309017f,  0.500000f),
        new Vector3(-0.688191f, -0.587785f,  0.425325f),
        new Vector3(-0.681718f, -0.147621f,  0.716567f),
        new Vector3(-0.442863f, -0.238856f,  0.864188f),
        new Vector3(-0.587785f, -0.425325f,  0.688191f),
        new Vector3(-0.309017f, -0.500000f,  0.809017f),
        new Vector3(-0.147621f, -0.716567f,  0.681718f),
        new Vector3(-0.425325f, -0.688191f,  0.587785f),
        new Vector3(-0.162460f, -0.262866f,  0.951056f),
        new Vector3( 0.442863f, -0.238856f,  0.864188f),
        new Vector3( 0.162460f, -0.262866f,  0.951056f),
        new Vector3( 0.309017f, -0.500000f,  0.809017f),
        new Vector3( 0.147621f, -0.716567f,  0.681718f),
        new Vector3( 0.000000f, -0.525731f,  0.850651f),
        new Vector3( 0.425325f, -0.688191f,  0.587785f),
        new Vector3( 0.587785f, -0.425325f,  0.688191f),
        new Vector3( 0.688191f, -0.587785f,  0.425325f),
        new Vector3(-0.955423f,  0.295242f,  0.000000f),
        new Vector3(-0.951056f,  0.162460f,  0.262866f),
        new Vector3(-1.000000f,  0.000000f,  0.000000f),
        new Vector3(-0.850651f,  0.000000f,  0.525731f),
        new Vector3(-0.955423f, -0.295242f,  0.000000f),
        new Vector3(-0.951056f, -0.162460f,  0.262866f),
        new Vector3(-0.864188f,  0.442863f, -0.238856f),
        new Vector3(-0.951056f,  0.162460f, -0.262866f),
        new Vector3(-0.809017f,  0.309017f, -0.500000f),
        new Vector3(-0.864188f, -0.442863f, -0.238856f),
        new Vector3(-0.951056f, -0.162460f, -0.262866f),
        new Vector3(-0.809017f, -0.309017f, -0.500000f),
        new Vector3(-0.681718f,  0.147621f, -0.716567f),
        new Vector3(-0.681718f, -0.147621f, -0.716567f),
        new Vector3(-0.850651f,  0.000000f, -0.525731f),
        new Vector3(-0.688191f,  0.587785f, -0.425325f),
        new Vector3(-0.587785f,  0.425325f, -0.688191f),
        new Vector3(-0.425325f,  0.688191f, -0.587785f),
        new Vector3(-0.425325f, -0.688191f, -0.587785f),
        new Vector3(-0.587785f, -0.425325f, -0.688191f),
        new Vector3(-0.688191f, -0.587785f, -0.425325f)
    };

    //---------------------------------------------------------------------------------------------------------
}
