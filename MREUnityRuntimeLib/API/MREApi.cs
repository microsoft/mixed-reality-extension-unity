// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;

using MixedRealityExtension.App;
using MixedRealityExtension.Assets;
using MixedRealityExtension.Core;
using MixedRealityExtension.Factories;
using MixedRealityExtension.PluginInterfaces.Behaviors;
using MixedRealityExtension.PluginInterfaces;
using UnityEngine;

using AppManager = MixedRealityExtension.Util.ObjectManager<MixedRealityExtension.App.IMixedRealityExtensionApp>;
using MixedRealityExtension.Util.Logging;

namespace MixedRealityExtension.API
{
	/// <summary>
	/// Static class that serves as the Mixed Reality Extension SDK API.
	/// </summary>
	public static class MREAPI
	{
		/// <summary>
		/// Initializes the Mixed Reality Extension SDK API.
		/// </summary>
		/// <param name="defaultMaterial">The material template used for all SDK-spawned meshes.</param>
		/// <param name="layerApplicator">The class used to apply MRE layers to Unity colliders.</param>
		/// <param name="assetCache">The class responsible for long-term asset caching.</param>
		/// <param name="textFactory">The text factory to use within the runtime.</param>
		/// <param name="permissionManager">The instance responsible for presenting users with permission requests.</param>
		/// <param name="behaviorFactory">The behavior factory to use within the runtime.</param>
		/// <param name="dialogFactory"></param>
		/// <param name="libraryFactory">The library resource factory to use within the runtime.</param>
		/// <param name="videoPlayerFactory"></param>
		/// <param name="primitiveFactory">The primitive factory to use within the runtime.</param>
		/// <param name="gltfImporterFactory">The glTF loader factory. Uses default GLTFSceneImporter if omitted.</param>
		/// <param name="materialPatcher">Overrides default material property map (color and mainTexture only).</param>
		/// <param name="userInfoProvider">Provides appId/sessionId scoped IUserInfo instances.</param>
		/// <param name="logger">The logger to be used by the MRE SDK.</param>
		public static void InitializeAPI(
			// required properties
			UnityEngine.Material defaultMaterial,
			ILayerApplicator layerApplicator,
			IAssetCache assetCache,
			ITextFactory textFactory,
			IPermissionManager permissionManager,
			// missing features if omitted
			IBehaviorFactory behaviorFactory = null,
			IDialogFactory dialogFactory = null,
			ILibraryResourceFactory libraryFactory = null,
			IVideoPlayerFactory videoPlayerFactory = null,
			// reasonable defaults provided
			IPrimitiveFactory primitiveFactory = null,
			IGLTFImporterFactory gltfImporterFactory = null,
			IMaterialPatcher materialPatcher = null,
			IMRELogger logger = null)
		{
			// required properties
			AppsAPI.DefaultMaterial = defaultMaterial;
			AppsAPI.LayerApplicator = layerApplicator;
			AppsAPI.AssetCache = assetCache;
			AppsAPI.TextFactory = textFactory;
			AppsAPI.PermissionManager = permissionManager;

			// missing features if omitted
			AppsAPI.BehaviorFactory = behaviorFactory;
			AppsAPI.DialogFactory = dialogFactory;
			AppsAPI.LibraryResourceFactory = libraryFactory;
			AppsAPI.VideoPlayerFactory = videoPlayerFactory;

			// reasonable defaults provided
			AppsAPI.PrimitiveFactory = primitiveFactory ?? new MWPrimitiveFactory();
			AppsAPI.GLTFImporterFactory = gltfImporterFactory ?? new GLTFImporterFactory();
			AppsAPI.MaterialPatcher = materialPatcher ?? new DefaultMaterialPatcher();

#if ANDROID_DEBUG
			Logger = logger ?? new UnityLogger(null);
#else
			Logger = logger ?? new ConsoleLogger(null);
#endif
		}

		/// <summary>
		/// Gets the apps API for the Mixed Reality Extension SDK.
		/// </summary>
		public static MREAppsAPI AppsAPI { get; } = new MREAppsAPI();

		/// <summary>
		/// Gets the logger to use within the MRE SDK.
		/// </summary>
		public static IMRELogger Logger { get; private set; }

		// TODO @tombu - Re-visit this with the upcoming user design and implementation.
		//public static MWIUsersAPI UsersAPI { get; } = new MWIUsersAPI();
	}

