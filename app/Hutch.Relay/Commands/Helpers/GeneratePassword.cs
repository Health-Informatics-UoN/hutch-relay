using System.Security.Cryptography;
using System.Text;

namespace Hutch.Relay.Commands.Helpers;

public static class GeneratePassword
{
  
  private static readonly char[] chars = ("abcdefghijklmnopqrstuvwxyz" +
                                          "ABCDEFGHIJKLMNOPQRSTUVWXYZ" +
                                          "0123456789" +
                                          "$#_!%*&").ToCharArray();
  public static string GenerateUniquePassword(int size)
  {
    byte[] data = new byte[4 * size];
    using (var crypto = RandomNumberGenerator.Create())
    {
      crypto.GetBytes(data);
    }

    StringBuilder result = new StringBuilder(size);
    for (int i = 0; i < size; i++)
    {
      var rnd = BitConverter.ToUInt32(data, i * 4);
      var index = rnd % chars.Length;

      result.Append(chars[index]);
    }
    return result.ToString();
  }
}
