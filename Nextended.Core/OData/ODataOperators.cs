using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq.Expressions;

namespace Nextended.Core.OData;

internal class ODataOperators
{
    public static ReadOnlyDictionary<ExpressionType, string> OperatorDictionary = new(
        new Dictionary<ExpressionType, string>
        {
            { ExpressionType.GreaterThan, "gt" },
            { ExpressionType.GreaterThanOrEqual, "gte" },
            { ExpressionType.LessThan, "lt" },
            { ExpressionType.LessThanOrEqual, "lte" },
            { ExpressionType.Equal, "eq" },
            { ExpressionType.NotEqual, "ne" }
        }
    );
}