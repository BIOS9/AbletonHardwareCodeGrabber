using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace HardwareCodeGrabber
{
    class Program
    {
        public static void Main()
        {
            Process proc = Process.GetProcesses()
                .Where(x => x.ProcessName.Contains("Ableton Live"))
                .FirstOrDefault() ?? throw new Exception("Ableton Live process not found");

            Thread.Sleep(5000); // Wait for ableton to load a bit
            string dumpPath = Path.Combine(Path.GetTempPath(), Path.GetTempFileName());

            string procdumpPath = Path.Combine(Path.GetTempPath(), "procdump.exe");
            WriteResourceToFile("HardwareCodeGrabber.procdump.exe", procdumpPath);

            Process.Start(new ProcessStartInfo
            {
                FileName = procdumpPath,
                Arguments = $"-mp {proc.Id} {dumpPath}",
                RedirectStandardOutput = true,
            }).WaitForExit();


            byte[] pattern = new byte[] {
                0x59, 0x00, 0x6F, 0x00, 0x75, 0x00, 0x72, 0x00,
                0x20, 0x00, 0x68, 0x00, 0x61, 0x00, 0x72, 0x00,
                0x64, 0x00, 0x77, 0x00, 0x61, 0x00, 0x72, 0x00,
                0x65, 0x00, 0x20, 0x00, 0x63, 0x00, 0x6F, 0x00,
                0x64, 0x00, 0x65, 0x00, 0x3A, 0x00, 0x20, 0x00
            };

            byte[] hardwareCodeBuffer = new byte[58];
            int patternIndex = 0;

            using (FileStream fs = new FileStream(dumpPath + ".dmp", FileMode.Open, FileAccess.Read))
            {
                while (fs.Position < fs.Length)
                {
                    byte b = (byte)fs.ReadByte();
                    if (b == pattern[patternIndex])
                    {
                        patternIndex++;
                        if (patternIndex == pattern.Length)
                        {
                            fs.Read(hardwareCodeBuffer, 0, hardwareCodeBuffer.Length);
                            break;
                        }
                    }
                    else
                    {
                        patternIndex = 0;
                    }
                }
            }
            File.Delete(dumpPath);
            File.Delete(procdumpPath);
            string hardwareCode = Encoding.Unicode.GetString(hardwareCodeBuffer).Trim();

            if(!Regex.IsMatch(hardwareCode, @"^([A-Z0-9]{4}-){5}[A-Z0-9]{4}$"))
            {
                throw new Exception("Failed to find hardwre code in memory");
            }
            Console.Write(hardwareCode);
        }

        public static void WriteResourceToFile(string resourceName, string fileName)
        {
            using (var resource = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName))
            {
                using (var file = new FileStream(fileName, FileMode.Create, FileAccess.Write))
                {
                    if(resource == null)
                        throw new Exception("Failed to extract resource. Resource not found");
                    resource.CopyTo(file);
                }
            }
        }
    }
}