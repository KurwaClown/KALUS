using System.IO;
using System.Text;

namespace League
{
    internal class Auth
    {
        private static int port = 0;
        private static string basic = "";

        internal static void SetBasicAuth(string filename)
		{
			string[] lockfile_content = GetLockfileContent(filename);

			port = int.Parse(lockfile_content[2]);
			basic = Base64Encode($"riot:{lockfile_content[3]}");
		}

		private static string[] GetLockfileContent(string filename)
		{
			string[] lockfile_content;
			int x = filename.IndexOf("LeagueClientUx.exe");
			string dir_path = filename.Remove(x);

			using (var fs = new FileStream(dir_path + "lockfile", FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
			{
				using (var sr = new StreamReader(fs, Encoding.Default))
				{
					lockfile_content = sr.ReadToEnd().Split(":");
				}
			}

			return lockfile_content;
		}

		internal static void ResetAuth()
        {
            port = 0;
            basic = "";
        }

        internal static string GetBasicAuth()
        {
            return basic;
        }

        internal static int GetPort()
        {
            return port;
        }

        internal static bool IsAuthSet()
        {
            return (port != 0 && basic != "");
        }


        private static string Base64Encode(string plainText)
        {
            var plainTextBytes = Encoding.UTF8.GetBytes(plainText);
            return System.Convert.ToBase64String(plainTextBytes);
        }
    }
}
