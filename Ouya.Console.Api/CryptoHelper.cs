// OUYA Development Kit C# bindings - Copyright (C) Konaju Games
// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt' which is part of this source code package.

using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Ouya.Console.Api
{
    static class CryptoHelper
    {
        static byte[] key = { 112, 24, 164, 208, 255, 163, 125, 249, 214, 239, 60, 233, 195, 223, 85, 197, 150, 210, 239, 103, 175, 147, 208, 150, 67, 162, 149, 176, 96, 94, 153, 131 };

        static public string Encrypt(string unencrypted, string passKey)
        {
            var guid = Guid.Parse(passKey);
            return Convert.ToBase64String(Encrypt(Encoding.UTF8.GetBytes(unencrypted), guid.ToByteArray()));
        }

        static public string Decrypt(string encrypted, string passKey)
        {
            var guid = Guid.Parse(passKey);
            return Encoding.UTF8.GetString(Decrypt(Convert.FromBase64String(encrypted), guid.ToByteArray()));
        }

        static byte[] Encrypt(byte[] buffer, byte[] vector)
        {
            var rm = new RijndaelManaged();
            var encryptor = rm.CreateEncryptor(key, vector);
            return Transform(buffer, encryptor);
        }

        static byte[] Decrypt(byte[] buffer, byte[] vector)
        {
            var rm = new RijndaelManaged();
            var decryptor = rm.CreateDecryptor(key, vector);
            return Transform(buffer, decryptor);
        }

        static byte[] Transform(byte[] buffer, ICryptoTransform transform)
        {
            MemoryStream stream = new MemoryStream();
            using (CryptoStream cs = new CryptoStream(stream, transform, CryptoStreamMode.Write))
            {
                cs.Write(buffer, 0, buffer.Length);
            }
            return stream.ToArray();
        }
    }
}