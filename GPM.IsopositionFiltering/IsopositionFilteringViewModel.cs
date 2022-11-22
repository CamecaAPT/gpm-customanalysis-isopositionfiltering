using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media;
using Cameca.CustomAnalysis.Interface;
using Cameca.CustomAnalysis.Utilities;
using CommunityToolkit.Mvvm.Input;

namespace GPM.CustomAnalysis.IsopositionFiltering;

internal class IsopositionFilteringViewModel : AnalysisViewModelBase<IsopositionFilteringNode>
{

	public const string UniqueId = "GPM.CustomAnalysis.IsopositionFiltering.IsopositionFilteringViewModel";

	private readonly IRenderDataFactory renderDataFactory;
	private bool optionsChanged = false;

	private readonly AsyncRelayCommand runCommand;
	public ICommand RunCommand => runCommand;

	public IsopositionFilteringOptions Options => Node!.Options;

	public ObservableCollection<object> Tabs { get; } = new();

	private object? selectedTab;
	public object? SelectedTab
	{
		get => selectedTab;
		set => SetProperty(ref selectedTab, value);
	}

	public IsopositionFilteringViewModel(
		IAnalysisViewModelBaseServices services,
		IRenderDataFactory renderDataFactory) : base(services)
	{
		this.renderDataFactory = renderDataFactory;
		runCommand = new AsyncRelayCommand(OnRun, UpdateSelectedEventCountsEnabled);
	}

	protected override void OnCreated(ViewModelCreatedEventArgs eventArgs)
	{
		base.OnCreated(eventArgs);
		if (Node is { } node)
		{
			node.Options.PropertyChanged += OptionsOnPropertyChanged;
		}
	}
	
	private async Task OnRun()
	{
		foreach (var item in Tabs)
		{
			if (item is IDisposable disposable)
				disposable.Dispose();
		}
		Tabs.Clear();

		var data = await Node!.Run();
		if (data is null) return;

		var experimentalLineRenderData = renderDataFactory.CreateLine(
			data.Experimental,
			Colors.Red,
			name: "Experimental");
		var randomizedLineRenderData = renderDataFactory.CreateLine(
			data.Experimental,
			Colors.Blue,
			name: "Randomized");
		var chart2DContentViewModel = new Chart2DContentViewModel(
			"Frequency distribution",
			new IRenderData[]
			{
				experimentalLineRenderData,
				randomizedLineRenderData
			});
		Tabs.Add(chart2DContentViewModel);
		SelectedTab = chart2DContentViewModel;
	}

	private void OptionsOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
	{
		optionsChanged = true;
		runCommand.NotifyCanExecuteChanged();
	}


	private bool UpdateSelectedEventCountsEnabled() => !Tabs.Any() || optionsChanged;
}
