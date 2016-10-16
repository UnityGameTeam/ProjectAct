using System.Collections.Generic;

namespace GameLogic.Components
{
    public class Bundle
    {
        protected static Stack<Bundle> _BundlesCache = new Stack<Bundle>();

        protected Dictionary<string,string> m_BundleMap = new Dictionary<string, string>();

        protected Bundle()
        {
            
        }

        public static Bundle GetBundle()
        {
            if (_BundlesCache.Count > 0)
            {
                return _BundlesCache.Pop();
            }    
            return new Bundle();
        }

        public static void CacheBundle(Bundle bundle)
        {
            if (bundle == null)
                return;

            bundle.Clear();
            _BundlesCache.Push(bundle);
        }

        public void Clear()
        {
            m_BundleMap.Clear();
        }

        public string GetString(string key, string defaultValue = null)
        {
            if (m_BundleMap.ContainsKey(key))
            {
                return m_BundleMap[key];
            }
            return defaultValue;
        }

        public void PutString(string key, string value)
        {
            if (m_BundleMap.ContainsKey(key))
            {
                m_BundleMap[key] = value;
            }
            else
            {
                m_BundleMap.Add(key,value);
            }
        }
    }
}
