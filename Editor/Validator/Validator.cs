using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace Percas.Editor
{
    public class Validator
    {
        private readonly PercasConfigSO percasConfig;

        public Validator(PercasConfigSO percasConfig)
        {
            this.percasConfig = percasConfig;
        }

        public bool CheckPackageName()
        {
            return !Constants.DefaultPackageName.Equals(percasConfig.PackageName);
        }

        public bool CheckIcon()
        {
            return percasConfig.IconTexture != null;
        }

        public bool CheckSplash()
        {
            return true;
        }

        static bool FilesAreEqual_Hash(FileInfo first, FileInfo second)
        {
            using var inputStream1 = first.OpenRead();
            using var inputStream2 = second.OpenRead();
            byte[] firstHash = MD5.Create().ComputeHash(inputStream1);
            byte[] secondHash = MD5.Create().ComputeHash(inputStream2);

            for (int i = 0; i < firstHash.Length; i++)
            {
                if (firstHash[i] != secondHash[i])
                    return false;
            }

            return true;
        }
    }
}