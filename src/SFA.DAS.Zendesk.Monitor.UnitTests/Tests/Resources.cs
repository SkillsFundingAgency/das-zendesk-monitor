using System;
using System.IO;
using System.Linq;
using System.Reflection;

namespace SFA.DAS.Zendesk.Monitor.UnitTests.Tests
{
    public class Resources
    {
        public static string LoadAsString(string resourceName)
        {
            try
            {
                var assembly = Assembly.GetExecutingAssembly();
                var allResources = assembly.GetManifestResourceNames().OrderBy(x => x).ToList();

                using (Stream stream = assembly.GetManifestResourceStream(resourceName))
                using (StreamReader reader = new StreamReader(stream))
                {
                    string result = reader.ReadToEnd();
                    return result;
                }
            }
            catch (Exception e)
            {
                throw new Exception($"Could not read resource `{resourceName}`", e);
            }
        }
    }
}