using Prism.Mvvm;

namespace GPM.CustomAnalysis.IsopositionFiltering;

public class IsopositionFilteringOptions : BindableBase
{
	private string name_Int = "AM";
	private double grid_size_init = 1.0;
	private double grid_delo = 0.5;
	private double x_threshold = 1.0;  // Concentration threshold : Min
	private double x_step = 0.1;  // Concentration distribution visualization : bin size
	private int disp_step = 20;  // Number of Iteration for the progress "bar"


	public string Name_Int
	{
		get => name_Int;
		set => SetProperty(ref name_Int, value);
	}

	public double Grid_size_init
	{
		get => grid_size_init;
		set => SetProperty(ref grid_size_init, value);
	}

	public double Grid_delo
	{
		get => grid_delo;
		set => SetProperty(ref grid_delo, value);
	}

	public double X_threshold
	{
		get => x_threshold;
		set => SetProperty(ref x_threshold, value);
	}

	public double X_step
	{
		get => x_step;
		set => SetProperty(ref x_step, value);
	}

	public int Disp_step
	{
		get => disp_step;
		set => SetProperty(ref disp_step, value);
	}
}
