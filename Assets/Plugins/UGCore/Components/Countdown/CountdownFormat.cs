using System;
using System.Text;
using UGFoundation;

namespace UGCore.Components
{
    internal static class CountdownFormat
    {
        internal static string Format(string format, int remainSeconds)
        {
            StringBuilder result = StringBuilderCache.Acquire();

            int i = 0;
            int tokenLen = 0;
            while (i < format.Length)
            {
                char ch = format[i];
                switch (ch)
                {
                    case '{':
                        int repeatCharNum = 0;
                        if ((repeatCharNum = ParseRepeatPattern(format, i, 's')) > 0)
                        {
                            int seconds = remainSeconds;
                            if (ParseNextChar(format, i + repeatCharNum) == '}')
                            {
                                seconds = remainSeconds % Countdown.SecondsMinute;
                                result.Append(seconds.ToString().PadLeft(repeatCharNum, '0'));
                                tokenLen = repeatCharNum + 2;
                            }
                            else if (ParseNextChar(format, i + repeatCharNum) == '+' &&
                                     ParseNextChar(format, i + repeatCharNum + 1) == '}')
                            {
                                result.Append(seconds.ToString().PadLeft(repeatCharNum, '0'));
                                tokenLen = repeatCharNum + 3;
                            }
                            else
                            {
                                result.Append(ch);
                                tokenLen = 1;
                            }
                        }
                        else if ((repeatCharNum = ParseRepeatPattern(format, i, 'm')) > 0)
                        {
                            int minutes = 0;
                            if (ParseNextChar(format, i + repeatCharNum) == '}')
                            {
                                minutes = (remainSeconds % Countdown.SecondsHour) / Countdown.SecondsMinute;
                                result.Append(minutes.ToString().PadLeft(repeatCharNum, '0'));
                                tokenLen = repeatCharNum + 2;
                            }
                            else if (ParseNextChar(format, i + repeatCharNum) == '+' &&
                                     ParseNextChar(format, i + repeatCharNum + 1) == '}')
                            {
                                minutes = remainSeconds / Countdown.SecondsMinute;
                                result.Append(minutes.ToString().PadLeft(repeatCharNum, '0'));
                                tokenLen = repeatCharNum + 3;
                            }
                            else
                            {
                                result.Append(ch);
                                tokenLen = 1;
                            }
                        }
                        else if ((repeatCharNum = ParseRepeatPattern(format, i, 'h')) > 0)
                        {
                            int hours = 0;
                            if (ParseNextChar(format, i + repeatCharNum) == '}')
                            {
                                hours = (remainSeconds % Countdown.SecondsDay) / Countdown.SecondsHour;
                                result.Append(hours.ToString().PadLeft(repeatCharNum, '0'));
                                tokenLen = repeatCharNum + 2;
                            }
                            else if (ParseNextChar(format, i + repeatCharNum) == '+' &&
                                     ParseNextChar(format, i + repeatCharNum + 1) == '}')
                            {
                                hours = remainSeconds / Countdown.SecondsHour;
                                result.Append(hours.ToString().PadLeft(repeatCharNum, '0'));
                                tokenLen = repeatCharNum + 3;
                            }
                            else
                            {
                                result.Append(ch);
                                tokenLen = 1;
                            }
                        }
                        else if ((repeatCharNum = ParseRepeatPattern(format, i, 'd')) > 0 && ParseNextChar(format, i + repeatCharNum) == '}')
                        {
                            int days = 0;
                            days = remainSeconds / Countdown.SecondsDay;
                            result.Append(days.ToString().PadLeft(repeatCharNum, '0'));
                            tokenLen = repeatCharNum + 2;
                        }
                        else
                        {
                            result.Append(ch);
                            tokenLen = 1;
                        }
                        break;
                    default:
                        result.Append(ch);
                        tokenLen = 1;
                        break;
                }
                i += tokenLen;
            }

            return StringBuilderCache.GetStringAndRelease(result);
        }

        internal static int ParseNextChar(String format, int pos)
        {
            if (pos >= format.Length - 1)
            {
                return (-1);
            }
            return ((int)format[pos + 1]);
        }

        internal static int ParseRepeatPattern(String format, int pos, char patternChar)
        {
            int len = format.Length;
            int index = pos + 1;
            while ((index < len) && (format[index] == patternChar))
            {
                index++;
            }
            return (index - pos - 1);
        }
    }
}
