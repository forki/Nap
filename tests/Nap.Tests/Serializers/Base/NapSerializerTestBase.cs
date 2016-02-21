using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

using Nap.Tests.TestClasses;

namespace Nap.Tests.Serializers.Base
{
    public abstract class NapSerializerTestBase
    {
        protected static string GetFileContents(string fileName)
        {
            //var assemblyPath = new FileInfo(Assembly.GetAssembly(typeof(TestClass)).Location).Directory?.FullName ?? string.Empty;
            var path = Path.Combine(Directory.GetCurrentDirectory(), "Assets", fileName);
            using (var fs = File.OpenRead(path))
            using (var sr = new StreamReader(fs))
            {
                return sr.ReadToEnd();
            }
        }
    }
}