	/// <summary>
	/// Class that contains the mixed reality extension application API.
	/// </summary>
	public class MREAppsAPI
	{
		private AppManager _apps = new AppManager();

		/// <summary>
		/// The material template used for all SDK-spawned materials.
		/// </summary>
		public UnityEngine.Material DefaultMaterial { get; internal set; }

		/// <summary>
		/// The class responsible for assigning layers to colliders.
		/// </summary>
		public ILayerApplicator LayerApplicator { get; internal set; }

		/// <summary>
		/// The class responsible for long-term asset caching.
		/// </summary>
		public IAssetCache AssetCache { get; internal set; }

		internal IBehaviorFactory BehaviorFactory { get; set; }

		internal ITextFactory TextFactory { get; set; }

		internal IPrimitiveFactory PrimitiveFactory { get; set; }

		internal ILibraryResourceFactory LibraryResourceFactory { get; set; }

		internal IVideoPlayerFactory VideoPlayerFactory { get; set; }

		internal IGLTFImporterFactory GLTFImporterFactory { get; set; }

		internal IMaterialPatcher MaterialPatcher { get; set; }

		internal IDialogFactory DialogFactory { get; set; }

		internal IPermissionManager PermissionManager { get; set; }

		/// <summary>
		/// Creates a new mixed reality extension app and adds it to the MRE runtime.
		/// </summary>
		/// <param name="ownerScript">The owner unity script for the app.</param>
		/// <param name="ephemeralAppId">A unique identifier for the MRE behind this instance's URL, in the absence
		/// of a global app ID. Used for generating user IDs that are consistent within this session across clients,
		/// but not reliable across time. Must be synchronized across all clients in this session, and must be
		/// periodically rotated.</param>
		/// <param name="globalAppId">A unique identifier for the MRE behind this instance's URL. Used for generating
		/// consistent user IDs for this MRE. Would typically come from an app registry or similar. If supplied, must
		/// be synchronized across all clients in this session.
		/// </param>
		/// <returns>Returns the newly created mixed reality extension app.</returns>
		public IMixedRealityExtensionApp CreateMixedRealityExtensionApp(
			MonoBehaviour ownerScript,
			string ephemeralAppId,
			string globalAppId)
		{
			var mreApp = new MixedRealityExtensionApp(globalAppId ?? string.Empty, ephemeralAppId, ownerScript)
			{
				InstanceId = Guid.NewGuid()
			};

			_apps.Add(mreApp.InstanceId, mreApp);
			return mreApp;
		}

		/// <summary>
		/// Removes the app from the runtime.
		/// </summary>
		/// <param name="app">The app to remove.</param>
		public void RemoveApp(IMixedRealityExtensionApp app)
		{
			var mreApp = (MixedRealityExtensionApp)app;
			mreApp.Shutdown();
			_apps.Remove(mreApp.InstanceId);
		}

		/// <summary>
		/// Get the <see cref="IMixedRealityExtensionApp"/> app that has the given app ID.
		/// </summary>
		/// <param name="id">The ID of the app to get.</param>
		/// <returns>The app with the given ID.</returns>
		public IMixedRealityExtensionApp GetApp(Guid id)
		{
			return ((MixedRealityExtensionApp)_apps.Get(id));
		}
	}

	/* TODO @tombu - Re-vist this with the upcoming user design and implementation.
	/// <summary>
	/// Static class that contains the mixed reality extension user interop API.
	/// </summary>
	public class MWIUsersAPI
	{
		private ObjectManager<IUser> _manager;

		internal MWIUsersAPI()
		{
			_manager = new ObjectManager<IUser>();
		}

		/// <summary>
		/// Add a new mixed reality extension user.
		/// </summary>
		/// <param name="engineUser">The engine user to add.</param>
		/// <returns>The app user created for the engine user.</returns>
		internal IUser AddUser(IEngineUser engineUser)
		{
			return _manager.Create((id) => new User(id, engineUser));
		}

		/// <summary>
		/// Remove a mixed reality extension user.
		/// </summary>
		/// <param name="id">The ID of the user to remove from mixed reality extension apps.</param>
		internal void RemoveUser(Guid id)
		{
			_manager.Remove(id);
		}

		/// <summary>
		/// Returns the mixed reality extension user for the given user ID.
		/// </summary>
		/// <param name="userId">The user ID.</param>
		/// <returns>The mixed reality extension user with the supplied ID.</returns>
		public IUser GetUser(Guid userId)
		{
			return _manager.Get(userId);
		}
	}
	*/
}
