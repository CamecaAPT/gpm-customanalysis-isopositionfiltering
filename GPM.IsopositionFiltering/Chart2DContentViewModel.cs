using System;
using System.Collections.Generic;
using System.Linq;
using Cameca.CustomAnalysis.Interface;

namespace GPM.CustomAnalysis.IsopositionFiltering;

internal class Chart2DContentViewModel : IDisposable
{
	public string Title { get; }

	public ICollection<IRenderData> RenderData { get; }

	public Chart2DContentViewModel(string title, IEnumerable<IRenderData> content)
	{
		Title = title;
		RenderData = content.ToList();
	}

	public void Dispose()
	{
		foreach (var item in RenderData)
		{
			if (item is IDisposable disposable)
				disposable.Dispose();
		}
	}
}
