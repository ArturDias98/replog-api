using System.Text.Json;
using FluentValidation;
using replog_shared.Json;

namespace replog_shared.Helpers;

public static class ChangeDataHelper
{
    public static T DeserializeAndValidate<T>(JsonElement? data, IValidator<T> validator)
    {
        if (!data.HasValue)
            throw new ValidationException("Data is required.");

        var result = data.Value.Deserialize<T>(JsonDefaults.Options)
                     ?? throw new ValidationException("Failed to deserialize change data.");

        validator.ValidateAndThrow(result);

        return result;
    }
}
