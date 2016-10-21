
namespace UGCore.Components
{
    public class VersionNumber
    {
        public int[] m_VersionNumbers = new int[4];
 
        public override string ToString()
        {
            return string.Format("{0}.{1}.{2}.{3}", m_VersionNumbers[0], m_VersionNumbers[1], m_VersionNumbers[2], m_VersionNumbers[3]);
        }

        public static int CompareTo(VersionNumber obj1, VersionNumber obj2)
        {
            for (int i = 0; i < obj1.m_VersionNumbers.Length; ++i)
            {
                var result = obj1.m_VersionNumbers[i].CompareTo(obj2.m_VersionNumbers[i]);
                if (result != 0)
                {
                    return result;
                }
            }
            return 0;
        }

        private void SetVersionNumber(int index, int value)
        {
            if (index >= 4)
            {
                return;
            }
            m_VersionNumbers[index] = value;
        }

        public static VersionNumber ParseString(string version)
        {
            if (string.IsNullOrEmpty(version))
            {
                return null;
            }

            VersionNumber versionNumber = new VersionNumber();

            int count = 0;
            int startIndex = 0;
            for (int i = 0; i < version.Length; ++i)
            {
                if (version[i] < '0' || version[i] > '9')
                {
                    int result = 0;
                    if (!int.TryParse(version.Substring(startIndex, i - startIndex), out result))
                    {
                        return null;
                    }

                    versionNumber.SetVersionNumber(count, result);
                    ++count;

                    if (version[i] != '.' || count >= 4)
                    {
                        return null;
                    }

                    startIndex = i + 1;
                    continue;
                }

                if (i == version.Length - 1)
                {
                    int result = 0;
                    if (!int.TryParse(version.Substring(startIndex, i - startIndex + 1), out result))
                    {
                        return null;
                    }
                    versionNumber.SetVersionNumber(count, result);

                    if (count >= 4)
                    {
                        return null;
                    }
                }
            }

            return versionNumber;
        }
    }
}
