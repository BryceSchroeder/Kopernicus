/**
 * Kopernicus Planetary System Modifier
 * ====================================
 * Created by: BryceSchroeder and Teknoman117 (aka. Nathaniel R. Lewis)
 * Maintained by: Thomas P., NathanKell and KillAshley
 * Additional Content by: Gravitasi, aftokino, KCreator, Padishar, Kragrathea, OvenProofMars, zengei, MrHappyFace
 * ------------------------------------------------------------- 
 * This library is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public
 * License as published by the Free Software Foundation; either
 * version 3 of the License, or (at your option) any later version.
 *
 * This library is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public
 * License along with this library; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston,
 * MA 02110-1301  USA
 * 
 * This library is intended to be used as a plugin for Kerbal Space Program
 * which is copyright 2011-2015 Squad. Your usage of Kerbal Space Program
 * itself is governed by the terms of its EULA, not the license above.
 * 
 * https://kerbalspaceprogram.com
 */

using System;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Kopernicus
{
    [KSPAddon(KSPAddon.Startup.MainMenu, true)]
    public class ClockFixer : MonoBehaviour
    {
        public void Start()
        {
            if (Templates.customClock)
            {
                // Find the home planet
                CelestialBody homePlanet = FlightGlobals.Bodies.First(b => b.isHomeWorld);

                // Get custom year and day duration
                if (Templates.useKopernicusTime)
                {
                    ClockFormatter.Y.value = homePlanet.orbitDriver.orbit.period;
                    ClockFormatter.D.value = homePlanet.solarDayLength;

                    // If tidally locked set day = year
                    if (ClockFormatter.Y.value == homePlanet.rotationPeriod)
                        ClockFormatter.D.value = ClockFormatter.Y.value;
                }

                // Convert negative numbers to positive
                if (ClockFormatter.Y.value < 0)
                    ClockFormatter.Y.value = -ClockFormatter.Y.value;
                if (ClockFormatter.D.value < 0)
                    ClockFormatter.D.value = -ClockFormatter.D.value;

                // If weird number revert to stock values
                if (double.IsInfinity(ClockFormatter.D.value) || double.IsNaN(ClockFormatter.D.value) || double.IsInfinity(ClockFormatter.Y.value) || double.IsNaN(ClockFormatter.Y.value))
                {
                    ClockFormatter.D.value = 3600 * (GameSettings.KERBIN_TIME ? 6 : 24);
                    ClockFormatter.Y.value = 3600 * (GameSettings.KERBIN_TIME ? 6 * 426 : 24 * 365);
                }

                // Replace the stock Formatter
                KSPUtil.dateTimeFormatter = new ClockFormatter();
            }
        }
    }

    public class ClockFormatter : IDateTimeFormatter
    {
        public static KSPUtil.DefaultDateTimeFormatter DTF = new KSPUtil.DefaultDateTimeFormatter();
                
        public static class S
        {
            public static string singular = "Second";
            public static string plural = "Seconds";
            public static string symbol = "s";
            public static double value = 1;
        }

        public static class M
        {
            public static string singular = "Min";
            public static string plural = "Mins";
            public static string symbol = "m";
            public static double value = 60;
        }

        public static class H
        {
            public static string singular = "Hour";
            public static string plural = "Hours";
            public static string symbol = "h";
            public static double value = 3600;
        }

        public static class D
        {
            public static string singular = "Day";
            public static string plural = "Days";
            public static string symbol = "d";
            public static double value = 3600 * (GameSettings.KERBIN_TIME ? 6 : 24);
        }

        public static class Y
        {
            public static string singular = "Year";
            public static string plural = "Years";
            public static string symbol = "y";
            public static double value = 3600 * (GameSettings.KERBIN_TIME ? 6 * 426 : 24 * 365);
        }


        public static int[] num = new int[6];
         
        public string PrintTimeLong(double time)
        {
            string text = CheckNum(time);
            if (text != null)
                return text;

            GetTime(time);
            StringBuilder sb = StringBuilderCache.Acquire();
            sb.Append(num[1]).Append(num[1] == 1 ? Y.singular : Y.plural).Append(", ");
            sb.Append(num[5]).Append(num[5] == 1 ? D.singular : D.plural).Append(", ");
            sb.Append(num[4]).Append(num[4] == 1 ? H.singular : H.plural).Append(", ");
            sb.Append(num[3]).Append(num[3] == 1 ? M.singular : M.plural).Append(", ");
            sb.Append(num[2]).Append(num[2] == 1 ? S.singular : S.plural);
            return sb.ToStringAndRelease();
        }

        public string PrintTimeStamp(double time, bool days = false, bool years = false)
        {
            string text = CheckNum(time);
            if (text != null)
                return text;

            GetTime(time);
            StringBuilder stringBuilder = StringBuilderCache.Acquire();

            if (years)
                stringBuilder.Append(Y.singular + " ").Append(num[1]).Append(", ");
            if (days)
                stringBuilder.Append("Day ").Append(num[5]).Append(" - ");

            stringBuilder.AppendFormat("{0:00}:{1:00}", num[4], num[3]);

            if (num[1] < 10)
                stringBuilder.AppendFormat(":{0:00}", num[2]);

            return stringBuilder.ToStringAndRelease();
        }

        public string PrintTimeStampCompact(double time, bool days = false, bool years = false)
        {
            string text = CheckNum(time);
            if (text != null)
                return text;

            GetTime(time);
            StringBuilder stringBuilder = StringBuilderCache.Acquire();
            if (years)
                stringBuilder.Append(num[1]).Append(Y.symbol + ", ");

            if (days)
                stringBuilder.Append(num[5]).Append(D.symbol + ", ");

            stringBuilder.AppendFormat("{0:00}:{1:00}", num[4], num[3]);
            if (num[1] < 10)
                stringBuilder.AppendFormat(":{0:00}", num[2]);

            return stringBuilder.ToStringAndRelease();
        }

        public string PrintTime(double time, int valuesOfInterest, bool explicitPositive)
        {
            string text = CheckNum(time);
            if (text != null)
                return text;

            bool flag = time < 0.0;
            GetTime(time);
            string[] array = { S.symbol, M.symbol, H.symbol, D.symbol, Y.symbol };
            StringBuilder stringBuilder = StringBuilderCache.Acquire();
            if (flag)
                stringBuilder.Append("- ");
            else if (explicitPositive)
                stringBuilder.Append("+ ");

            int[] list = { num[2], num[3], num[4], num[5], num[1] };
            int num0 = list.Length;
            while (num0-- > 0)
            {
                if (list[num0] != 0)
                {
                    for (int i = num0; i > Mathf.Max(num0 - valuesOfInterest, -1); i--)
                    {
                        stringBuilder.Append(Math.Abs(list[i])).Append(array[i]);
                        if (i - 1 > Mathf.Max(num0 - valuesOfInterest, -1))
                            stringBuilder.Append(", ");
                    }
                    break;
                }
            }
            return stringBuilder.ToStringAndRelease();
        }

        public string PrintTimeCompact(double time, bool explicitPositive)
        {
            string text = CheckNum(time);
            if (text != null)
                return text;

            bool flag = time < 0.0;
            GetTime(time);
            StringBuilder stringBuilder = StringBuilderCache.Acquire();
            if (flag)
                stringBuilder.Append("T- ");
            else if (explicitPositive)
                stringBuilder.Append("T+ ");

            if (num[5] > 0)
                stringBuilder.Append(Math.Abs(num[5])).Append(":");

            stringBuilder.AppendFormat("{0:00}:{1:00}:{2:00}", num[4], num[3], num[2]);
            return stringBuilder.ToStringAndRelease();
        }
        public string PrintDateDelta(double time, bool includeTime, bool includeSeconds, bool useAbs)
        {
            string text = CheckNum(time);
            if (text != null)
                return text;

            if (useAbs && time < 0.0)
                time = -time;

            StringBuilder stringBuilder = StringBuilderCache.Acquire();
            GetTime(time);

            if (num[1] > 1)
                stringBuilder.Append(num[1]).Append(" " + Y.plural);
            else if (num[1] == 1)
                stringBuilder.Append(num[1]).Append(" " + Y.singular);

            if (num[5] > 1)
            {
                if (stringBuilder.Length != 0)
                    stringBuilder.Append(", ");
                stringBuilder.Append(num[5]).Append(" " + D.plural);
            }
            else if (num[5] == 1)
            {
                if (stringBuilder.Length != 0)
                    stringBuilder.Append(", ");
                stringBuilder.Append(num[5]).Append(" " + D.singular);
            }
            if (includeTime)
            {
                if (num[4] > 1)
                {
                    if (stringBuilder.Length != 0)
                        stringBuilder.Append(", ");
                    stringBuilder.Append(num[4]).Append(" " + H.plural);
                }
                else if (num[4] == 1)
                {
                    if (stringBuilder.Length != 0)
                        stringBuilder.Append(", ");
                    stringBuilder.Append(num[4]).Append(" " + H.singular);
                }
                if (num[3] > 1)
                {
                    if (stringBuilder.Length != 0)
                        stringBuilder.Append(", ");
                    stringBuilder.Append(num[3]).Append(" " + M.plural);
                }
                else if (num[3] == 1)
                {
                    if (stringBuilder.Length != 0)
                        stringBuilder.Append(", ");
                    stringBuilder.Append(num[3]).Append(" " + M.singular);
                }
                if (includeSeconds)
                {
                    if (num[2] > 1)
                    {
                        if (stringBuilder.Length != 0)
                            stringBuilder.Append(", ");
                        stringBuilder.Append(num[2]).Append(" " + S.plural);
                    }
                    else if (num[2] == 1)
                    {
                        if (stringBuilder.Length != 0)
                            stringBuilder.Append(", ");
                        stringBuilder.Append(num[2]).Append(" " + S.singular);
                    }
                }
            }
            if (stringBuilder.Length == 0)
                stringBuilder.Append((!includeTime) ? "0 " + D.plural : ((!includeSeconds) ? "0 " + M.plural : "0 " + S.plural));

            return stringBuilder.ToStringAndRelease();
        }

        public string PrintDateDeltaCompact(double time, bool includeTime, bool includeSeconds, bool useAbs)
        {
            string text = CheckNum(time);
            if (text != null)
                return text;

            if (useAbs && time < 0.0)
                time = -time;

            StringBuilder stringBuilder = StringBuilderCache.Acquire();
            GetTime(time);
            if (num[1] > 0)
                stringBuilder.Append(num[1]).Append(Y.symbol);

            if (num[5] > 0)
            {
                if (stringBuilder.Length != 0)
                    stringBuilder.Append(", ");
                stringBuilder.Append(num[5]).Append(D.symbol);
            }
            if (includeTime)
            {
                if (num[4] > 0)
                {
                    if (stringBuilder.Length != 0)
                        stringBuilder.Append(", ");
                    stringBuilder.Append(num[4]).Append(H.symbol);
                }
                if (num[3] > 0)
                {
                    if (stringBuilder.Length != 0)
                        stringBuilder.Append(", ");
                    stringBuilder.Append(num[3]).Append(M.symbol);
                }
                if (includeSeconds && num[2] > 0)
                {
                    if (stringBuilder.Length != 0)
                        stringBuilder.Append(", ");
                    stringBuilder.Append(num[2]).Append(S.symbol);
                }
            }
            if (stringBuilder.Length == 0)
                stringBuilder.Append((!includeTime) ? "0" + D.symbol : ((!includeSeconds) ? "0" + M.symbol : "0" + S.symbol));
            return stringBuilder.ToStringAndRelease();
        }

        public string PrintDate(double time, bool includeTime, bool includeSeconds = false)
        {
            string text = CheckNum(time);

            if (text != null)
                return text;


            StringBuilder stringBuilder = StringBuilderCache.Acquire();
            GetDate(time);

            stringBuilder.Append(Y.singular + " ").Append(num[1] + 1).Append(", " + D.singular + " ").Append(num[5] + 1);
            if (includeTime)
                stringBuilder.Append(" - ").Append(num[4]).Append(H.symbol + ", ").Append(num[3]).Append(M.symbol);
            if (includeSeconds)
                stringBuilder.Append(", ").Append(num[2]).Append(S.symbol);
            return stringBuilder.ToStringAndRelease();
        }
        public string PrintDateNew(double time, bool includeTime)
        {
            string text = CheckNum(time);
            if (text != null)
                return text;

            StringBuilder stringBuilder = StringBuilderCache.Acquire();
            GetDate(time);
            stringBuilder.Append(Y.singular + " ").Append(num[1] + 1).Append(", " + D.singular + " ").Append(num[5] + 1);
            if (includeTime)
                stringBuilder.AppendFormat(" - {0:D2}:{1:D2}:{2:D2}", num[4], num[3], num[2]);
            return stringBuilder.ToStringAndRelease();
        }

        public string PrintDateCompact(double time, bool includeTime, bool includeSeconds = false)
        {
            string text = CheckNum(time);
            if (text != null)
                return text;

            StringBuilder stringBuilder = StringBuilderCache.Acquire();
            GetDate(time);
            stringBuilder.AppendFormat(Y.symbol + "{0}, " + D.symbol + "{1:00}", num[1] + 1, num[5] + 1);
            if (includeTime)
                stringBuilder.AppendFormat(", {0}:{1:00}", num[4], num[3]);
            if (includeSeconds)
                stringBuilder.AppendFormat(":{0:00}", num[2]);
            return stringBuilder.ToStringAndRelease();
        }

        private static string CheckNum(double time)
        {
            if (double.IsNaN(time))
                return "NaN";

            if (double.IsPositiveInfinity(time))
                return "+Inf";

            if (double.IsNegativeInfinity(time))
                return "-Inf";

            return null;
        }

        public void GetDate(double time)
        {
            // This will work also when a year cannot be divided in days without a remainder
            // If the year ends halfway through a day, the clock will go:
            // Year 1 Day 365   ==>   Year 2 Day 0    (Instead of starting directly with Day 1)
            // Day 0 will last untill Day 365 would have ended, then Day 1 will start.
            // This way the time shown by the clock will always be consistent with the position of the sun in the sky

            // Number of IRL seconds in this day
            int num0 = (int)(time % D.value);
            // Number of years in this time
            int num1 = (int)(time / Y.value);
            // Number of seconds in this minute
            int num2 = (int)((num0 % M.value) / S.value);
            // Number of minutes in this hour
            int num3 = (int)((num0 / M.value) % (H.value / M.value));
            // Number of hours in this day
            int num4 = (int)(num0 / H.value);
            // Number of days in this year
            int num5 = (int)(time / D.value) - (int)(Math.Round(Y.value / D.value, 0, MidpointRounding.AwayFromZero) * num1);
            
            num = new int[] { num0, num1, num2, num3, num4, num5 };
        }

        public void GetTime(double time)
        {
            // This will count the number of Years, Days, Hours, Minutes and Seconds
            // If a Year lasts 10.5 days, and time = 14 days, the result will be: 
            // 1 Year, 3 days, and whatever hours-minutes-seconds fit in 0.5 days.
            // ( 10.5 + 3 + 0.5 = 14 )

            // Number of years in this time
            int num1 = (int)(time / Y.value);
            // Number of seconds in this year
            int num2 = (int)(time - (num1 * Y.value));
            // Number of minutes in this hour
            int num3 = (int)((num2 / M.value) % (H.value / M.value));
            // Number of hours in this day
            int num4 = (int)((num2 / H.value) % (D.value / H.value));
            // Number of days in this year
            int num5 = (int)(num2 / D.value);
            // Number of seconds in this minute
            num2 = (int)((num2 % M.value) / S.value);

            num = new[] { 0, num1, num2, num3, num4, num5 };
        }

        public int Minute
        {
            get { return 60; }
        }
        public int Hour
        {
            get { return Minute * 60; }
        }
        public int Day
        {
            get {  return Hour * 6; }
        }
        public int Year
        {
            get { return Day * 426; }
        }
    }
}
