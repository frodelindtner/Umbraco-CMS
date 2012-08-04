﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Umbraco.Core.Logging;
using Umbraco.Core.Resolving;

namespace Umbraco.Core
{
	/// <summary>
	/// A bootstrapper for the Umbraco application which initializes all objects for the Core of the application 
	/// </summary>
	/// <remarks>
	/// This does not provide any startup functionality relating to web objects
	/// </remarks>
	internal class CoreBootManager : IBootManager
	{

		private DisposableTimer _timer;

		public virtual IBootManager Initialize()
		{
			LogHelper.Info<CoreBootManager>("Umbraco application starting");
			_timer = DisposableTimer.Start(x => LogHelper.Info<CoreBootManager>("Umbraco application startup complete" + " (took " + x + "ms)"));

			//create the ApplicationContext
			ApplicationContext.Current = new ApplicationContext()
			{
				IsReady = true	// fixme
			};

			InitializeResolvers();
			return this;
		}

		/// <summary>
		/// Fires after initialization and calls the callback to allow for customizations to occur
		/// </summary>
		/// <param name="afterStartup"></param>
		/// <returns></returns>
		public virtual IBootManager Startup(Action<ApplicationContext> afterStartup)
		{
			if (afterStartup != null)
			{
				afterStartup(ApplicationContext.Current);	
			}			
			return this;
		}

		/// <summary>
		/// Fires after startup and calls the callback once customizations are locked
		/// </summary>
		/// <param name="afterComplete"></param>
		/// <returns></returns>
		public virtual IBootManager Complete(Action<ApplicationContext> afterComplete)
		{
			//freeze resolution to not allow Resolvers to be modified
			Resolution.Freeze();

			//stop the timer and log the output
			_timer.Dispose();

			if (afterComplete != null)
			{
				afterComplete(ApplicationContext.Current);	
			}
			
			return this;
		}

		/// <summary>
		/// Create the resolvers
		/// </summary>
		protected virtual void InitializeResolvers()
		{
			CacheRefreshersResolver.Current = new CacheRefreshersResolver(
				PluginManager.Current.ResolveCacheRefreshers());

			DataTypesResolver.Current = new DataTypesResolver(
				PluginManager.Current.ResolveDataTypes());

			MacroFieldEditorsResolver.Current = new MacroFieldEditorsResolver(
				PluginManager.Current.ResolveMacroRenderings());

			PackageActionsResolver.Current = new PackageActionsResolver(
				PluginManager.Current.ResolvePackageActions());

			ActionsResolver.Current = new ActionsResolver(
				PluginManager.Current.ResolveActions());
		}
	}
}