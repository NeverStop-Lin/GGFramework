using System;
using System.Globalization;

namespace Framework.Core
{
    public static class UnitUtils
    {
        const int UnitLength = 3;

        public static double ToValue(this string value)
        {
            var result = 0d;
            if (string.IsNullOrEmpty(value))
            {
                return result;
            }

            // ʹ���������ʽ�������ֺ͵�λ����
            var match = System.Text.RegularExpressions.Regex.Match(value, @"^(\d+\.?\d*)([a-zA-Z]*)$");
            if (!match.Success) return result;

            var numberPart = match.Groups[1].Value;
            var suffix = match.Groups[2].Value.ToUpper();

            if (!double.TryParse(numberPart, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out var number)) return result;
            string[] baseUnits =
            {
                "", "K", "M", "B", "T"
            };

            // ����Ԥ���嵥λ
            var unitIndex = System.Array.FindIndex(baseUnits, u => u == suffix);
            if (unitIndex != -1)
            {
                result = number * System.Math.Pow(10, UnitLength * unitIndex);
            }
            else if (suffix.Length == 2) // �����Զ���˫��ĸ��λ
            {
                var extraIndex = (suffix[0] - 'A') * 26 + (suffix[1] - 'A');
                var curIndex = baseUnits.Length + extraIndex;
                result = number * System.Math.Pow(10, UnitLength * curIndex);
            }
            return result;
        }

        public static string ToUnitValue(this double value)
        {
            string[] baseUnits =
            {
                "", "K", "M", "B", "T"
            };
            string suffix;
            var curIndex = 0;
            var scaledValue = value;
            var unitMaxValue = Math.Pow(10, UnitLength);
            while (scaledValue >= unitMaxValue)
            {
                scaledValue /= unitMaxValue;
                curIndex++;
            }


            if (curIndex < baseUnits.Length)
            {
                suffix = baseUnits[curIndex];
            }
            else
            {
                var extraIndex = curIndex - baseUnits.Length;
                var firstChar = (char)('a' + extraIndex / 26);
                var secondChar = (char)('a' + extraIndex % 26);
                suffix = $"{firstChar}{secondChar}";
            }


            return $"{scaledValue:0.##}{suffix}";
        }

    }
}