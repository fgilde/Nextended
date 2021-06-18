using System;
using System.Collections.Generic;
using System.Linq;

namespace Nextended.Core.Extensions
{
    public static class ExceptionExtensions
    {
        public static Exception FindBaseException(this Exception ex)
        {
            var innerEx = ex;
            while (ex.InnerException != null)
                innerEx = ex.InnerException;
            return innerEx;
        }

        public static string ExtractMessage(this AggregateException aggregateException)
        {
            return string.Join(Environment.NewLine + "- ", aggregateException.ExtractMessages());
        }
        
        public static IEnumerable<string> ExtractMessages(this AggregateException aggregateException)
        {
            return aggregateException.Unwrap().Select(exception => exception.Message);
        }

        public static string ExtractFriendlyMessage(this Exception ex)
        {
            return ex == null ? null : ExtractMessage(ex.InnerException);
        }

        public static string ExtractMessage(this Exception ex)
        {
            return ex == null ? null : ex.Message + Environment.NewLine +
                                       ExtractMessage(ex.InnerException);
        }

        public static string ExtractDescription(this Exception ex)
        {
            return ex == null ? null :
                ("(" + ex.GetType() + ") " +
                 ex.Message + Environment.NewLine +
                 ex.StackTrace + Environment.NewLine + Environment.NewLine +
                 ExtractDescription(ex.InnerException));
        }

        public static IEnumerable<Exception> Unwrap(this AggregateException aggregateException)
        {
            var result = new List<Exception>();
            foreach (var exception in aggregateException.InnerExceptions)
            {
                if (exception is AggregateException aggregated)
                    result.AddRange(aggregated.Unwrap());
                else
                    result.Add(exception);
            }
            return result;
        }
	}
}