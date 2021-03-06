﻿using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Rendering;

namespace com.jackie2009.scrollStaticShadowmap
{
	


public class StaticShadowCaster : MonoBehaviour
{
 	public Shader castShader;
	public Shader mergeDepthShader;
	public Shader screenSpaceShadowsShader;
	public LightShadowResolution shadowResolution;
	private Camera cmr;
	public RenderTexture shadowmap;
[Range(0,2)]
	public float bias=0.05f;
	[Range(0,3)]
	public float normalBias=0.4f;
 	// Use this for initialization
    private int currentRenderIndex=0;
    private Matrix4x4[] lightProjecionMatrixs=new Matrix4x4[10];
    private Material mergeMat;
    private void Awake()
    {
	     cmr = GetComponent<Camera>();
	     mergeMat=new Material(mergeDepthShader);
		print( SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.Shadowmap));
		print(SystemInfo.supports2DArrayTextures);
		print((SystemInfo.copyTextureSupport & CopyTextureSupport.DifferentTypes) != 0);
		print((SystemInfo.copyTextureSupport & CopyTextureSupport.Copy3D) != 0);
	
		GraphicsSettings.SetCustomShader(BuiltinShaderType.ScreenSpaceShadows,screenSpaceShadowsShader);
	}



	private void OnEnable()
	{
		castShadow();
	}

	private void OnDisable()
	{
		cmr.targetTexture = null;
		shadowmap.Release();
		shadowmap = null;
		Shader.SetGlobalFloat("_shadowmapEnable", 0);
		Shader.SetGlobalTexture("_shadowmap",null);
		foreach (var renderer in FindObjectsOfType<Renderer>())
		{
					 
			if(renderer.gameObject.CompareTag("staticShadowmap"))
				renderer.shadowCastingMode = ShadowCastingMode.On;
		}
	}

	void castShadow()
	{
		/*
		rtArray.dimension = TextureDimension.Tex2DArray;
		rtArray.volumeDepth = 9;
		rtArray.Create();
		*/
		if (shadowResolution == LightShadowResolution.FromQualitySettings)
			shadowResolution = (LightShadowResolution)QualitySettings.shadowResolution;
		int rtSize = (1 << (int)shadowResolution) * 1024;
 
 			shadowmap= new RenderTexture(rtSize, rtSize, 16/*depth*/, RenderTextureFormat.Shadowmap, RenderTextureReadWrite.Linear);
 
	 	shadowmap.useMipMap = false;
	 shadowmap.autoGenerateMips = false;
	 	shadowmap.filterMode = FilterMode.Bilinear;
		 shadowmap.name = "StaticShadowmap";
		 shadowmap.Create();
		
		cmr.enabled = false;
		 //cmr.targetTexture = shadowmap;
		 
        Shader.SetGlobalTexture("_shadowmap",shadowmap);
        Shader.SetGlobalFloat("_shadowmapEnable", 1);
		 foreach (var renderer in FindObjectsOfType<Renderer>())
		 {
			 //一定要留至少一个对象投射阴影 否则整个阴影流程会被引擎 优化掉 直接跳过我们的静态阴影
		 if(renderer.gameObject.CompareTag("staticShadowmap"))
			  renderer.shadowCastingMode = ShadowCastingMode.Off;
		 }


		 renderShadow();
	}

	private void renderShadow()
	{
		Shader.SetGlobalFloat("_shadowmapBias", bias);
		Shader.SetGlobalFloat("_shadowmapNormalBias", normalBias);

		     cmr.RenderWithShader(castShader,"");
        		 UpdateMatrix();
	}

	private void OnDestroy()
	{
		Shader.SetGlobalFloat("_shadowmapEnable", 0);
		Shader.SetGlobalTexture("_shadowmap",null);
	}

 

	private void OnRenderImage(RenderTexture src, RenderTexture dest)
	{
		//;
		if (shadowmap == null) return;
 
		 mergeMat.SetTexture("_rtTex",src);
		 mergeMat.SetInt("_rtID", currentRenderIndex);// new Vector4(destX,destY,(float)cmr.targetTexture.width/shadowmap.width,0));
		 Graphics.Blit(null,shadowmap,mergeMat);
		
		 
	}

	private void UpdateMatrix()
	{
	
		Matrix4x4 worldToView = cmr.worldToCameraMatrix;
		Matrix4x4 projection  = GL.GetGPUProjectionMatrix(cmr.projectionMatrix, false);
		Matrix4x4 lightProjecionMatrix =  projection * worldToView;
		//Shader.SetGlobalMatrix ("_LightProjection", lightProjecionMatrix);
 
		lightProjecionMatrixs[currentRenderIndex] = lightProjecionMatrix;		
 
		Shader.SetGlobalMatrixArray("_LightProjections", lightProjecionMatrixs);
		print(currentRenderIndex);
		 
 	}


	public void renderWithIndex(int index,Vector3 pos)
	{
		if (shadowmap == null)
		{
			return;
		}

		 
		pos.y = 0;
		transform.parent.position = pos;
		currentRenderIndex = index;
 
		int rtSize = shadowmap.width*4/5;
		if (index != 0) rtSize = shadowmap.width/5;
		if (cmr.targetTexture != null)
		{
		//	var tempRT = cmr.targetTexture;
			//RenderTexture.ReleaseTemporary(tempRT);
		}

		var tempRT =cmr.targetTexture = RenderTexture.GetTemporary(rtSize, rtSize, 16, shadowmap.format);		
 
		renderShadow();
		
 
		 
		  cmr.targetTexture =null;
		  RenderTexture.ReleaseTemporary(tempRT);
 
		
	}
}
}