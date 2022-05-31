using System.Numerics;

namespace GPM.CustomAnalysis.IsopositionFiltering;

internal class FrequencyDistributionComparisonData
{
	public Vector3[] Experimental { get; }
	public Vector3[] Randomized { get; }

	public FrequencyDistributionComparisonData(Vector3[] experimental, Vector3[] randomized)
	{
		Experimental = experimental;
		Randomized = randomized;
	}
}
