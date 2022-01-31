using System.Data.SqlClient;
using System.IO;
using System.Security;
using System.Security.Cryptography;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;

namespace WFInfo
{
    public class EncryptedDataService
    {
        private static readonly IDataProtector JwtProtector;

        static EncryptedDataService()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddDataProtection();
            var services = serviceCollection.BuildServiceProvider();
            IDataProtectionProvider provider = services.GetService<IDataProtectionProvider>();
            JwtProtector = provider?.CreateProtector("WFInfo.JWT.v1");
        } 

        public static string LoadStoredJWT()
        {
            try
            {
                var fileText = File.ReadAllText(Main.AppPath + @"\jwt_encrypted");
                return JwtProtector?.Unprotect(fileText);
            }
            catch (FileNotFoundException e)
            {
                Main.AddLog($"{e.Message}, JWT not set");
            }
            catch (CryptographicException e)
            {
                Main.AddLog($"{e.Message}, JWT decryption failed");
            }

            return null;
        }
        
        public static void PersistJWT(string jwt)
        {
            var encryptedJWT = JwtProtector?.Protect(jwt);
            File.WriteAllText(Main.AppPath + @"\jwt_encrypted", encryptedJWT);
        }
        
        
    }
}