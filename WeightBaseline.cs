using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Spt.Tables;

namespace FeatherLight;

[Injectable(InjectionType.Singleton)]
public sealed class WeightBaseline
{
    public Dictionary<MongoId, double> Weights { get; } = [];
}

[Injectable(TypePriority = OnLoadOrder.Preload)]
public sealed class WeightBaselineCapture(TemplateTable templateTable, WeightBaseline baseline) : IOnLoad
{
    public Task OnLoadAsync(CancellationToken cancellationToken)
    {
        baseline.Weights.Clear();
        foreach (var item in templateTable.Items.Values)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (item.Properties?.Weight is { } weight && double.IsFinite(weight) && weight >= 0)
            {
                baseline.Weights[item.Id] = weight;
            }
        }

        return Task.CompletedTask;
    }
}
