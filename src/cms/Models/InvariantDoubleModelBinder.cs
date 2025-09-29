using System.Globalization;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace cms.Models;

public class InvariantDoubleModelBinder : IModelBinder
{
    public Task BindModelAsync(ModelBindingContext ctx)
    {
        var val = ctx.ValueProvider.GetValue(ctx.ModelName).FirstValue;
        if (double.TryParse(val, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var d))
            ctx.Result = ModelBindingResult.Success(d);
        else
            ctx.ModelState.AddModelError(ctx.ModelName, "Invalid number.");
        return Task.CompletedTask;
    }
}