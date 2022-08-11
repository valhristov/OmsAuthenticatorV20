using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography.Pkcs;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace SignData
{
    class Program
    {
        static int Main(string[] args)
        {
            var serialNumber = args.FirstOrDefault();
            var value = args.Skip(1).FirstOrDefault();

            if (string.IsNullOrEmpty(serialNumber) || string.IsNullOrEmpty(value))
            {
                Console.WriteLine("Provide certificate serial number as first argument and value to sign or path as second.");
                return 1;
            }

            try
            {
                if (File.Exists(value))
                {
                    try
                    {
                        value = File.ReadAllText(value);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                        return 1;
                    }
                }

                var certificate = GetSignerCertificate(serialNumber);
                Console.Write(Sign(value, certificate));
                return 0; // OK
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return 1;
            }
        }

        private static string Sign(string value, X509Certificate2 certificate)
        {
            var cmsSigner = new CmsSigner(certificate);
            cmsSigner.IncludeOption = X509IncludeOption.EndCertOnly;

            var signedCms = new SignedCms(new ContentInfo(Encoding.UTF8.GetBytes(value)), detached: false);
            signedCms.ComputeSignature(cmsSigner);

            return Convert.ToBase64String(signedCms.Encode());
        }

        private static X509Certificate2 GetSignerCertificate(string serialNumber)
        {
            var certificates = GetCertificates(serialNumber, StoreLocation.LocalMachine);

            if (certificates.Count == 0)
            {
                certificates = GetCertificates(serialNumber, StoreLocation.CurrentUser);
            }

            return certificates.Count > 0
                ? certificates[0]
                : throw new InvalidOperationException($"Cannot find certificate with SerialNumber='{serialNumber}'.");

            static X509Certificate2Collection GetCertificates(string serialNumber, StoreLocation storeLocation)
            {
                using (var store = new X509Store(StoreName.My, storeLocation))
                {
                    store.Open(OpenFlags.ReadOnly);

                    var certificates = store.Certificates.Find(X509FindType.FindBySerialNumber, serialNumber, false);

                    store.Close();

                    return certificates;
                }
            }
        }
    }
}
