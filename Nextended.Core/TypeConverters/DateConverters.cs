using System;
using YamlDotNet.Core.Tokens;

namespace Nextended.Core.TypeConverters;

//DateTime
public class DateTimeToDoubleConverter(bool allowAssignableInputs) : GenericTypeConverter<DateTime, double>(time => time.Ticks, allowAssignableInputs);

public class DoubleToDateTimeConverter(bool allowAssignableInputs) : GenericTypeConverter<double, DateTime>(ticks => new DateTime(Convert.ToInt64(ticks)), allowAssignableInputs);


// TimeSpan
public class TimeSpanToDoubleConverter(bool allowAssignableInputs) : GenericTypeConverter<TimeSpan, double>(time => time.Ticks, allowAssignableInputs);

public class DoubleToTimeSpanConverter(bool allowAssignableInputs) : GenericTypeConverter<double, TimeSpan>(ticks => TimeSpan.FromTicks(Convert.ToInt64(ticks)), allowAssignableInputs);


//DateOnly
#if !NETSTANDARD
public class DateOnlyToDoubleConverter(bool allowAssignableInputs) : GenericTypeConverter<DateOnly, double>(date => date.DayNumber, allowAssignableInputs);
public class DoubleToDateOnlyConverter(bool allowAssignableInputs) : GenericTypeConverter<double, DateOnly>(dayNumber => DateOnly.FromDayNumber(Convert.ToInt32(dayNumber)), allowAssignableInputs);
#endif

//TimeOnly

#if !NETSTANDARD
public class TimeOnlyToDoubleConverter(bool allowAssignableInputs) : GenericTypeConverter<TimeOnly, double>(time => time.Ticks, allowAssignableInputs);
public class DoubleToTimeOnlyConverter(bool allowAssignableInputs) : GenericTypeConverter<double, TimeOnly>(ticks => new TimeOnly(Convert.ToInt64(ticks)), allowAssignableInputs);
#endif
