﻿using System.ComponentModel;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Xml.Serialization;
using Cameca.CustomAnalysis.Interface;
using Cameca.CustomAnalysis.Utilities;

namespace GPM.CustomAnalysis.IsopositionFiltering;

[DefaultView(IsopositionFilteringViewModel.UniqueId, typeof(IsopositionFilteringViewModel))]
internal class IsopositionFilteringNode : AnalysisNodeBase
{
	public class NodeDisplayInfo : INodeDisplayInfo
	{
		public string Title { get; } = "GPM Isoposition Filtering";
		public ImageSource? Icon { get; } = null;
	}

	public static NodeDisplayInfo DisplayInfo { get; } = new();

	public const string UniqueId = "GPM.CustomAnalysis.IsopositionFiltering.IsopositionFilteringNode";

	private readonly IsopositionFiltering isopositionFiltering;

	public IsopositionFilteringOptions Options { get; private set; } = new();

	public IsopositionFilteringNode(IAnalysisNodeBaseServices services) : base(services)
	{
		isopositionFiltering = new IsopositionFiltering();
	}

	public async Task<FrequencyDistributionComparisonData?> Run()
	{
		if (await Services.IonDataProvider.GetIonData(InstanceId) is not { } ionData)
			return null;

		return isopositionFiltering.Run(ionData, Options);
	}

	protected override byte[]? GetSaveContent()
	{
		var serializer = new XmlSerializer(typeof(IsopositionFilteringOptions));
		using var stringWriter = new StringWriter();
		serializer.Serialize(stringWriter, Options);
		return Encoding.UTF8.GetBytes(stringWriter.ToString());
	}

	protected override void OnCreated(NodeCreatedEventArgs eventArgs)
	{
        base.OnCreated(eventArgs);
        Options.PropertyChanged += OptionsOnPropertyChanged;

		if (eventArgs.Trigger == EventTrigger.Load && eventArgs.Data is { } data)
        {
            var xmlData = Encoding.UTF8.GetString(data);
            var serializer = new XmlSerializer(typeof(IsopositionFilteringOptions));
            using var stringReader = new StringReader(xmlData);
            if (serializer.Deserialize(stringReader) is IsopositionFilteringOptions loadedOptions)
            {
                Options = loadedOptions;
            }
        }
	}

	private void OptionsOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
	{
		if (CanSaveState is { } canSaveState)
		{
			canSaveState.CanSave = true;
		}
	}
}
