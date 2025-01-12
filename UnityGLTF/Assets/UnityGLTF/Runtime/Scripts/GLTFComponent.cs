using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using UnityGLTF.Loader;

namespace UnityGLTF
{
    /// <summary>
    /// Component to load a GLTF scene with
    /// </summary>
    public class GLTFComponent : MonoBehaviour
	{
		public string GLTFUri = null;
		public bool Multithreaded = true;
		public bool UseStream = false;
		public bool AppendStreamingAssets = true;
		public bool PlayAnimationOnLoad = true;
        public ImporterFactory Factory = null;
        public UnityAction onLoadComplete;

#if UNITY_ANIMATION
        public IEnumerable<Animation> Animations { get; private set; }
#endif

		[SerializeField]
		private bool loadOnStart = true;

		[SerializeField] private int RetryCount = 10;
		[SerializeField] private float RetryTimeout = 2.0f;
		private int numRetries = 0;


		public int MaximumLod = 300;
		public int Timeout = 8;
		public GLTFSceneImporter.ColliderType Collider = GLTFSceneImporter.ColliderType.None;
		public GameObject LastLoadedScene { get; private set; } = null;

		[SerializeField]
		private Shader shaderOverride = null;

		private async void Start()
		{
			if (!loadOnStart) return;

			try
			{
				await Load();
			}
#if WINDOWS_UWP
			catch (Exception)
#else
			catch (HttpRequestException)
#endif
			{
				if (numRetries++ >= RetryCount)
					throw;

				Debug.LogWarning("Load failed, retrying");
				await Task.Delay((int)(RetryTimeout * 1000));
				Start();
			}
		}

		public async Task Load()
		{
			var importOptions = new ImportOptions
			{
				AsyncCoroutineHelper = gameObject.GetComponent<AsyncCoroutineHelper>() ?? gameObject.AddComponent<AsyncCoroutineHelper>()
			};

			GLTFSceneImporter sceneImporter = null;
			try
			{
                Factory = Factory ?? ScriptableObject.CreateInstance<DefaultImporterFactory>();

                // UseStream is currently not supported...
                string fullPath;
                if (AppendStreamingAssets)
	                fullPath = Path.Combine(Application.persistentDataPath, GLTFUri.TrimStart(new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar }));
                else
	                fullPath = GLTFUri;

                string dir = URIHelper.GetDirectoryName(fullPath);
                importOptions.DataLoader = new UnityWebRequestLoader(dir);
                sceneImporter = Factory.CreateSceneImporter(
	                Path.GetFileName(fullPath),
	                importOptions
                );

                sceneImporter.SceneParent = gameObject.transform;
				sceneImporter.Collider = Collider;
				sceneImporter.MaximumLod = MaximumLod;
				sceneImporter.Timeout = Timeout;
				sceneImporter.IsMultithreaded = Multithreaded;
				sceneImporter.CustomShaderName = shaderOverride ? shaderOverride.name : null;

				// for logging progress
				await sceneImporter.LoadSceneAsync(
					onLoadComplete:LoadCompleteAction
					// ,progress: new Progress<ImportProgress>(
					// 	p =>
					// 	{
					// 		Debug.Log("Progress: " + p);
					// 	})
				);

				var component = sceneImporter.CreatedObject.GetComponent(Type.GetType("Kluest.GLTFAnimator, Assembly-CSharp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"));
				if (component != null)
				{
					component.GetType().GetProperty("CreatedAnimationClips").SetValue(component, sceneImporter.CreatedAnimationClips);
				}

				// Override the shaders on all materials if a shader is provided
				if (shaderOverride != null)
				{
					Renderer[] renderers = gameObject.GetComponentsInChildren<Renderer>();
					foreach (Renderer renderer in renderers)
					{
						renderer.sharedMaterial.shader = shaderOverride;
					}
				}

				LastLoadedScene = sceneImporter.LastLoadedScene;

#if UNITY_ANIMATION
				Animations = sceneImporter.LastLoadedScene.GetComponents<Animation>();

				if (PlayAnimationOnLoad && Animations.Any())
				{
					Animations.First().Play();
				}
#endif
			}
			finally
			{
				if(importOptions.DataLoader != null)
				{
					sceneImporter?.Dispose();
					sceneImporter = null;
					importOptions.DataLoader = null;
				}
			}
		}

		private void LoadCompleteAction(GameObject obj, ExceptionDispatchInfo exceptionDispatchInfo)
		{
			onLoadComplete?.Invoke();
		}
	}
}
