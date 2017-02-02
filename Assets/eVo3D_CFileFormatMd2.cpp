#include <stdio.h>
#include <string.h>
#include <stdlib.h>
#include "eVoDefines.h"
#include "eVo3D_CFileFormatMd2.h"
#include "eVo3D_SStructs.h"
#include "eVoExterns.h"

eVo3D_CFileFormatMd2::eVo3D_CFileFormatMd2(void)
{
	memset(&mHeader,0,sizeof(mHeader));
	mpRawFile = NULL;
}

eVo3D_CFileFormatMd2::~eVo3D_CFileFormatMd2(void)
{
	release();
}

void eVo3D_CFileFormatMd2::load(u8* apFileName)
{
	release();
	mpRawFile = (u8*)eVoFileSystem.loadFile(apFileName);
	mHeader = *((SMD2header*)mpRawFile);
}

eVo3D_CModel* eVo3D_CFileFormatMd2::convert(void)
{	
	eVo3D_CModel*	pModel		= NULL;
	eVo3D_CMesh*	pMeshTable	= NULL;
	eVoSVector3*	pVertices	= NULL;
	eVoSVector3*	pNormals	= NULL;
	eVoSVector2*	pUVs		= NULL;
	//.....................................
	pModel = new eVo3D_CModel();
	pMeshTable = new eVo3D_CMesh[mHeader.num_frames];
	pModel->setMeshes(pMeshTable,mHeader.num_frames);
	
	for(u32 f = 0;f < mHeader.num_frames;f++){
		SMD2Frame* pF = (SMD2Frame*)&mpRawFile[f * mHeader.framesize + mHeader.ofs_frames];
		
		pVertices = new eVoSVector3[mHeader.num_tris * 3];
		pNormals = new eVoSVector3[mHeader.num_tris * 3];
		pUVs = new eVoSVector2[mHeader.num_tris * 3];
		
		for(u32 t = 0;t < mHeader.num_tris;t++){
			SMD2Triangle* pTriangle = (SMD2Triangle*)&mpRawFile[t * sizeof(SMD2Triangle) + mHeader.ofs_tris];

			for(u32 v=0;v<3;v++){
				u32 iV = pTriangle->index_xyz[v];
				u32 iT = pTriangle->index_st[v];
			
				SMD2Vertex md2Vertex = ((SMD2Vertex*)&pF->verts)[iV];
				eVo3D_STexelCoordu md2TexelCoords = *((eVo3D_STexelCoordu*)&mpRawFile[iT * sizeof(eVo3D_STexelCoordu) + mHeader.ofs_st]);
				eVoSVector3 normal = eVoMD2LightNormalIndex[md2Vertex.lightNormalIndex];
				
				eVoSVector3 vertex;
				vertex.x = md2Vertex.x * pF->scale[0] + pF->translate[0];
				vertex.z = md2Vertex.y * pF->scale[1] + pF->translate[1];
				vertex.y = md2Vertex.z * pF->scale[2] + pF->translate[2];
				
				eVoSVector2 uv;
				uv.x = (f32)md2TexelCoords.u / 512.0f;
				uv.y = (f32)md2TexelCoords.v / 512.0f;
				
				pVertices[t*3+v] = vertex;
				pNormals[t*3+v] = normal;
				pUVs[t*3+v] = uv;
			}
		}
		
		pMeshTable[f].setTrianglesNo(mHeader.num_tris);
		pMeshTable[f].setVertices(pVertices,mHeader.num_tris * 3);
		pMeshTable[f].setNormals(pNormals,mHeader.num_tris * 3);
		pMeshTable[f].setUVs(pUVs,mHeader.num_tris * 3);
		pMeshTable[f].setName(pF->name);
		pMeshTable[f].boundingBox();
	}
	
	// Dodawanie animacji.
	u32 anims[1024];
	u32 animCounter = 0;
	memset(anims,0,sizeof(anims));
	u8 name1[16];
	u8 name2[16];
	memcpy(name2,pMeshTable[0].getName(),16);
	for(u32 i = 0;i < sizeof(name2);i++){
		if(name2[i] >= '0' && name2[i] <= '9'){
			name2[i] = 'x';
		}
	}
	memcpy(name1,name2,16);
	eVo3D_CAnimation* pAnim = NULL;
	
	for(u32 i = 0;i < mHeader.num_frames;i++){
		memcpy(name2,pMeshTable[i].getName(),16);
		for(u32 i = 0;i < sizeof(name2);i++){
			if(name2[i] >= '0' && name2[i] <= '9'){
				name2[i] = 'x';
			}
		}
		if(strcmp((char*)name1,(char*)name2) == 0){
			anims[animCounter]++;
		}
		else{
			animCounter++;
			anims[animCounter]++;
			memcpy(name1,name2,sizeof(name1));
		}
	}
	
	u32 startframe = 0;
	eVo3D_CAnimation* pAnimations = new eVo3D_CAnimation[animCounter];
	for(u32 i = 0;i < animCounter;i++){
		pAnimations[i].mFramesNo = anims[i];
		pAnimations[i].mStartFrame = startframe;
		pAnimations[i].setFPS(30);
		memcpy(pAnimations[i].mName,pMeshTable[startframe].getName(),sizeof(pAnimations[i].mName));
		startframe += anims[i];
	}

	pModel->setAnimations(pAnimations,animCounter);
	
	f32 s = 4.0f / pMeshTable[0].mMaxDimension;
	pModel->setScale(s,s,s);
	
	return pModel;
}

