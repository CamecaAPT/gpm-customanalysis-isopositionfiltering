using Cameca.CustomAnalysis.Interface;
using Prism.Commands;
using Prism.Events;

namespace GPM.CustomAnalysis.IsopositionFiltering;

internal class IsopositionFilteringNodeMenuFactory : IAnalysisMenuFactory
{

	public const string UniqueId = "GPM.CustomAnalysis.IsopositionFiltering.IsopositionFilteringNodeMenuFactory";

	private readonly IEventAggregator _eventAggregator;

	public IsopositionFilteringNodeMenuFactory(IEventAggregator eventAggregator)
	{
		_eventAggregator = eventAggregator;
	}

	public IMenuItem CreateMenuItem(IAnalysisMenuContext context) => new MenuAction(
		IsopositionFilteringNode.DisplayInfo.Title,
		new DelegateCommand(() => _eventAggregator.PublishCreateNode(
			IsopositionFilteringNode.UniqueId,
			context.NodeId,
			IsopositionFilteringNode.DisplayInfo.Title,
			IsopositionFilteringNode.DisplayInfo.Icon)),
		IsopositionFilteringNode.DisplayInfo.Icon);

	public AnalysisMenuLocation Location { get; } = AnalysisMenuLocation.Analysis;
}
