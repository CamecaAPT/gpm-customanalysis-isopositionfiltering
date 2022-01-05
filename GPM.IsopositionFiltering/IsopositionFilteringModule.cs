using System;
using Cameca.CustomAnalysis.Interface.CustomAnalysis;
using Prism.Ioc;
using Prism.Modularity;

namespace GPM.CustomAnalysis.IsopositionFiltering
{
    [ModuleDependency("IvasModule")]
    public class IsopositionFilteringModule : IModule
    {
        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            // Register any additional dependencies with the Unity IoC container
        }

        public void OnInitialized(IContainerProvider containerProvider)
        {
            var customAnalysisService = containerProvider.Resolve<ICustomAnalysisService>();

            customAnalysisService.Register<IsopositionFilteringCustomAnalysis, IsopositionFilteringOptions>(
                new CustomAnalysisDescription("GPM_IsopostionFiltering", "GPM Isoposition Filtering", new Version()));
        }
    }
}
