#ifndef __EVO_CMD2_H__
#define __EVO_CMD2_H__

#include <OpenGLES/ES2/gl.h>
#include "eVoTypes.h"
#include "eVoMthSVectors.h"
#include "eVo3D_SStructs.h"
#include "eVo3D_CTransform.h"
#include "eVo3D_CShaderManager.h"
#include "eVo3D_CModel.h"

#define MD2_IDENT	(('2'<<24) + ('P'<<16) + ('D'<<8) + 'I')
#define MD2_VERSION	8

struct SMD2header
{
  u32 ident;              // magic number. must be equal to "IDP2"
  u32 version;            // md2 version. must be equal to 8
  u32 skinwidth;          // width of the texture
  u32 skinheight;         // height of the texture
  u32 framesize;          // size of one frame in bytes
  u32 num_skins;          // number of textures
  u32 num_xyz;            // number of vertices
  u32 num_st;             // number of texture coordinates
  u32 num_tris;           // number of triangles
  u32 num_glcmds;         // number of opengl commands
  u32 num_frames;         // total number of frames
  u32 ofs_skins;          // offset to skin names (64 bytes each)
  u32 ofs_st;             // offset to s-t texture coordinates
  u32 ofs_tris;           // offset to triangles
  u32 ofs_frames;         // offset to frame data
  u32 ofs_glcmds;         // offset to opengl commands
  u32 ofs_end;            // offset to end of file
};

struct SMD2Vertex
{
  u8 x,y,z;               // compressed vertex (x, y, z) coordinates
  u8 lightNormalIndex;	  // "This is an index into a table of normals kept by Quake2"
};

struct SMD2Frame
{
  f32        scale[3];       // scale values
  f32        translate[3];   // translation vector
  u8         name[16];       // frame name
  SMD2Vertex verts;          // first vertex of this frame
};

struct SMD2Triangle
{
  u16 index_xyz[3];			 // indexes to triangle's vertices
  u16 index_st[3];           // indexes to vertices' texture coorinates
};

struct SUniversalVertex
{
  f32	mX,mY,mZ;
  f32	mU,mV;
  u32   mColor;
  f32	mNx,mNy,mNz;
};

class eVo3D_CFileFormatMd2 : public eVo3D_CTransform
{
	public:
	SMD2header	mHeader;
	u8*			mpRawFile;
	//............................................
		 eVo3D_CFileFormatMd2(void);
		 ~eVo3D_CFileFormatMd2(void);
	void			load(u8* apFileName);
	eVo3D_CModel*	convert(void);
	void			release(void);
};

extern eVoSVector3 eVoMD2LightNormalIndex[];

#endif //__EVO_CMD2_H__