void eVo3D_CFileFormatMd2::release(void)
{
	if(mpRawFile){
		delete[] mpRawFile;
		mpRawFile = NULL;
	}
	memset(&mHeader,0,sizeof(mHeader));
}

// Table of normals from anorms.h from Quake 2. Quake 2 uses constant normals, not calculated for each vertex.
eVoSVector3 eVoMD2LightNormalIndex[]={
{ -0.525731f,  0.000000f,  0.850651f }, 
{ -0.442863f,  0.238856f,  0.864188f }, 
{ -0.295242f,  0.000000f,  0.955423f }, 
{ -0.309017f,  0.500000f,  0.809017f }, 
{ -0.162460f,  0.262866f,  0.951056f }, 
{  0.000000f,  0.000000f,  1.000000f }, 
{  0.000000f,  0.850651f,  0.525731f }, 
{ -0.147621f,  0.716567f,  0.681718f }, 
{  0.147621f,  0.716567f,  0.681718f }, 
{  0.000000f,  0.525731f,  0.850651f }, 
{  0.309017f,  0.500000f,  0.809017f }, 
{  0.525731f,  0.000000f,  0.850651f }, 
{  0.295242f,  0.000000f,  0.955423f }, 
{  0.442863f,  0.238856f,  0.864188f }, 
{  0.162460f,  0.262866f,  0.951056f }, 
{ -0.681718f,  0.147621f,  0.716567f }, 
{ -0.809017f,  0.309017f,  0.500000f }, 
{ -0.587785f,  0.425325f,  0.688191f }, 
{ -0.850651f,  0.525731f,  0.000000f }, 
{ -0.864188f,  0.442863f,  0.238856f }, 
{ -0.716567f,  0.681718f,  0.147621f }, 
{ -0.688191f,  0.587785f,  0.425325f }, 
{ -0.500000f,  0.809017f,  0.309017f }, 
{ -0.238856f,  0.864188f,  0.442863f }, 
{ -0.425325f,  0.688191f,  0.587785f }, 
{ -0.716567f,  0.681718f, -0.147621f }, 
{ -0.500000f,  0.809017f, -0.309017f }, 
{ -0.525731f,  0.850651f,  0.000000f }, 
{  0.000000f,  0.850651f, -0.525731f }, 
{ -0.238856f,  0.864188f, -0.442863f }, 
{  0.000000f,  0.955423f, -0.295242f }, 
{ -0.262866f,  0.951056f, -0.162460f }, 
{  0.000000f,  1.000000f,  0.000000f }, 
{  0.000000f,  0.955423f,  0.295242f }, 
{ -0.262866f,  0.951056f,  0.162460f }, 
{  0.238856f,  0.864188f,  0.442863f }, 
{  0.262866f,  0.951056f,  0.162460f }, 
{  0.500000f,  0.809017f,  0.309017f }, 
{  0.238856f,  0.864188f, -0.442863f }, 
{  0.262866f,  0.951056f, -0.162460f }, 
{  0.500000f,  0.809017f, -0.309017f }, 
{  0.850651f,  0.525731f,  0.000000f }, 
{  0.716567f,  0.681718f,  0.147621f }, 
{  0.716567f,  0.681718f, -0.147621f }, 
{  0.525731f,  0.850651f,  0.000000f }, 
{  0.425325f,  0.688191f,  0.587785f }, 
{  0.864188f,  0.442863f,  0.238856f }, 
{  0.688191f,  0.587785f,  0.425325f }, 
{  0.809017f,  0.309017f,  0.500000f }, 
{  0.681718f,  0.147621f,  0.716567f }, 
{  0.587785f,  0.425325f,  0.688191f }, 
{  0.955423f,  0.295242f,  0.000000f }, 
{  1.000000f,  0.000000f,  0.000000f }, 
{  0.951056f,  0.162460f,  0.262866f }, 
{  0.850651f, -0.525731f,  0.000000f }, 
{  0.955423f, -0.295242f,  0.000000f }, 
{  0.864188f, -0.442863f,  0.238856f }, 
{  0.951056f, -0.162460f,  0.262866f }, 
{  0.809017f, -0.309017f,  0.500000f }, 
{  0.681718f, -0.147621f,  0.716567f }, 
{  0.850651f,  0.000000f,  0.525731f }, 
{  0.864188f,  0.442863f, -0.238856f }, 
{  0.809017f,  0.309017f, -0.500000f }, 
{  0.951056f,  0.162460f, -0.262866f }, 
{  0.525731f,  0.000000f, -0.850651f }, 
{  0.681718f,  0.147621f, -0.716567f }, 
{  0.681718f, -0.147621f, -0.716567f }, 
{  0.850651f,  0.000000f, -0.525731f }, 
{  0.809017f, -0.309017f, -0.500000f }, 
{  0.864188f, -0.442863f, -0.238856f }, 
{  0.951056f, -0.162460f, -0.262866f }, 
{  0.147621f,  0.716567f, -0.681718f }, 
{  0.309017f,  0.500000f, -0.809017f }, 
{  0.425325f,  0.688191f, -0.587785f }, 
{  0.442863f,  0.238856f, -0.864188f }, 
{  0.587785f,  0.425325f, -0.688191f }, 
{  0.688191f,  0.587785f, -0.425325f }, 
{ -0.147621f,  0.716567f, -0.681718f }, 
{ -0.309017f,  0.500000f, -0.809017f }, 
{  0.000000f,  0.525731f, -0.850651f }, 
{ -0.525731f,  0.000000f, -0.850651f }, 
{ -0.442863f,  0.238856f, -0.864188f }, 
{ -0.295242f,  0.000000f, -0.955423f }, 
{ -0.162460f,  0.262866f, -0.951056f }, 
{  0.000000f,  0.000000f, -1.000000f }, 
{  0.295242f,  0.000000f, -0.955423f }, 
{  0.162460f,  0.262866f, -0.951056f }, 
{ -0.442863f, -0.238856f, -0.864188f }, 
{ -0.309017f, -0.500000f, -0.809017f }, 
{ -0.162460f, -0.262866f, -0.951056f }, 
{  0.000000f, -0.850651f, -0.525731f }, 
{ -0.147621f, -0.716567f, -0.681718f }, 
{  0.147621f, -0.716567f, -0.681718f }, 
{  0.000000f, -0.525731f, -0.850651f }, 
{  0.309017f, -0.500000f, -0.809017f }, 
{  0.442863f, -0.238856f, -0.864188f }, 
{  0.162460f, -0.262866f, -0.951056f }, 
{  0.238856f, -0.864188f, -0.442863f }, 
{  0.500000f, -0.809017f, -0.309017f }, 
{  0.425325f, -0.688191f, -0.587785f }, 
{  0.716567f, -0.681718f, -0.147621f }, 
{  0.688191f, -0.587785f, -0.425325f }, 
{  0.587785f, -0.425325f, -0.688191f }, 
{  0.000000f, -0.955423f, -0.295242f }, 
{  0.000000f, -1.000000f,  0.000000f }, 
{  0.262866f, -0.951056f, -0.162460f }, 
{  0.000000f, -0.850651f,  0.525731f }, 
{  0.000000f, -0.955423f,  0.295242f }, 
{  0.238856f, -0.864188f,  0.442863f }, 
{  0.262866f, -0.951056f,  0.162460f }, 
{  0.500000f, -0.809017f,  0.309017f }, 
{  0.716567f, -0.681718f,  0.147621f }, 
{  0.525731f, -0.850651f,  0.000000f }, 
{ -0.238856f, -0.864188f, -0.442863f }, 
{ -0.500000f, -0.809017f, -0.309017f }, 
{ -0.262866f, -0.951056f, -0.162460f }, 
{ -0.850651f, -0.525731f,  0.000000f }, 
{ -0.716567f, -0.681718f, -0.147621f }, 
{ -0.716567f, -0.681718f,  0.147621f }, 
{ -0.525731f, -0.850651f,  0.000000f }, 
{ -0.500000f, -0.809017f,  0.309017f }, 
{ -0.238856f, -0.864188f,  0.442863f }, 
{ -0.262866f, -0.951056f,  0.162460f }, 
{ -0.864188f, -0.442863f,  0.238856f }, 
{ -0.809017f, -0.309017f,  0.500000f }, 
{ -0.688191f, -0.587785f,  0.425325f }, 
{ -0.681718f, -0.147621f,  0.716567f }, 
{ -0.442863f, -0.238856f,  0.864188f }, 
{ -0.587785f, -0.425325f,  0.688191f }, 
{ -0.309017f, -0.500000f,  0.809017f }, 
{ -0.147621f, -0.716567f,  0.681718f }, 
{ -0.425325f, -0.688191f,  0.587785f }, 
{ -0.162460f, -0.262866f,  0.951056f }, 
{  0.442863f, -0.238856f,  0.864188f }, 
{  0.162460f, -0.262866f,  0.951056f }, 
{  0.309017f, -0.500000f,  0.809017f }, 
{  0.147621f, -0.716567f,  0.681718f }, 
{  0.000000f, -0.525731f,  0.850651f }, 
{  0.425325f, -0.688191f,  0.587785f }, 
{  0.587785f, -0.425325f,  0.688191f }, 
{  0.688191f, -0.587785f,  0.425325f }, 
{ -0.955423f,  0.295242f,  0.000000f }, 
{ -0.951056f,  0.162460f,  0.262866f }, 
{ -1.000000f,  0.000000f,  0.000000f }, 
{ -0.850651f,  0.000000f,  0.525731f }, 
{ -0.955423f, -0.295242f,  0.000000f }, 
{ -0.951056f, -0.162460f,  0.262866f }, 
{ -0.864188f,  0.442863f, -0.238856f }, 
{ -0.951056f,  0.162460f, -0.262866f }, 
{ -0.809017f,  0.309017f, -0.500000f }, 
{ -0.864188f, -0.442863f, -0.238856f }, 
{ -0.951056f, -0.162460f, -0.262866f }, 
{ -0.809017f, -0.309017f, -0.500000f }, 
{ -0.681718f,  0.147621f, -0.716567f }, 
{ -0.681718f, -0.147621f, -0.716567f }, 
{ -0.850651f,  0.000000f, -0.525731f }, 
{ -0.688191f,  0.587785f, -0.425325f }, 
{ -0.587785f,  0.425325f, -0.688191f }, 
{ -0.425325f,  0.688191f, -0.587785f }, 
{ -0.425325f, -0.688191f, -0.587785f }, 
{ -0.587785f, -0.425325f, -0.688191f }, 
{ -0.688191f, -0.587785f, -0.425325f }
};
