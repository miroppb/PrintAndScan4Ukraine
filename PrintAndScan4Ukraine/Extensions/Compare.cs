using System.Collections.Generic;
using System.Reflection;

public static class Extensions
{
	public static List<Variance> Compare<T>(this T val1, T val2)
	{
		var variances = new List<Variance>();
		var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);
		foreach (var property in properties)
		{
			var v = new Variance
			{
				Prop = property.Name,
				ValA = property.GetValue(val1),
				ValB = property.GetValue(val2)
			};
			if (v.ValA == null && v.ValB == null)
			{
				continue;
			}
			if (
				(v.ValA == null && v.ValB != null)
				||
				(v.ValA != null && v.ValB == null)
			)
			{
				variances.Add(v);
				continue;
			}
			if (!v.ValA!.Equals(v.ValB))
			{
				variances.Add(v);
			}
		}
		return variances;
	}
}
public class Variance
{
	public string? Prop { get; set; }
	public object? ValA { get; set; }
	public object? ValB { get; set; }
}
