// WARNING: Do not modify! Generated file.

namespace UnityEngine.Purchasing.Security {
    public class GooglePlayTangle
    {
        private static byte[] data = System.Convert.FromBase64String("BJ14c6zHTHKzRWi5eWz+6yZ6ArrggEniueUDzKKPlq3oB+gJiK3PfrTNkA3ynRjf9pYxMfMlVDt1n+MjPwhph+bkJSFYzy22O7y32kLPhPcygAMgMg8ECyiESoT1DwMDAwcCAWImZOHB+rM6VBkMFtIBEnK1KroalStRS7wAPpanrgMz9V0nMbjJxgBWQVvT4N2XUtihCrYNAQu53ohPEIADDQIygAMIAIADAwK/AerVJEPHBb0/tlEqiWpANM5stvlYBTT5Tl6829YLJOv4SL8AEEILJYeixr8KMih2yPJ2JEWds3KoH2ms0NmZmwFquS1XzZxr4ITrX7mi5aAkA1HtxnSXCX5rGCsYjg2OxUEs/39zSG5mSiH+AYS5Kan3OwABAwID");
        private static int[] order = new int[] { 3,13,11,12,12,8,11,11,13,11,10,12,13,13,14 };
        private static int key = 2;

        public static readonly bool IsPopulated = true;

        public static byte[] Data() {
        	if (IsPopulated == false)
        		return null;
            return Obfuscator.DeObfuscate(data, order, key);
        }
    }
}
