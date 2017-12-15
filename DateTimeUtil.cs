using System;
using System.Collections.Generic;
using System.Linq;

namespace SDP.Tvp.Query.Extensions
{
    public static class DateTimeUtil
    {
        #region TimeZone

        public static DateTime ConvertTimeOffSetFromUtc(string dateValue, string timeZoneTo)
        {
            string timeZoneName = Convert.ToString(timeZoneTo).Trim();
            TimeZoneInfo timeZoneFound = String.IsNullOrEmpty(timeZoneName) ? TimeZoneInfo.Local : (TimeZoneInfo.FindSystemTimeZoneById(timeZoneName) ?? TimeZoneInfo.Local);

            DateTime dateTimeSent = DateTime.Parse(dateValue.Trim());
            return ConvertTimeOffSetFromUtc(dateTimeSent, timeZoneFound);
        }

        public static DateTime ConvertTimeOffSetFromUtc(DateTime dateValue, string timeZoneTo)
        {
            string timeZoneName = Convert.ToString(timeZoneTo).Trim();
            TimeZoneInfo timeZoneFound = String.IsNullOrEmpty(timeZoneName) ? TimeZoneInfo.Local : (TimeZoneInfo.FindSystemTimeZoneById(timeZoneName) ?? TimeZoneInfo.Local);

            return ConvertTimeOffSetFromUtc(dateValue, timeZoneFound);
        }

        public static DateTime ConvertTimeOffSetFromUtc(DateTime dateValue, TimeZoneInfo timeZoneTo)
        {
            var accessFromOff = new DateTimeOffset(dateValue, TimeSpan.Zero);
            return TimeZoneInfo.ConvertTime(accessFromOff.UtcDateTime, TimeZoneInfo.Utc, timeZoneTo);
        }

        public static IEnumerable<dynamic> ApplyTimeZoneToModel(IEnumerable<dynamic> model)
        {
            var applyTimeZoneToModel = model as IList<dynamic> ?? model.ToList();
            foreach (var item in applyTimeZoneToModel)
            {
                bool isTemporal1 = item.isTemporal != null && item.isTemporal;
                bool isTemporal2 = item.IsTemporal != null && item.IsTemporal;
                bool isTemporal3 = item.istemporal != null && item.istemporal;
                bool hasLocationTimeZone = item.locationTimeZone != null;

                if ((isTemporal1 || isTemporal2 || isTemporal3) && hasLocationTimeZone)
                {
                    string timeZoneName = Convert.ToString(item.locationTimeZone).Trim();
                    var itemTimeZone = String.IsNullOrEmpty(timeZoneName) ? TimeZoneInfo.Local : (TimeZoneInfo.FindSystemTimeZoneById(timeZoneName) ?? TimeZoneInfo.Local);

                    // START DATE
                    if (item.StartDate != null && !(item.StartDate is DBNull))
                    {
                        var startDateOff = new DateTimeOffset(item.StartDate, TimeSpan.Zero);
                        item.StartDate = (Object)TimeZoneInfo.ConvertTime(startDateOff.UtcDateTime, TimeZoneInfo.Utc, itemTimeZone);
                    }
                    else if (item.accessFrom != null && !(item.accessFrom is DBNull))
                    {
                        var accessFromOff = new DateTimeOffset(item.accessFrom, TimeSpan.Zero);
                        item.accessFrom = (Object)TimeZoneInfo.ConvertTime(accessFromOff.UtcDateTime, TimeZoneInfo.Utc, itemTimeZone);
                    }
                    // END DATE
                    if (item.EndDate != null && !(item.EndDate is DBNull))
                    {
                        var endDateOff = new DateTimeOffset(item.EndDate, TimeSpan.Zero);
                        item.EndDate = (Object)TimeZoneInfo.ConvertTime(endDateOff.UtcDateTime, TimeZoneInfo.Utc, itemTimeZone);
                    }
                    else if (item.accessTo != null && !(item.accessTo is DBNull))
                    {
                        var accessToOff = new DateTimeOffset(item.accessTo, TimeSpan.Zero);
                        item.accessTo = (Object)TimeZoneInfo.ConvertTime(accessToOff.UtcDateTime, TimeZoneInfo.Utc, itemTimeZone);
                    }
                }
            }
            return applyTimeZoneToModel;
        }

        #endregion
    }
}
