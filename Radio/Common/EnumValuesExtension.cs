using System;
using System.Windows.Markup;

namespace Radio.Common;

public class EnumValuesExtension : MarkupExtension
{
    public Type EnumType { get; set; } = null!;

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        if (EnumType is null || !EnumType.IsEnum)
            return Array.Empty<object>();

        return Enum.GetValues(EnumType);
    }
}
