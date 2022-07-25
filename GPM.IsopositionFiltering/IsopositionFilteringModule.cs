using System;
using Cameca.CustomAnalysis.Interface;
using Cameca.CustomAnalysis.Utilities;
using Prism.Ioc;
using Prism.Modularity;

namespace GPM.CustomAnalysis.IsopositionFiltering;

public class IsopositionFilteringModule : IModule
{
    public void RegisterTypes(IContainerRegistry containerRegistry)
    {
        containerRegistry.AddCustomAnalysisUtilities();

        containerRegistry.Register<object, IsopositionFilteringNode>(IsopositionFilteringNode.UniqueId);
        containerRegistry.RegisterInstance<INodeDisplayInfo>(IsopositionFilteringNode.DisplayInfo, IsopositionFilteringNode.UniqueId);
        containerRegistry.Register<IAnalysisMenuFactory, IsopositionFilteringNodeMenuFactory>(IsopositionFilteringNodeMenuFactory.UniqueId);
        containerRegistry.Register<object, IsopositionFilteringViewModel>(IsopositionFilteringViewModel.UniqueId);
    }

    public void OnInitialized(IContainerProvider containerProvider)
    {
        var extensionRegistry = containerProvider.Resolve<IExtensionRegistry>();

        extensionRegistry.RegisterAnalysisView<IsopositionFilteringView, IsopositionFilteringViewModel>(AnalysisViewLocation.Top);
    }
}