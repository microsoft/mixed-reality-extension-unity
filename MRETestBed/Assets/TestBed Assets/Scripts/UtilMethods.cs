using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

internal static class UtilMethods
{
	public static Guid StringToGuid(string str)
	{
		var stringbytes = Encoding.ASCII.GetBytes(str);
		var hashedBytes = new System.Security.Cryptography.SHA1CryptoServiceProvider().ComputeHash(stringbytes);
		Array.Resize(ref hashedBytes, 16);
		return new Guid(hashedBytes);
	}
}
